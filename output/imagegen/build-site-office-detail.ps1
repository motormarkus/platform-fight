$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'WorksiteDetail.cs') -ReferencedAssemblies System.Drawing
$folder=Join-Path $PSScriptRoot 'vegas-tyomaakonttori-katu-01'
$manifest=Get-Content -LiteralPath (Join-Path $folder 'osat.json') -Raw | ConvertFrom-Json
$generations=Get-Content -LiteralPath (Join-Path $folder 'generoidut.json') -Raw | ConvertFrom-Json
$rawDir=Join-Path $folder 'natiivit';$processedDir=Join-Path $folder 'sovitetut';$rowDir=Join-Path $folder 'rivit'
New-Item -ItemType Directory -Path $rawDir,$processedDir,$rowDir -Force | Out-Null
$paths=@{};$stats=@()
foreach($entry in $generations){
 $match=[regex]::Match($entry.hint,'as (C:\\[^\r\n]+?\.png) by default')
 if(!$match.Success){throw ('Missing generated path '+$entry.id)}
 $paths[$entry.id]=$match.Groups[1].Value
}
foreach($tile in $manifest){
 if(!$paths.ContainsKey($tile.id)){throw ('Missing tile '+$tile.id)}
 $raw=Join-Path $rawDir ($tile.id+'.png');Copy-Item -LiteralPath $paths[$tile.id] -Destination $raw
 $im=[Drawing.Bitmap]::new($raw)
 $stats+=[pscustomobject]@{id=$tile.id;nativeWidth=$im.Width;nativeHeight=$im.Height;placedWidth=$tile.w;placedHeight=$tile.h}
 $im.Dispose()
 $target=Join-Path $processedDir ($tile.id+'.png')
 if(!(Test-Path -LiteralPath $target)){[WorksiteDetail]::Process($raw,$tile.reference,$target)}
 Write-Output ('Processed '+$tile.id)
}
$stats | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $folder 'lahdetarkkuudet.json') -Encoding utf8
foreach($row in 0..1){
 $tiles=@($manifest | Where-Object row -eq $row | Sort-Object col);$h=[int]$tiles[0].h
 $canvas=[byte[]]::new(5120*$h*4)
 foreach($tile in $tiles){
  $w=0;$th=0;$d=[WorksiteDetail]::Load((Join-Path $processedDir ($tile.id+'.png')),[ref]$w,[ref]$th)
  $overlap=if($tile.col -eq 0){0}else{320}
  [WorksiteDetail]::JoinX($canvas,5120,$h,$d,$w,$th,[int]$tile.x,$overlap)
 }
 [WorksiteDetail]::Save($canvas,5120,$h,(Join-Path $rowDir ('r'+$row+'.png')))
}
$canvas=[byte[]]::new(5120*1844*4)
foreach($row in 0..1){
 $w=0;$h=0;$d=[WorksiteDetail]::Load((Join-Path $rowDir ('r'+$row+'.png')),[ref]$w,[ref]$h)
 $y=if($row -eq 0){0}else{762};$overlap=if($row -eq 0){0}else{320}
 [WorksiteDetail]::JoinY($canvas,5120,1844,$d,$w,$h,$y,$overlap)
}
[WorksiteDetail]::Clean($canvas)
$old=[Drawing.Bitmap]::new((Join-Path $folder 'edellinen-katupala.png'))
$old.RotateFlip([Drawing.RotateFlipType]::RotateNoneFlipX)
$edge=[WorksiteDetail]::Read($old);$old.Dispose()
[WorksiteDetail]::EdgeCollar($canvas,$edge,5120,1844)
$final=Join-Path $folder 'vegas-tyomaakonttori-katu-01-5120x1844.png'
[WorksiteDetail]::Save($canvas,5120,1844,$final)
[WorksiteDetail]::Preview($final,(Join-Path $folder 'esikatselu.png'),1920,692)
Write-Output $final
