$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$outDir=Join-Path $PSScriptRoot 'vegas-baari-wc'
$work=Join-Path $PSScriptRoot 'vegas-baari-wc-tyo'
[void][System.IO.Directory]::CreateDirectory($outDir)
[void][System.IO.Directory]::CreateDirectory($work)
$source=[System.Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-dca45114-3409-4b53-8e68-ab6ba161b44f.png')
$native='{0}x{1}' -f $source.Width,$source.Height
$left=Resize-Canvas $source 5120 1844
$left.Save((Join-Path $outDir 'vegas-baari-wc-vasen-5120x1844.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$small=Resize-Canvas $left 1280 461
$small.Save((Join-Path $outDir 'vasen-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$guide=[System.Drawing.Bitmap]::new(1376,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($guide);$g.Clear([System.Drawing.Color]::FromArgb(128,128,128));$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.DrawImage($small,[System.Drawing.Rectangle]::new(0,0,96,461),[System.Drawing.Rectangle]::new(1184,0,96,461),[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$guide.Save((Join-Path $work 'oikea-jatko-ohje.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$guide.Dispose();$small.Dispose();$left.Dispose();$source.Dispose()
Write-Output "Left native=$native; final=5120x1844"
