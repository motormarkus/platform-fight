$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$work=Join-Path $PSScriptRoot 'vegas-baari-lava-tyo'
[void][System.IO.Directory]::CreateDirectory($work)
$source=[System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-baari-sisatilat-vasen\vegas-baari-sisatilat-vasen-5120x1844.png'))
$small=Resize-Canvas $source 1280 461
$guide=[System.Drawing.Bitmap]::new(1376,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($guide);$g.Clear([System.Drawing.Color]::FromArgb(128,128,128))
$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.DrawImage($small,[System.Drawing.Rectangle]::new(0,0,96,461),[System.Drawing.Rectangle]::new(1184,0,96,461),[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$guide.Save((Join-Path $work 'oikea-jatko-ohje.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$small.Dispose();$source.Dispose();$guide.Dispose()
Write-Output (Join-Path $work 'oikea-jatko-ohje.png')
