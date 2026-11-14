$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$work=Join-Path $PSScriptRoot 'vegas-strip-tyomaa-paaty-tyo'
[void][IO.Directory]::CreateDirectory($work)
$source=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-strip-tyomaa-jatko\vegas-strip-tyomaa-jatko-5120x1844.png'))
$small=Resize-Canvas $source 1280 461
$guide=[Drawing.Bitmap]::new(1376,461,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($guide);$g.Clear([Drawing.Color]::FromArgb(128,128,128));$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.DrawImage($small,[Drawing.Rectangle]::new(0,0,96,461),[Drawing.Rectangle]::new(1184,0,96,461),[Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$guide.Save((Join-Path $work 'oikea-jatko-ohje.png'),[Drawing.Imaging.ImageFormat]::Png)
$crop=$source.Clone([Drawing.Rectangle]::new(4352,0,768,1844),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$detail=Resize-Canvas $crop 384 922
$detail.Save((Join-Path $work 'liitosreuna-lahikuva.png'),[Drawing.Imaging.ImageFormat]::Png)
$detail.Dispose();$crop.Dispose();$small.Dispose();$source.Dispose();$guide.Dispose()
