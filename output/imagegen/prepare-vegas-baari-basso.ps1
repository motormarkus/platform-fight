$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$work=Join-Path $PSScriptRoot 'vegas-baari-basso-tyo'
[void][IO.Directory]::CreateDirectory($work)
$source=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-baari-bakkari-v2\vegas-baari-bakkari-v2-5120x1844.png'))
$crop=$source.Clone([Drawing.Rectangle]::new(1120,140,1320,900),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$crop.Save((Join-Path $work 'julisteiden-vali-muokkausviite.png'),[Drawing.Imaging.ImageFormat]::Png)
$crop.Dispose();$source.Dispose()
