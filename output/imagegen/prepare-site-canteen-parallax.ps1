$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$folder=Join-Path $PSScriptRoot 'vegas-tyomaakonttori-ruokala-parallaksi'
[void][IO.Directory]::CreateDirectory($folder)
$source=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-tyomaakonttori-katu-01\vegas-tyomaakonttori-katu-01-5120x1844.png'))
$crop=$source.Clone([Drawing.Rectangle]::new(2600,400,2520,680),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$crop.Save((Join-Path $folder 'ikkunat-lahikuva.png'),[Drawing.Imaging.ImageFormat]::Png)
$crop.Dispose()
$preview=Resize-Canvas $source 1920 692
$preview.Save((Join-Path $folder 'katukuva-viite.png'),[Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
$crop=$source.Clone([Drawing.Rectangle]::new(2500,350,2620,800),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$crop.Save((Join-Path $folder 'tausta-muokkausviite.png'),[Drawing.Imaging.ImageFormat]::Png)
$crop.Dispose();$source.Dispose()
Write-Output $folder
