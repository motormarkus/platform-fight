$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$folder=Join-Path $PSScriptRoot 'vegas-yotaivas-ylajatko'
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$source=Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'Assets\Sprites\Taustat\vegas_taivas.png'
$sky=[Drawing.Bitmap]::new($source)
$small=Resize-Canvas $sky 1280 461
$guide=[Drawing.Bitmap]::new(1280,1043,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($guide);$g.Clear([Drawing.Color]::FromArgb(128,128,128));$g.DrawImage($small,[Drawing.Rectangle]::new(0,947,1280,96),[Drawing.Rectangle]::new(0,0,1280,96),[Drawing.GraphicsUnit]::Pixel);$g.Dispose()
$guide.Save((Join-Path $folder 'ylajatkon-taytto-ohje.png'),[Drawing.Imaging.ImageFormat]::Png)
$small.Save((Join-Path $folder 'alkuperainen-taivas-viite.png'),[Drawing.Imaging.ImageFormat]::Png)
$sky.Dispose();$small.Dispose();$guide.Dispose()
