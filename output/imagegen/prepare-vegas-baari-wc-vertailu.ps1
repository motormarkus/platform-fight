$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$work=Join-Path $PSScriptRoot 'vegas-baari-wc-tyo'
$old=[System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-baari-wc\vegas-baari-wc-vasen-5120x1844.png'))
$raw=[System.Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-dd641e9c-193b-4305-93d3-8f1d337fa31b.png')
Write-Output ('Native={0}x{1}' -f $raw.Width,$raw.Height)
$new=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($new);$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[System.Drawing.Imaging.ImageAttributes]::new();$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
$g.DrawImage($raw,[System.Drawing.Rectangle]::new(0,0,5120,1844),[single]($raw.Width*96/1376.0),[single]0,[single]($raw.Width*1280/1376.0),[single]$raw.Height,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$g.Dispose();$attrs.Dispose();$raw.Dispose()
$new.Save((Join-Path $work 'raaka-5120x1844.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$join=[System.Drawing.Bitmap]::new(1024,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($join)
$g.DrawImage($old,[System.Drawing.Rectangle]::new(0,0,512,1844),[System.Drawing.Rectangle]::new(4608,0,512,1844),[System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($new,[System.Drawing.Rectangle]::new(512,0,512,1844),[System.Drawing.Rectangle]::new(0,0,512,1844),[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$join.Save((Join-Path $work 'raaka-liitos.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$join.Dispose();$new.Dispose();$old.Dispose()
