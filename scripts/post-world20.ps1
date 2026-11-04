# World20 batch 1: 9 verified images (Hawaii OSM excluded, license unclear)
$ErrorActionPreference = "Stop"
$base = "https://pathlens.cognitivebd.com"
$dir = "d:/PathLens AI/evaluation/world20"
New-Item -ItemType Directory -Force -Path $dir | Out-Null
function Post-One($f, $url, $lat, $lon, $type, $sev, $title, $desc) {
  $dest = Join-Path $dir $f
  if (-not (Test-Path $dest)) { Invoke-WebRequest -Uri $url -OutFile $dest -UseBasicParsing }
  $kb = [math]::Round((Get-Item $dest).Length/1KB,1)
  Write-Host "$f $kb KB"
  $body = @{title=$title;description=$desc;obstacleType=$type;severity=$sev;latitude=$lat;longitude=$lon} | ConvertTo-Json -Compress
  $rep = Invoke-RestMethod -Method Post -Uri "$base/api/reports" -ContentType "application/json" -Body $body
  Write-Host ("report " + $rep.id)
  $ev = curl.exe -s -F "file=@$dest;type=image/jpeg" "$base/api/reports/$($rep.id)/evidence"
  $eid = ($ev | ConvertFrom-Json).id
  Write-Host ("evidence " + $eid)
  $job = Invoke-RestMethod -Method Post -Uri "$base/api/evidence/$eid/analyze"
  Write-Host ("job " + $job.id + " " + $job.status)
  Start-Sleep -Seconds 65
}
Post-One "w01-philadelphia-ramp3616.jpg" "https://commons.wikimedia.org/wiki/Special:FilePath/Curb_cut_for_wheelchair_ramp_%28DSC_3616%29.jpg?width=960" 39.934408 -75.158883 "MissingOrBlockedRamp" "Low" "Granite curb-cut wheelchair ramp, Philadelphia" "CC BY-SA 4.0 Nick-philly. Clear granite curb cut. Own city evidence."
Post-One "w02-philadelphia-ramp3500.jpg" "https://commons.wikimedia.org/wiki/Special:FilePath/Curb_cut_for_wheelchair_ramp_%28DSC_3500%29.jpg?width=960" 39.934150 -75.158595 "MissingOrBlockedRamp" "Low" "Lowered granite curb ramp, Philadelphia" "CC BY-SA 4.0 Nick-philly. Lowered curb with tactile edge. Own GPS."
Post-One "w03-barcelona-tempramp.jpg" "https://commons.wikimedia.org/wiki/Special:FilePath/Temporary_curb_ramp_at_construction_site_%2818602577880%29.jpg?width=960" 41.397455 2.150700 "ConstructionBarrier" "Medium" "Temporary curb ramp at construction site, Barcelona" "CC BY 2.0 Eric Fischer. Metal temp ramp at works. Own GPS."
Post-One "w04-spain-tempramp.jpg" "https://commons.wikimedia.org/wiki/Special:FilePath/Temporary_curb_ramp_in_construction_zone_%2818811858825%29.jpg?width=960" 43.258025 -2.948250 "ConstructionBarrier" "Medium" "Temporary ramp in construction zone, Spain" "CC BY 2.0 Eric Fischer. Temp ramp plates at site. Own GPS."
Post-One "w05-colchester-lowered.jpg" "https://commons.wikimedia.org/wiki/Special:FilePath/Sidewalk_ramp_to_lowered_curb_and_flat_tactile_paving_in_Colchester_VT.jpg?width=960" 44.529253 -73.175161 "MissingOrBlockedRamp" "Low" "Sidewalk ramp to lowered curb and tactile, Colchester VT" "CC0 Ezekielf. Ramp reaches road level. Own GPS."
Post-One "w06-colchester-long.jpg" "https://commons.wikimedia.org/wiki/Special:FilePath/Long_gradual_sidewalk_ramp_to_tactile_paving_and_curb_cut_in_Colchester_VT.jpg?width=960" 44.528761 -73.175828 "MissingOrBlockedRamp" "Low" "Long gradual sidewalk ramp, Colchester VT" "CC0 Ezekielf. Gentle ramp to tactile and curb cut. Own GPS."
Post-One "w07-colchester-flat.jpg" "https://commons.wikimedia.org/wiki/Special:FilePath/Nearly_flat_sidewalk_ramp_to_tactile_paving_and_curb_cut_in_Colchester_VT.jpg?width=960" 44.529003 -73.171486 "MissingOrBlockedRamp" "Low" "Nearly flat sidewalk ramp, Colchester VT" "CC0 Ezekielf. Almost flat incline. GPS from EXIF; verify on map."
Post-One "w08-winooski-flush.jpg" "https://commons.wikimedia.org/wiki/Special:FilePath/Sidewalk_ramp_to_flush_curb_with_flat_tactile_paving_in_Winooski_VT.jpg?width=960" 44.490714 -73.184017 "MissingOrBlockedRamp" "Low" "Flush-curb ramp and tactile, Winooski VT" "CC0 Ezekielf. Flush curb at street level. Own GPS."
Post-One "w10-leeds-barrier.jpg" "https://geograph.org.uk/reuse.php?id=7667696&download=1" 53.81213 -1.62272 "ConstructionBarrier" "High" "Wind-blown barriers across footpath, Leeds UK" "CC BY-SA 2.0 Stephen Craven via Geograph. Barriers blown across pavement. Own GPS."
Write-Host "DONE world20 batch 1"
