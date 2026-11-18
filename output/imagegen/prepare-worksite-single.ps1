$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$folder=Join-Path $PSScriptRoot 'vegas-tyomaa-yhtena'
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$old=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-strip-tyomaa-ylatasot\koko-tyomaa-esikatselu.png'))
$guide=[Drawing.Bitmap]::new(3840,1152,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($guide);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImageUnscaled($old,0,230);$g.Dispose()
$guide.Save((Join-Path $folder 'sommitteluviite-kattovaralla.png'),[Drawing.Imaging.ImageFormat]::Png)
$ground=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-strip-tyomaa\vegas-strip-tyomaa-5120x1844.png'))
$street=$ground.Clone([Drawing.Rectangle]::new(0,1060,768,784),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$street.Save((Join-Path $folder 'vasen-katuviite.png'),[Drawing.Imaging.ImageFormat]::Png)
$old.Dispose();$guide.Dispose();$ground.Dispose();$street.Dispose()
