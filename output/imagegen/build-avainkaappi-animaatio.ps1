$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
Add-Type -Path (Join-Path $root 'key-cabinet-animation.cs')
$out=Join-Path $root 'avainkaappi-aukeaminen-10'
$work=Join-Path $root 'avainkaappi-animaatio-tyotiedostot'
$null=New-Item -ItemType Directory -Path $out,$work -Force
function Pixels([System.Drawing.Bitmap]$im){
 $r=[System.Drawing.Rectangle]::new(0,0,$im.Width,$im.Height)
 $bd=$im.LockBits($r,[System.Drawing.Imaging.ImageLockMode]::ReadOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $d=[byte[]]::new($bd.Stride*$im.Height);[System.Runtime.InteropServices.Marshal]::Copy($bd.Scan0,$d,0,$d.Length);$im.UnlockBits($bd);return ,$d
}
function Put-Pixels([System.Drawing.Bitmap]$im,[byte[]]$d){
 $r=[System.Drawing.Rectangle]::new(0,0,$im.Width,$im.Height)
 $bd=$im.LockBits($r,[System.Drawing.Imaging.ImageLockMode]::WriteOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 [System.Runtime.InteropServices.Marshal]::Copy($d,0,$bd.Scan0,$d.Length);$im.UnlockBits($bd)
}
$closed=[System.Drawing.Bitmap]::FromFile('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-a7f5c767-deea-4c88-98f3-51fb97e29226.png')
$opened=[System.Drawing.Bitmap]::FromFile('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-655b8e43-b788-458a-8eaf-2cd97d19f791.png')
$frontPixels=Pixels $closed;$openPixels=Pixels $opened
[KeyCabinetAnimation]::Clean($frontPixels);[KeyCabinetAnimation]::Clean($openPixels)
Put-Pixels $closed $frontPixels;Put-Pixels $opened $openPixels
$opened.Save((Join-Path $work 'avoin-alpha.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$body=[System.Drawing.Bitmap]::new(1493,1054,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($body)
$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attr=[System.Drawing.Imaging.ImageAttributes]::new();$attr.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
$g.DrawImage($closed,[System.Drawing.Rectangle]::new(344,79,1084,917),309,79,1091,917,[System.Drawing.GraphicsUnit]::Pixel,$attr)
# The fixed key compartment comes from one image in every frame.
$g.DrawImage($opened,[System.Drawing.Rectangle]::new(386,79,1042,917),386,79,1042,917,[System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($opened,[System.Drawing.Rectangle]::new(344,79,1084,35),344,79,1084,35,[System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($opened,[System.Drawing.Rectangle]::new(344,979,1084,18),344,979,1084,18,[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$attr.Dispose()
$basePixels=Pixels $body
[KeyCabinetAnimation]::Clean($basePixels);Put-Pixels $body $basePixels
$body.Save((Join-Path $work 'kiintea-runko.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$angles=@(0,12,24,36,48,60,72,84,96,[KeyCabinetAnimation]::EndAngle)
$sheet=[System.Drawing.Bitmap]::new(7680,2176,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$sheetG=[System.Drawing.Graphics]::FromImage($sheet);$sheetG.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$contact=[System.Drawing.Bitmap]::new(1920,544)
$contactG=[System.Drawing.Graphics]::FromImage($contact);$contactG.Clear([System.Drawing.Color]::FromArgb(60,69,73));$contactG.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$font=[System.Drawing.Font]::new('Arial',12)
$previewPaths=@();$entries=@();$errors=0
for($i=0;$i -lt 10;$i++){
 $d=[KeyCabinetAnimation]::Render($basePixels,$frontPixels,$openPixels,$angles[$i])
 $errors+=[KeyCabinetAnimation]::OutsideMotionDifferences($basePixels,$d)
 $native=[System.Drawing.Bitmap]::new(1493,1054,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb);Put-Pixels $native $d
 $frame=[System.Drawing.Bitmap]::new(1536,1088,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $fg=[System.Drawing.Graphics]::FromImage($frame);$fg.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy;$fg.DrawImageUnscaled($native,21,17);$fg.Dispose()
 $name='avainkaappi-{0:D2}.png' -f ($i+1)
 $frame.Save((Join-Path $out $name),[System.Drawing.Imaging.ImageFormat]::Png)
 $column=$i%5;$row=[int][Math]::Floor($i/5)
 $sheetG.DrawImageUnscaled($frame,$column*1536,$row*1088)
 $contactG.DrawImage($frame,[System.Drawing.Rectangle]::new($column*384,$row*272,384,272))
 $contactG.DrawString(($i+1).ToString(),$font,[System.Drawing.Brushes]::White,[single]($column*384+8),[single]($row*272+6))
 $preview=[System.Drawing.Bitmap]::new(768,544,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $pg=[System.Drawing.Graphics]::FromImage($preview);$pg.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy;$pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$pg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
 $pg.DrawImage($frame,[System.Drawing.Rectangle]::new(0,0,768,544));$pg.Dispose()
 $previewPath=Join-Path $work ('esikatselu-{0:D2}.png' -f ($i+1));$preview.Save($previewPath,[System.Drawing.Imaging.ImageFormat]::Png);$previewPaths+=$previewPath
 $entries+=@{file=$name;frame=$i+1;doorAngleDegrees=[Math]::Round($angles[$i],3);sheetX=$column*1536;sheetYTop=$row*1088}
 $preview.Dispose();$frame.Dispose();$native.Dispose()
}
if($errors -ne 0){throw "Stationary cabinet changed outside door motion: $errors"}
$sheetG.Dispose();$contactG.Dispose();$font.Dispose()
$sheet.Save((Join-Path $out 'avainkaappi-spritearkki-5x2.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$contact.Save((Join-Path $out 'ruudut-esikatselu.jpg'),[System.Drawing.Imaging.ImageFormat]::Jpeg)
[KeyCabinetAnimation]::WriteApng([string[]]$previewPaths,(Join-Path $out 'avautuminen-esikatselu.png'))
$metadata=[ordered]@{frameCount=10;frameWidth=1536;frameHeight=1088;sheetColumns=5;sheetRows=2;ordering='left to right, top row then bottom row';normalizedPivot=@(0.5,0.5);doorHingePixelFromTopLeft=@(406,561);recommendedFps=12;frames=$entries}
$metadata | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'animaatio.json') -Encoding utf8
@('AVAINKAAPPI - 10 RUUDUN AVAUTUMINEN','','01 = suljettu, 10 = avoin viitekuvan asentoon.','Jokainen PNG: 1536 x 1088, aito alpha-lapinakyvyys.','Kaapin sijainti ja ankkuripiste ovat samat kaikissa ruuduissa.','Spritearkki: 5 saraketta, 2 rivia; jarjestys vasemmalta oikealle, ylhaalta alas.','Kayta samaa pivotia jokaisessa ruudussa, esimerkiksi (0.5, 0.5).','12 fps on sopiva lahtokohta. Pysayta viimeiseen ruutuun; sulkeminen toimii kaanteisessa jarjestyksessa.','avautuminen-esikatselu.png on liikkuva APNG-esikatselu.','Oven etu- ja sisapinnan kuvamateriaalit ovat image_gen-tyokalulla valmisteltuja.','Runko ja avaimet on kiinnitetty yhteen tasoon; ovi on saranoitu ja kuvattu samoilla materiaaleilla kaikkiin ruutuihin.') | Set-Content -LiteralPath (Join-Path $out 'LUE-MINUT.txt') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $root 'avainkaappi-animaatio-generointiohjeet.txt') -Destination (Join-Path $out 'generointiohjeet.txt') -Force
$files=Get-ChildItem -LiteralPath $out -Filter 'avainkaappi-??.png' | Sort-Object Name
if($files.Count -ne 10){throw 'Expected exactly 10 frame PNGs'}
$hashes=@();foreach($file in $files){
 $im=[System.Drawing.Bitmap]::FromFile($file.FullName)
 if($im.Width -ne 1536 -or $im.Height -ne 1088){throw 'Frame size mismatch'}
 foreach($pt in @(@(0,0),@(1535,0),@(0,1087),@(1535,1087))){if($im.GetPixel($pt[0],$pt[1]).A -ne 0){throw 'Background corner not transparent'}}
 $im.Dispose();$hashes+=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
}
if(@($hashes | Sort-Object -Unique).Count -ne 10){throw 'Duplicate animation frame detected'}
$report=@('10 unique PNG frames; all 1536 x 1088.','All corners alpha=0; common canvas and pivot.','Stationary pixels outside door sweep: 0 changes.',"Final angle: $([Math]::Round([KeyCabinetAnimation]::EndAngle,3)) degrees",'Sprite sheet: 7680 x 2176; 5 columns x 2 rows.','Preview: 10-frame APNG, 768 x 544.')
$report | Set-Content -LiteralPath (Join-Path $out 'tarkistus.txt')
$report
$sheet.Dispose();$contact.Dispose();$body.Dispose();$closed.Dispose();$opened.Dispose()
