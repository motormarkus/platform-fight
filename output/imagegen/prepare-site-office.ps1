$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$folder=Join-Path $PSScriptRoot 'vegas-tyomaakonttori-katu-01'
[void][IO.Directory]::CreateDirectory($folder)
$source=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-tyomaa-tarkka-v2\vegas-tyomaa-tarkka-15360x5632.png'))
$street=$source.Clone([Drawing.Rectangle]::new(10240,3788,5120,1844),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$street.Save((Join-Path $folder 'edellinen-katupala.png'),[Drawing.Imaging.ImageFormat]::Png)
$small=Resize-Canvas $street 1280 461
$small.Save((Join-Path $folder 'katutason-tyyliviite.png'),[Drawing.Imaging.ImageFormat]::Png)
$guide=[Drawing.Bitmap]::new(1376,461,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($guide);$g.Clear([Drawing.Color]::Gray);$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.DrawImage($small,[Drawing.Rectangle]::new(0,0,96,461),[Drawing.Rectangle]::new(1184,0,96,461),[Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$guide.Save((Join-Path $folder 'jatko-ohje.png'),[Drawing.Imaging.ImageFormat]::Png)
$guide.Dispose();$small.Dispose();$street.Dispose();$source.Dispose()
Write-Output $folder
