<#
.SYNOPSIS
  Seed SF demo reports for judges - 7 diverse markers around 37.7749,-122.4194.
.DESCRIPTION
  Creates 7 reports via POST /api/reports so map is never empty for fresh clone.
  Each gets evidence upload + analysis. Uses synthetic placeholders - no faces/plates.
.EXAMPLE
  ./scripts/seed-sf.ps1
  ./scripts/seed-sf.ps1 -ApiBaseUrl http://localhost:5145
#>
param([string]$ApiBaseUrl = "http://localhost:5145")
$ErrorActionPreference = "Stop"
$ApiBaseUrl = $ApiBaseUrl.TrimEnd('/')
function Invoke-ApiJson {
  param([string]$Method,[string]$Path,$Body)
  $params = @{ Method=$Method; Uri="$ApiBaseUrl$Path" }
  if ($null -ne $Body) { $params.ContentType="application/json"; $params.Body=($Body|ConvertTo-Json -Depth 8) }
  return Invoke-RestMethod @params
}
Write-Host "Seeding SF demo reports to $ApiBaseUrl ..." -ForegroundColor Cyan
try { $health=Invoke-RestMethod -Uri "$ApiBaseUrl/health/live" -TimeoutSec 5; Write-Host "Health live: $($health.status)" -ForegroundColor Green } catch { Write-Host "WARNING: $ApiBaseUrl/health/live not reachable - is docker compose up? $($_.Exception.Message)" -ForegroundColor Yellow }
$reports = @(
  @{ title="Market St - Scooter blocking sidewalk"; description="Synthetic placeholder: scooter cluster on Market St near 5th."; obstacleType="BlockedSidewalk"; severity="High"; latitude=37.7749; longitude=-122.4194; image="evaluation/sf-dataset/sf-01_blockedsidewalk.png"; captureTimeUtc="2026-10-28T10:00:00Z" },
  @{ title="Civic Center - Cracked uneven pavement"; description="Synthetic: broken surface with pothole near Civic Center."; obstacleType="BrokenSurface"; severity="Medium"; latitude=37.7755; longitude=-122.4180; image="evaluation/sf-dataset/sf-02_brokensurface.png"; captureTimeUtc="2026-10-27T11:00:00Z" },
  @{ title="SoMa - Ramp blocked by trash"; description="Synthetic: wheelchair ramp entrance blocked."; obstacleType="MissingOrBlockedRamp"; severity="High"; latitude=37.7760; longitude=-122.4170; image="evaluation/sf-dataset/sf-03_missingorblockedramp.png"; captureTimeUtc="2026-10-26T09:00:00Z" },
  @{ title="Mission - Unexpected stairs"; description="Synthetic: stairs where ramp expected."; obstacleType="StairsOrStep"; severity="High"; latitude=37.7765; longitude=-122.4165; image="evaluation/sf-dataset/sf-04_stairsorstep.png"; captureTimeUtc="2026-10-25T10:00:00Z" },
  @{ title="SoMa - Construction scaffolding barrier"; description="Synthetic: construction barrier forcing detour."; obstacleType="ConstructionBarrier"; severity="Medium"; latitude=37.7740; longitude=-122.4200; image="evaluation/sf-dataset/sf-05_constructionbarrier.png"; captureTimeUtc="2026-10-28T08:00:00Z" },
  @{ title="Embarcadero - Clear well-maintained sidewalk"; description="Synthetic: clear path, Unknown = no obstacle."; obstacleType="Unknown"; severity="Low"; latitude=37.7735; longitude=-122.4210; image="evaluation/sf-dataset/sf-06_unknown.png"; captureTimeUtc="2026-10-29T09:00:00Z" },
  @{ title="Ferry Building - Fallen branch"; description="Synthetic: Other - fallen tree branch."; obstacleType="Other"; severity="Medium"; latitude=37.7762; longitude=-122.4160; image="evaluation/sf-dataset/sf-13_other.png"; captureTimeUtc="2026-10-28T12:00:00Z" }
)
$created=@()
foreach ($r in $reports) {
  try {
    $report=Invoke-ApiJson -Method Post -Path "/api/reports" -Body @{ title=$r.title; description=$r.description; obstacleType=$r.obstacleType; severity=$r.severity; latitude=$r.latitude; longitude=$r.longitude }
    Write-Host "Created report $($report.id) - $($r.title)" -ForegroundColor Green
    $created+=$report
    $imagePath=Join-Path $PSScriptRoot ".." $r.image
    if (-not (Test-Path $imagePath)) { $imagePath=$r.image }
    if (Test-Path $imagePath) {
      $mime=if ($imagePath -like "*.jpg"){"image/jpeg"}else{"image/png"}
      $curlArgs=@("-s","-F","file=@$imagePath;type=$mime","-F","captureTimeUtc=$($r.captureTimeUtc)","$ApiBaseUrl/api/reports/$($report.id)/evidence")
      $evidenceJson=& curl.exe @curlArgs 2>$null
      if ($LASTEXITCODE -eq 0 -and $evidenceJson) {
        $evidence=$evidenceJson|ConvertFrom-Json
        if ($evidence.id) {
          Write-Host "  Uploaded evidence $($evidence.id)" -ForegroundColor DarkGreen
          try { $job=Invoke-ApiJson -Method Post -Path "/api/evidence/$($evidence.id)/analyze" -Body $null; Write-Host "  Analysis job $($job.id) $($job.status)" -ForegroundColor DarkCyan } catch { Write-Host "  Analysis trigger failed: $($_.Exception.Message)" -ForegroundColor Yellow }
        }
      } else { Write-Host "  Evidence upload failed (curl exit $LASTEXITCODE)" -ForegroundColor Yellow }
    } else { Write-Host "  No image at $imagePath - report without evidence" -ForegroundColor Yellow }
  } catch {
    Write-Host "Failed '$($r.title)': $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message){ Write-Host $_.ErrorDetails.Message -ForegroundColor Red }
  }
  Start-Sleep -Milliseconds 500
}
Write-Host "`nSeed complete: $($created.Count)/7 reports created." -ForegroundColor Cyan
if ($created.Count -gt 0){ Write-Host "Map: $ApiBaseUrl/health/live Healthy, frontend /map will show markers." -ForegroundColor Cyan; $created|ForEach-Object{ Write-Host "  $($_.id) - $($_.title)" } }
Write-Host "`nCleanup: DELETE /api/reports/{id} soft-deletes; evidence retained 30d per PRIVACY_RETENTION.md" -ForegroundColor DarkGray

