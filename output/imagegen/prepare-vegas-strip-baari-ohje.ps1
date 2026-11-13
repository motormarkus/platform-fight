$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$work=Join-Path $PSScriptRoot 'vegas-strip-baari-tyo'
[void][System.IO.Directory]::CreateDirectory($work)
$source=[System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-strip-kasino-ulkoa-v2\vegas-strip-kasino-ulkoa-v2-5120x1844.png'))
$small=Resize-Canvas $source 1280 461
$guide=[System.Drawing.Bitmap]::new(1376,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($guide);$g.Clear([System.Drawing.Color]::FromArgb(128,128,128))
$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.DrawImage($small,[System.Drawing.Rectangle]::new(1280,0,96,461),[System.Drawing.Rectangle]::new(0,0,96,461),[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$guide.Save((Join-Path $work 'vasen-jatko-ohje.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$small.Dispose();$source.Dispose();$guide.Dispose()
Write-Output (Join-Path $work 'vasen-jatko-ohje.png')
