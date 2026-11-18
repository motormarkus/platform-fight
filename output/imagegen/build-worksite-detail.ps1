param([string]$Only='', [switch]$AssembleOnly)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'WorksiteDetail.cs') -ReferencedAssemblies System.Drawing
$folder=Join-Path $PSScriptRoot 'vegas-tyomaa-tarkka-v2'
$manifest=Get-Content -LiteralPath (Join-Path $folder 'osat.json') -Raw | ConvertFrom-Json
$generations=Get-Content -LiteralPath (Join-Path $folder 'generoidut-kaikki.json') -Raw | ConvertFrom-Json
$rawDir=Join-Path $folder 'natiivit'
$processedDir=Join-Path $folder 'sovitetut'
$rowDir=Join-Path $folder 'rivit'
New-Item -ItemType Directory -Path $rawDir,$processedDir,$rowDir -Force | Out-Null
$paths=@{}
foreach($entry in $generations){
 $match=[regex]::Match($entry.hint,'as (C:\\[^\r\n]+?\.png) by default')
 if(!$match.Success){throw ('Missing generated path '+$entry.id)}
 $paths[$entry.id]=$match.Groups[1].Value
}
$stats=@()
foreach($tile in $manifest){
 if(!$paths.ContainsKey($tile.id)){throw ('Missing tile '+$tile.id)}
 $raw=Join-Path $rawDir ($tile.id+'.png')
 if(!(Test-Path -LiteralPath $raw)){Copy-Item -LiteralPath $paths[$tile.id] -Destination $raw}
 $im=[Drawing.Bitmap]::new($raw)
 $stats+=[pscustomobject]@{id=$tile.id;nativeWidth=$im.Width;nativeHeight=$im.Height;placedWidth=$tile.w;placedHeight=$tile.h;raw=$raw}
 $im.Dispose()
 if($AssembleOnly -or ($Only -and $Only -ne $tile.id)){continue}
 $target=Join-Path $processedDir ($tile.id+'.png')
 if(Test-Path -LiteralPath $target){continue}
 $watch=[Diagnostics.Stopwatch]::StartNew()
 [WorksiteDetail]::Process($raw,$tile.reference,$target)
 Write-Output ('Processed {0}: {1:N1}s' -f $tile.id,$watch.Elapsed.TotalSeconds)
}
$stats | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $folder 'lahdetarkkuudet.json') -Encoding utf8
if($Only){exit}
foreach($row in 0..3){
 $tiles=@($manifest | Where-Object row -eq $row | Sort-Object col)
 $h=[int]$tiles[0].h
 $canvas=[byte[]]::new(15360*$h*4)
 foreach($tile in $tiles){
  $w=0;$th=0
  $d=[WorksiteDetail]::Load((Join-Path $processedDir ($tile.id+'.png')),[ref]$w,[ref]$th)
  $overlap=if($tile.col -eq 0){0}else{384}
  [WorksiteDetail]::JoinX($canvas,15360,$h,$d,$w,$th,[int]$tile.x,$overlap)
 }
 [WorksiteDetail]::Save($canvas,15360,$h,(Join-Path $rowDir ('r'+$row+'.png')))
 Write-Output ('Assembled row '+$row)
}
$canvas=[byte[]]::new(15360*5632*4)
foreach($row in 0..3){
 $w=0;$h=0;$d=[WorksiteDetail]::Load((Join-Path $rowDir ('r'+$row+'.png')),[ref]$w,[ref]$h)
 $y=if($row -eq 0){0}else{$row*1408-192}
 $overlap=if($row -eq 0){0}else{384}
 [WorksiteDetail]::JoinY($canvas,15360,5632,$d,$w,$h,$y,$overlap)
 Write-Output ('Joined full row '+$row)
}
$w=0;$h=0
$original=[WorksiteDetail]::Load((Join-Path $PSScriptRoot 'vegas-tyomaa-yhtena\vegas-tyomaa-yhtenainen-15360x5632.png'),[ref]$w,[ref]$h)
[WorksiteDetail]::Clean($canvas)
[WorksiteDetail]::EdgeCollar($canvas,$original,15360,5632)
$final=Join-Path $folder 'vegas-tyomaa-tarkka-15360x5632.png'
[WorksiteDetail]::Save($canvas,15360,5632,$final)
[WorksiteDetail]::Preview($final,(Join-Path $folder 'esikatselu.png'),2560,939)
Write-Output $final

