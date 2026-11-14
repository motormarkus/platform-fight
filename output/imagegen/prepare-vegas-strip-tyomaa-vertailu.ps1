$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$work=Join-Path $PSScriptRoot 'vegas-strip-tyomaa-tyo'
$raw=[Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-60364cdc-7dd4-4b28-aed5-a077002c5bb4.png')
$native="$($raw.Width)x$($raw.Height)"
$null=Clean-Alpha $raw
$out=[Drawing.Bitmap]::new(5120,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($out)
$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$a=[Drawing.Imaging.ImageAttributes]::new();$a.SetWrapMode([Drawing.Drawing2D.WrapMode]::TileFlipXY)
$cut=$raw.Width*96.0/1376.0
$g.DrawImage($raw,[Drawing.Rectangle]::new(0,0,5120,1844),[single]$cut,[single]0,[single]($raw.Width-$cut),[single]$raw.Height,[Drawing.GraphicsUnit]::Pixel,$a)
$g.Dispose();$a.Dispose()
$null=Clean-Alpha $out
$out.Save((Join-Path $work 'tyomaa-raaka-5120x1844.png'),[Drawing.Imaging.ImageFormat]::Png)
$old=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-strip-haakappeli\vegas-strip-haakappeli-5120x1844.png'))
$oldSmall=Resize-Canvas $old 1280 461;$small=Resize-Canvas $out 1280 461
$small.Save((Join-Path $work 'tyomaa-raaka-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
$pair=[Drawing.Bitmap]::new(2560,461,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($pair);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImageUnscaled($oldSmall,0,0);$g.DrawImageUnscaled($small,1280,0);$g.Dispose()
$pair.Save((Join-Path $work 'liitos-raaka-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
$close=[Drawing.Bitmap]::new(1024,804,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($close);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImage($old,[Drawing.Rectangle]::new(0,0,512,804),[Drawing.Rectangle]::new(4608,1040,512,804),[Drawing.GraphicsUnit]::Pixel);$g.DrawImage($out,[Drawing.Rectangle]::new(512,0,512,804),[Drawing.Rectangle]::new(0,1040,512,804),[Drawing.GraphicsUnit]::Pixel);$g.Dispose()
$close.Save((Join-Path $work 'liitos-raaka-lahikuva.png'),[Drawing.Imaging.ImageFormat]::Png)
$out.Dispose();$raw.Dispose();$old.Dispose();$small.Dispose();$oldSmall.Dispose();$pair.Dispose();$close.Dispose()
Write-Output "Native=$native, guide crop=$cut"
