$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'WorksiteDetail.cs') -ReferencedAssemblies System.Drawing
$folder=Join-Path $PSScriptRoot 'vegas-konttori-kanttiini-sisatilat'
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
 $stats+=[pscustomobject]@{id=$tile.id;nativeWidth=$im.Width;nativeHeight=$im.Height;placedWidth=$tile.w;placedHeight=$tile.h};$im.Dispose()
 $target=Join-Path $processedDir ($tile.id+'.png')
 if(!(Test-Path -LiteralPath $target)){[WorksiteDetail]::Process($raw,$tile.reference,$target)}
 Write-Output ('Processed '+$tile.id)
}
$stats | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $folder 'lahdetarkkuudet.json') -Encoding utf8
foreach($row in 0..1){
 $tiles=@($manifest | Where-Object row -eq $row | Sort-Object col);$h=[int]$tiles[0].h
 $canvas=[byte[]]::new(10240*$h*4)
 foreach($tile in $tiles){
  $w=0;$th=0;$d=[WorksiteDetail]::Load((Join-Path $processedDir ($tile.id+'.png')),[ref]$w,[ref]$th)
  $overlap=if($tile.col -eq 0){0}else{320}
  [WorksiteDetail]::JoinX($canvas,10240,$h,$d,$w,$th,[int]$tile.x,$overlap)
 }
 [WorksiteDetail]::Save($canvas,10240,$h,(Join-Path $rowDir ('r'+$row+'.png')))
}
$canvas=[byte[]]::new(10240*1844*4)
foreach($row in 0..1){
 $w=0;$h=0;$d=[WorksiteDetail]::Load((Join-Path $rowDir ('r'+$row+'.png')),[ref]$w,[ref]$h)
 $y=if($row -eq 0){0}else{762};$overlap=if($row -eq 0){0}else{320}
 [WorksiteDetail]::JoinY($canvas,10240,1844,$d,$w,$h,$y,$overlap)
}
[WorksiteDetail]::Clean($canvas)
$final=Join-Path $folder 'kanttiini-kokonaisuus-10240x1844.png'
[WorksiteDetail]::Save($canvas,10240,1844,$final)
[WorksiteDetail]::Crop($final,(Join-Path $folder '01-kanttiini-vasen-5120x1844.png'),0,0,5120,1844)
[WorksiteDetail]::Crop($final,(Join-Path $folder '02-kanttiini-oikea-portaat-5120x1844.png'),5120,0,5120,1844)
[WorksiteDetail]::Preview($final,(Join-Path $folder 'esikatselu.png'),2560,461)
[WorksiteDetail]::Crop($final,(Join-Path $folder 'liitos-100prosenttia.png'),4800,0,640,1844)
$src=[Drawing.Bitmap]::new($final)
$sheet=[Drawing.Bitmap]::new(1800,1000,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($sheet);$g.Clear([Drawing.Color]::FromArgb(255,35,42,55))
$rects=@(@(180,300),@(3500,300),@(9200,100),@(4800,700),@(2000,1100),@(7000,1100))
for($i=0;$i -lt 6;$i++){$r=$rects[$i];$g.DrawImage($src,[Drawing.Rectangle]::new(($i%3)*600,[Math]::Floor($i/3)*500,600,500),[int]$r[0],[int]$r[1],600,500,[Drawing.GraphicsUnit]::Pixel)}
$g.Dispose();$sheet.Save((Join-Path $folder 'yksityiskohdat-100prosenttia.png'),[Drawing.Imaging.ImageFormat]::Png);$sheet.Dispose();$src.Dispose()
$result=@()
foreach($name in @('01-kanttiini-vasen-5120x1844.png','02-kanttiini-oikea-portaat-5120x1844.png')){
 $path=Join-Path $folder $name;$im=[Drawing.Bitmap]::new($path)
 $result+=[pscustomobject]@{file=$name;width=$im.Width;height=$im.Height;bytes=(Get-Item -LiteralPath $path).Length};$im.Dispose()
}
$result | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $folder 'tarkistus.json') -Encoding utf8
$result | Format-Table -AutoSize
