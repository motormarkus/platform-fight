param([switch]$ProcessOnly)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'WorksiteDetail.cs') -ReferencedAssemblies System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'OfficeExtension.cs')
$folder=Join-Path $PSScriptRoot 'vegas-tyomaakonttori-jatko-ja-katto'
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
 $target=Join-Path $processedDir ($tile.id+'.png')
 if($tile.id -eq 'r0c5'){Copy-Item -LiteralPath $tile.reference -Destination $target;continue}
 if(!$paths.ContainsKey($tile.id)){if($ProcessOnly){continue};throw ('Missing tile '+$tile.id)}
 $raw=Join-Path $rawDir ($tile.id+'.png');Copy-Item -LiteralPath $paths[$tile.id] -Destination $raw
 $im=[Drawing.Bitmap]::new($raw)
 $stats+=[pscustomobject]@{id=$tile.id;nativeWidth=$im.Width;nativeHeight=$im.Height;placedWidth=$tile.w;placedHeight=$tile.h}
 $im.Dispose()
 if(!(Test-Path -LiteralPath $target)){[WorksiteDetail]::Process($raw,$tile.reference,$target)}
 Write-Output ('Processed '+$tile.id)
}
$stats | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $folder 'lahdetarkkuudet.json') -Encoding utf8
if($ProcessOnly){exit}
$base=[Drawing.Bitmap]::new((Join-Path $folder 'sommittelu-10240x3688.png'))
foreach($row in 0..3){
 $tiles=@($manifest | Where-Object row -eq $row | Sort-Object col);$h=[int]$tiles[0].h;$y0=[int]$tiles[0].y
 $crop=$base.Clone([Drawing.Rectangle]::new(0,$y0,10240,$h),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $canvas=[WorksiteDetail]::Read($crop);$crop.Dispose()
 foreach($tile in $tiles){
  $w=0;$th=0;$d=[WorksiteDetail]::Load((Join-Path $processedDir ($tile.id+'.png')),[ref]$w,[ref]$th)
  $overlap=if($tile.col -eq 0){0}else{320}
  [WorksiteDetail]::JoinX($canvas,10240,$h,$d,$w,$th,[int]$tile.x,$overlap)
 }
 [WorksiteDetail]::Save($canvas,10240,$h,(Join-Path $rowDir ('r'+$row+'.png')))
 Write-Output ('Joined row '+$row)
}
$base.Dispose()
$canvas=[byte[]]::new(10240*3688*4)
foreach($row in 0..3){
 $w=0;$h=0;$d=[WorksiteDetail]::Load((Join-Path $rowDir ('r'+$row+'.png')),[ref]$w,[ref]$h)
 $y=@(0,762,1684,2606)[$row];$overlap=if($row -eq 0){0}else{320}
 [WorksiteDetail]::JoinY($canvas,10240,3688,$d,$w,$h,$y,$overlap)
}
[WorksiteDetail]::Clean($canvas)
$oldpath=Join-Path $PSScriptRoot 'vegas-tyomaakonttori-kaikki-ikkunat-tyhjat/vegas-tyomaakonttori-lasit-tyhjat-5120x1844.png'
$ow=0;$oh=0;$old=[WorksiteDetail]::Load($oldpath,[ref]$ow,[ref]$oh)
[OfficeExtension]::RestoreAndCollar($canvas,10240,3688,$old,$ow,$oh)
$checks=[OfficeExtension]::Check($canvas,10240,$old,$ow,$oh)
$final=Join-Path $folder 'tyomaakonttori-kokonaisuus-10240x3688.png'
[WorksiteDetail]::Save($canvas,10240,3688,$final)
[WorksiteDetail]::Crop($final,(Join-Path $folder '02-katu-oikea-5120x1844.png'),5120,1844,5120,1844)
[WorksiteDetail]::Crop($final,(Join-Path $folder '03-yla-vasen-5120x1844.png'),0,0,5120,1844)
[WorksiteDetail]::Crop($final,(Join-Path $folder '04-yla-oikea-5120x1844.png'),5120,0,5120,1844)
[WorksiteDetail]::Preview($final,(Join-Path $folder 'esikatselu.png'),2048,738)
[pscustomobject]@{width=10240;height=3688;oldTileChangedChannels=$checks[0];upperBoundaryDifferentChannels=$checks[1];rightBoundaryDifferentChannels=$checks[2];detailSources=$stats.Count} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $folder 'tarkistus.json') -Encoding utf8
Write-Output $final
Get-Content -LiteralPath (Join-Path $folder 'tarkistus.json')
