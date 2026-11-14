param([ValidateSet('middle','left')][string]$Part='middle',[string]$RawPath)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$folder=Join-Path $PSScriptRoot 'vegas-strip-tyomaa-ylatasot';$work=Join-Path $folder ('tyo-'+$Part)
$raw=[Drawing.Bitmap]::new($RawPath);$null=Clean-Alpha $raw
$dst=[Drawing.Bitmap]::new(5120,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($dst);$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy;$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$a=[Drawing.Imaging.ImageAttributes]::new();$a.SetWrapMode([Drawing.Drawing2D.WrapMode]::TileFlipXY)
$g.DrawImage($raw,[Drawing.Rectangle]::new(0,0,5120,1844),[single]0,[single]0,[single]($raw.Width*1280/1376),[single]($raw.Height*461/557),[Drawing.GraphicsUnit]::Pixel,$a);$g.Dispose();$a.Dispose();$null=Clean-Alpha $dst
$dst.Save((Join-Path $work 'raaka-5120x1844.png'),[Drawing.Imaging.ImageFormat]::Png)
$belowName=if($Part -eq 'middle'){'vegas-strip-tyomaa-jatko'}else{'vegas-strip-tyomaa'}
$rightName=if($Part -eq 'middle'){'03-paaty'}else{'02-keskiosa'}
$below=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot "$belowName\$belowName-5120x1844.png"))
$right=[Drawing.Bitmap]::new((Join-Path $folder "$rightName-5120x1844.png"))
$small=Resize-Canvas $dst 1280 461;$small.Save((Join-Path $work 'raaka-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
$rightSmall=Resize-Canvas $right 1280 461
$pair=[Drawing.Bitmap]::new(2560,461,[Drawing.Imaging.PixelFormat]::Format32bppArgb);$g=[Drawing.Graphics]::FromImage($pair);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImageUnscaled($small,0,0);$g.DrawImageUnscaled($rightSmall,1280,0);$g.Dispose();$pair.Save((Join-Path $work 'vaakaliitos-raaka.png'),[Drawing.Imaging.ImageFormat]::Png)
$belowSmall=Resize-Canvas $below 1280 461
$vertical=[Drawing.Bitmap]::new(1280,922,[Drawing.Imaging.PixelFormat]::Format32bppArgb);$g=[Drawing.Graphics]::FromImage($vertical);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImageUnscaled($small,0,0);$g.DrawImageUnscaled($belowSmall,0,461);$g.Dispose();$vertical.Save((Join-Path $work 'pystyliitos-raaka.png'),[Drawing.Imaging.ImageFormat]::Png)
foreach($entry in @(@($dst,1843,'raw'),@($below,0,'below'))){$im=$entry[0];$y=$entry[1];$inside=$false;$runs=@();for($x=0;$x -lt 5120;$x++){$yes=$im.GetPixel($x,$y).A -gt 128;if($yes -and !$inside){$start=$x;$inside=$true};if(!$yes -and $inside){$runs+="$start-$($x-1)";$inside=$false}};if($inside){$runs+="$start-5119"};Write-Output "$($entry[2]) bottom/top alpha=$($runs -join ',')"}
foreach($entry in @(@($dst,5119,'raw-right'),@($right,0,'right-left'))){$im=$entry[0];$x=$entry[1];$inside=$false;$runs=@();for($y=0;$y -lt 1844;$y++){$yes=$im.GetPixel($x,$y).A -gt 128;if($yes -and !$inside){$start=$y;$inside=$true};if(!$yes -and $inside){$runs+="$start-$($y-1)";$inside=$false}};if($inside){$runs+="$start-1843"};Write-Output "$($entry[2]) alpha=$($runs -join ',')"}
Write-Output "Native=$($raw.Width)x$($raw.Height)"
foreach($im in @($raw,$dst,$below,$right,$small,$rightSmall,$belowSmall,$pair,$vertical)){$im.Dispose()}
