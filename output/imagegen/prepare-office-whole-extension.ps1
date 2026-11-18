$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$folder=Join-Path $PSScriptRoot 'vegas-tyomaakonttori-jatko-ja-katto';[void][IO.Directory]::CreateDirectory($folder)
$source=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-tyomaakonttori-kaikki-ikkunat-tyhjat\vegas-tyomaakonttori-lasit-tyhjat-5120x1844.png'))
$guide=[Drawing.Bitmap]::new(2048,738,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($guide);$g.Clear([Drawing.Color]::Gray);$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy;$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($source,[Drawing.Rectangle]::new(0,369,1024,369));$g.Dispose();$source.Dispose()
$guide.Save((Join-Path $folder 'kokonaisuuden-ohje.png'),[Drawing.Imaging.ImageFormat]::Png);$guide.Dispose()
