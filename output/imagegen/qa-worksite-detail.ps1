$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'WorksiteDetail.cs') -ReferencedAssemblies System.Drawing
$folder=Join-Path $PSScriptRoot 'vegas-tyomaa-tarkka-v2'
$qa=Join-Path $folder 'tarkistus'
New-Item -ItemType Directory -Path $qa -Force | Out-Null
$final=Join-Path $folder 'vegas-tyomaa-tarkka-15360x5632.png'
$old=Join-Path $PSScriptRoot 'vegas-tyomaa-yhtena\vegas-tyomaa-yhtenainen-15360x5632.png'
$im=[Drawing.Bitmap]::new($final)
$prev=[Drawing.Bitmap]::new($old)
$font=[Drawing.Font]::new('Arial',18,[Drawing.FontStyle]::Bold)
$brush=[Drawing.SolidBrush]::new([Drawing.Color]::White)
$compare=[Drawing.Bitmap]::new(2048,1072)
$g=[Drawing.Graphics]::FromImage($compare);$g.Clear([Drawing.Color]::FromArgb(33,40,52))
$g.DrawString('VANHA - 100 %', $font,$brush,10,8)
$g.DrawString('UUSI - 100 %', $font,$brush,1034,8)
$g.DrawImage($prev,[Drawing.Rectangle]::new(0,48,1024,1024),[Drawing.Rectangle]::new(5800,4190,1024,1024),[Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($im,[Drawing.Rectangle]::new(1024,48,1024,1024),[Drawing.Rectangle]::new(5800,4190,1024,1024),[Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$compare.Save((Join-Path $folder 'tarkkuusvertailu-100prosenttia.png'),[Drawing.Imaging.ImageFormat]::Png);$compare.Dispose()
# Full resolution seam samples: each inset is an unscaled crop.
foreach($item in @(@('katto',300),@('ylataso',1650),@('keskitaso',3200),@('katutaso',4700))){
 $contact=[Drawing.Bitmap]::new(7*512,560)
 $g=[Drawing.Graphics]::FromImage($contact);$g.Clear([Drawing.Color]::FromArgb(33,40,52))
 foreach($c in 1..7){
  $g.DrawString(('x='+($c*1920)),$font,$brush,($c-1)*512+8,8)
  $g.DrawImage($im,[Drawing.Rectangle]::new(($c-1)*512,48,512,512),[Drawing.Rectangle]::new($c*1920-256,[int]$item[1],512,512),[Drawing.GraphicsUnit]::Pixel)
 }
 $g.Dispose();$contact.Save((Join-Path $qa ('pystyliitokset-'+$item[0]+'.png')),[Drawing.Imaging.ImageFormat]::Png);$contact.Dispose()
}
foreach($r in 1..3){
 $contact=[Drawing.Bitmap]::new(6*512,560)
 $g=[Drawing.Graphics]::FromImage($contact);$g.Clear([Drawing.Color]::FromArgb(33,40,52))
 $xs=@(1350,2050,3500,7100,7850,14000)
 for($c=0;$c -lt $xs.Count;$c++){
  $g.DrawString(('x='+$xs[$c]+' y='+($r*1408)),$font,$brush,$c*512+8,8)
  $g.DrawImage($im,[Drawing.Rectangle]::new($c*512,48,512,512),[Drawing.Rectangle]::new($xs[$c]-256,$r*1408-256,512,512),[Drawing.GraphicsUnit]::Pixel)
 }
 $g.Dispose();$contact.Save((Join-Path $qa ('vaakaliitokset-'+$r+'.png')),[Drawing.Imaging.ImageFormat]::Png);$contact.Dispose()
}
$edgeDiff=0
for($y=0;$y -lt $im.Height;$y++){if($im.GetPixel(0,$y).ToArgb() -ne $prev.GetPixel(0,$y).ToArgb()){$edgeDiff++}}
$topOpaque=0;$bottomClear=0
for($x=0;$x -lt $im.Width;$x++){if($im.GetPixel($x,0).A -gt 0){$topOpaque++};if($im.GetPixel($x,$im.Height-1).A -lt 255){$bottomClear++}}
$stats=Get-Content -LiteralPath (Join-Path $folder 'lahdetarkkuudet.json') -Raw | ConvertFrom-Json
$nativePixels=($stats | ForEach-Object {$_.nativeWidth*$_.nativeHeight} | Measure-Object -Sum).Sum
$report=[pscustomobject]@{
 output=$final;width=$im.Width;height=$im.Height;bytes=(Get-Item -LiteralPath $final).Length;
 tileCount=$stats.Count;nativeSourcePixelsIncludingOverlaps=$nativePixels;
 leftEdgeDifferentPixels=$edgeDiff;topBorderNontransparentPixels=$topOpaque;bottomBorderNonopaquePixels=$bottomClear;
 note='32 locally redrawn native images, registered and resampled into the final canvas. Not a single native 15K generation. PNG alpha preserved. Source detail greatly exceeds the former 2172x724 whole-scene source.'
}
$report | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $folder 'tarkistus.json') -Encoding utf8
$im.Dispose();$prev.Dispose();$font.Dispose();$brush.Dispose()
$report | ConvertTo-Json -Depth 4

