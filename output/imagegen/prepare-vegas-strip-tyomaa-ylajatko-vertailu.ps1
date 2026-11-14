$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$work=Join-Path $PSScriptRoot 'vegas-strip-tyomaa-ylajatko-tyo'
$raw=[Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-290e20e8-f187-423b-a016-9f7a5a55a905.png')
$null=Clean-Alpha $raw
$dst=[Drawing.Bitmap]::new(5120,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($dst);$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy;$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$a=[Drawing.Imaging.ImageAttributes]::new();$a.SetWrapMode([Drawing.Drawing2D.WrapMode]::TileFlipXY)
$cut=$raw.Height*461.0/557.0
$g.DrawImage($raw,[Drawing.Rectangle]::new(0,0,5120,1844),[single]0,[single]0,[single]$raw.Width,[single]$cut,[Drawing.GraphicsUnit]::Pixel,$a);$g.Dispose();$a.Dispose()
$null=Clean-Alpha $dst
$dst.Save((Join-Path $work 'yla-raaka-5120x1844.png'),[Drawing.Imaging.ImageFormat]::Png)
$small=Resize-Canvas $dst 1280 461;$small.Save((Join-Path $work 'yla-raaka-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$small.Dispose()
$lower=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-strip-tyomaa-paaty\vegas-strip-tyomaa-paaty-5120x1844.png'))
foreach($item in @(@($dst,1843,'upper'),@($lower,0,'lower'))){$im=$item[0];$y=$item[1];$tag=$item[2];$runs=@();$inside=$false;for($x=0;$x -lt 5120;$x++){$v=$im.GetPixel($x,$y).A -gt 128;if($v -and !$inside){$start=$x;$inside=$true};if(!$v -and $inside){$runs+="$start-$($x-1)";$inside=$false}};if($inside){$runs+="$start-5119"};Write-Output "$tag alpha runs=$($runs -join ',')"}
$pair=[Drawing.Bitmap]::new(1280,922,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($pair);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImage($dst,[Drawing.Rectangle]::new(0,0,1280,461));$g.DrawImage($lower,[Drawing.Rectangle]::new(0,461,1280,461));$g.Dispose()
$pair.Save((Join-Path $work 'pysty-raaka-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
Write-Output "Native=$($raw.Width)x$($raw.Height), crop upper=$cut"
$pair.Dispose();$lower.Dispose();$dst.Dispose();$raw.Dispose()
