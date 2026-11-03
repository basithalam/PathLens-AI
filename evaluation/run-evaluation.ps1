<#
.SYNOPSIS
  PathLens AI reproducible evaluation runner (Blueprint Section 19).

.DESCRIPTION
  Reads a labelled manifest CSV, uploads each image to the running API,
  runs analysis, polls the job with a bounded wait, compares pairs, and
  writes a JSON report with expected vs actual outcomes.

  Counts are reported honestly: correct, incorrect, and inconclusive are
  separate. A confident wrong answer is the most serious error;
  Inconclusive is a safe answer, never hidden inside "incorrect".

  The API must already be running (with whichever AI provider configured
  server-side). This script never sets API keys itself.

.EXAMPLE
  ./evaluation/run-evaluation.ps1 -ManifestPath evaluation/manifest.example.csv
#>
param(
    [string]$ApiBaseUrl = "http://localhost:5145",
    [Parameter(Mandatory = $true)]
    [string]$ManifestPath,
    [string]$OutputPath = "evaluation/results.json",
    [int]$PollTimeoutSeconds = 90,
    # The API rate limits evidence uploads (H5: 10 per hour per client). A run longer than
    # the limit used to fail with "Upload did not return an evidence id", because curl -s hid
    # the 429 and the body was not JSON. These two knobs make the limit visible and survivable.
    [int]$UploadSpacingSeconds = 0,
    [int]$Upload429Retries = 5
)

$ErrorActionPreference = "Stop"
$manifestDir = Split-Path -Parent (Resolve-Path $ManifestPath)
$rows = Import-Csv $ManifestPath

function Cut([string]$text, [int]$length) {
    if ([string]::IsNullOrEmpty($text)) { return "" }
    if ($text.Length -le $length) { return $text }
    return $text.Substring(0, $length) + "…"
}

function Invoke-ApiJson {
    param([string]$Method, [string]$Path, $Body)
    $params = @{ Method = $Method; Uri = "$ApiBaseUrl$Path" }
    if ($null -ne $Body) {
        $params.ContentType = "application/json"
        $params.Body = ($Body | ConvertTo-Json)
    }
    return Invoke-RestMethod @params
}

$results = @()
$byId = @{}
$reportByKey = @{}

foreach ($row in $rows) {
    $imagePath = Join-Path $manifestDir $row.image
    if (-not (Test-Path $imagePath)) {
        $results += [ordered]@{ id = $row.id; error = "Image file not found: $imagePath" }
        continue
    }

    try {
        # Pair members share one report (comparisons stay within a report);
        # single rows get their own report.
        $reportKey = if ($row.pairId) { "pair:$($row.pairId)" } else { "single:$($row.id)" }
        if (-not $reportByKey.ContainsKey($reportKey)) {
            $reportByKey[$reportKey] = Invoke-ApiJson -Method Post -Path "/api/reports" -Body @{
                title = "Evaluation $reportKey"
                description = "Created by the evaluation runner."
                obstacleType = "Unknown"
                severity = "Unknown"
                latitude = [double]$row.latitude
                longitude = [double]$row.longitude
            }
        }
        $report = $reportByKey[$reportKey]

        # Multipart upload (with the claimed capture time) through curl,
        # which ships with Windows and handles form fields reliably.
        $mime = switch ([IO.Path]::GetExtension($imagePath).ToLowerInvariant()) {
            ".jpg" { "image/jpeg" }
            ".jpeg" { "image/jpeg" }
            ".webp" { "image/webp" }
            default { "image/png" }
        }
        $formFields = @("-F", "file=@$imagePath;type=$mime")
        if ($row.captureTimeUtc) { $formFields += @("-F", "captureTimeUtc=$($row.captureTimeUtc)") }

        $evidence = $null
        $attempt = 0
        while ($null -eq $evidence) {
            $attempt++
            $bodyFile = [IO.Path]::GetTempFileName()
            try {
                $httpCode = [int](& curl.exe -s -o $bodyFile -w "%{http_code}" @formFields `
                    "$ApiBaseUrl/api/reports/$($report.id)/evidence")
                $responseBody = (Get-Content $bodyFile -Raw) + ""

                if ($httpCode -eq 429 -and $attempt -le $Upload429Retries) {
                    $wait = 60 * $attempt
                    Write-Warning ("Upload rate limited (429) for $($row.id) - waiting $wait s, retry $attempt of $Upload429Retries.")
                    Start-Sleep -Seconds $wait
                    continue
                }
                if ($httpCode -lt 200 -or $httpCode -ge 300) {
                    throw "Upload returned HTTP $httpCode : $(Cut ($responseBody -replace '\s+', ' ') 200)"
                }

                $parsed = $responseBody | ConvertFrom-Json
                if (-not $parsed -or -not $parsed.id) {
                    throw "Upload returned HTTP $httpCode but no evidence id: $(Cut $responseBody 200)"
                }
                $evidence = $parsed
            } finally {
                Remove-Item $bodyFile -Force -ErrorAction SilentlyContinue
            }
        }

        if ($UploadSpacingSeconds -gt 0) { Start-Sleep -Seconds $UploadSpacingSeconds }

        $job = Invoke-ApiJson -Method Post -Path "/api/evidence/$($evidence.id)/analyze" -Body $null

        $deadline = (Get-Date).AddSeconds($PollTimeoutSeconds)
        $jobStatus = $job
        while ($jobStatus.status -notin @("Completed", "Failed")) {
            if ((Get-Date) -gt $deadline) {
                throw "Analysis job $($job.id) did not finish within $PollTimeoutSeconds seconds."
            }
            Start-Sleep -Seconds 2
            $jobStatus = Invoke-ApiJson -Method Get -Path "/api/analysis-jobs/$($job.id)"
        }

        if ($jobStatus.status -eq "Failed") {
            $results += [ordered]@{
                id = $row.id
                expectedObstacle = $row.expectedObstacle
                actualObstacle = $null
                provider = $null
                verdict = "Error"
                error = $jobStatus.errorMessage
            }
            continue
        }

        $completed = Invoke-ApiJson -Method Get -Path "/api/evidence/$($evidence.id)"
        $analysis = $completed.analysisResultJson | ConvertFrom-Json

        $verdict = if ($analysis.obstacleType -eq $row.expectedObstacle) { "Correct" }
            elseif ($analysis.obstacleType -eq "Unknown") { "Inconclusive" }
            else { "Incorrect" }

        $entry = [ordered]@{
            id = $row.id
            reportId = $report.id
            evidenceId = $evidence.id
            expectedObstacle = $row.expectedObstacle
            actualObstacle = $analysis.obstacleType
            confidence = $analysis.confidence
            provider = $analysis.provider
            model = $analysis.model
            pairId = $row.pairId
            captureTimeUtc = $row.captureTimeUtc
            verdict = $verdict
        }
        $results += $entry
        $byId[$row.id] = $entry
    }
    catch {
        $results += [ordered]@{ id = $row.id; verdict = "Error"; error = $_.Exception.Message }
    }
}

# Pair comparisons: rows sharing a pairId, ordered by capture time.
$pairResults = @()
$pairGroups = $rows | Where-Object { $_.pairId } | Group-Object pairId
foreach ($group in $pairGroups) {
    $ordered = $group.Group | Sort-Object captureTimeUtc
    if ($ordered.Count -lt 2) { continue }

    $earlier = $byId[$ordered[0].id]
    $later = $byId[$ordered[1].id]
    $expectedRow = $ordered | Where-Object { $_.expectedOutcome } | Select-Object -First 1
    if (-not $earlier -or -not $later -or -not $expectedRow) { continue }

    try {
        $comparison = Invoke-ApiJson -Method Post -Path "/api/comparisons" -Body @{
            earlierEvidenceId = $earlier.evidenceId
            laterEvidenceId = $later.evidenceId
        }

        $verdict = if ($comparison.outcome -eq $expectedRow.expectedOutcome) { "Correct" }
            elseif ($comparison.outcome -eq "Inconclusive") { "Inconclusive" }
            else { "Incorrect" }

        $pairResults += [ordered]@{
            pairId = $group.Name
            expectedOutcome = $expectedRow.expectedOutcome
            actualOutcome = $comparison.outcome
            verdict = $verdict
        }
    }
    catch {
        $pairResults += [ordered]@{ pairId = $group.Name; verdict = "Error"; error = $_.Exception.Message }
    }
}

$summary = [ordered]@{
    generatedAt = (Get-Date).ToUniversalTime().ToString("O")
    apiBaseUrl = $ApiBaseUrl
    manifest = $ManifestPath
    images = [ordered]@{
        total = $results.Count
        correct = @($results | Where-Object { $_.verdict -eq "Correct" }).Count
        incorrect = @($results | Where-Object { $_.verdict -eq "Incorrect" }).Count
        inconclusive = @($results | Where-Object { $_.verdict -eq "Inconclusive" }).Count
        errors = @($results | Where-Object { $_.verdict -eq "Error" }).Count
    }
    pairs = [ordered]@{
        total = $pairResults.Count
        correct = @($pairResults | Where-Object { $_.verdict -eq "Correct" }).Count
        incorrect = @($pairResults | Where-Object { $_.verdict -eq "Incorrect" }).Count
        inconclusive = @($pairResults | Where-Object { $_.verdict -eq "Inconclusive" }).Count
        errors = @($pairResults | Where-Object { $_.verdict -eq "Error" }).Count
    }
    note = "A 20-30 image set is a demo evaluation, never proof of general accuracy. Inconclusive is reported separately from incorrect."
}

$report = [ordered]@{ summary = $summary; images = $results; pairs = $pairResults }
$report | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputPath -Encoding UTF8

Write-Host "Images: $($summary.images.correct) correct, $($summary.images.incorrect) incorrect, $($summary.images.inconclusive) inconclusive, $($summary.images.errors) errors"
Write-Host "Pairs:  $($summary.pairs.correct) correct, $($summary.pairs.incorrect) incorrect, $($summary.pairs.inconclusive) inconclusive, $($summary.pairs.errors) errors"
Write-Host "Full report written to $OutputPath"

if ($summary.images.errors -gt 0 -or $summary.pairs.errors -gt 0) {
    exit 1
}
