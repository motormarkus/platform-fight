$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$folder=Join-Path $PSScriptRoot 'vegas-tyomaakonttori-katu-01'
$old=[Drawing.Bitmap]::new((Join-Path $folder 'edellinen-katupala.png'))
$name='vegas-tyomaakonttori-katu-01-5120x1844.png'
$im=[Drawing.Bitmap]::new((Join-Path $folder $name))
$different=0;$floorHoles=0
foreach($y in 0..1843){if($old.GetPixel(5119,$y).ToArgb() -ne $im.GetPixel(0,$y).ToArgb()){$different++}}
foreach($y in 1450..1843){foreach($x in @(0,512,1024,1536,2048,2560,3072,3584,4096,4608,5119)){if($im.GetPixel($x,$y).A -ne 255){$floorHoles++}}}
$joined=[Drawing.Bitmap]::new(10240,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($joined);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImageUnscaled($old,0,0);$g.DrawImageUnscaled($im,5120,0);$g.Dispose()
$preview=Resize-Canvas $joined 2560 461;$preview.Save((Join-Path $folder 'katuliitos-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$preview.Dispose();$joined.Dispose()
$seam=[Drawing.Bitmap]::new(1024,800,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($seam);$g.Clear([Drawing.Color]::FromArgb(34,40,58))
$g.DrawImage($old,[Drawing.Rectangle]::new(0,0,512,800),[Drawing.Rectangle]::new(4608,1044,512,800),[Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($im,[Drawing.Rectangle]::new(512,0,512,800),[Drawing.Rectangle]::new(0,1044,512,800),[Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$seam.Save((Join-Path $folder 'katuliitos-100prosenttia.png'),[Drawing.Imaging.ImageFormat]::Png);$seam.Dispose()
$detail=[Drawing.Bitmap]::new(2048,768,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($detail);$g.Clear([Drawing.Color]::FromArgb(34,40,58))
$g.DrawImage($im,[Drawing.Rectangle]::new(0,0,1024,768),[Drawing.Rectangle]::new(1350,500,1024,768),[Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($im,[Drawing.Rectangle]::new(1024,0,1024,768),[Drawing.Rectangle]::new(3000,500,1024,768),[Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$detail.Save((Join-Path $folder 'yksityiskohdat-100prosenttia.png'),[Drawing.Imaging.ImageFormat]::Png);$detail.Dispose()
$qa=[pscustomobject]@{width=$im.Width;height=$im.Height;bytes=(Get-Item -LiteralPath (Join-Path $folder $name)).Length;leftEdgeDifferentPixels=$different;sampledRoadAlphaHoles=$floorHoles;detailTiles=6;alignment='Bottom aligned to 15360x5632 worksite; y offset 3788; world x offset 15360.'}
$qa | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $folder 'tarkistus.json') -Encoding utf8
$old.Dispose();$im.Dispose();$qa | Format-List
if($different -gt 0 -or $floorHoles -gt 0){throw 'QA edge/alpha mismatch'}
