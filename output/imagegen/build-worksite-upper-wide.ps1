param([ValidateSet('middle','left')][string]$Part='middle')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
. (Join-Path $PSScriptRoot 'worksite-upper-registration.ps1')
$folder=Join-Path $PSScriptRoot 'vegas-strip-tyomaa-ylatasot';$work=Join-Path $folder ('tyo-'+$Part)
$config=Get-Content -LiteralPath (Join-Path $work 'registration.json') -Raw | ConvertFrom-Json
$belowName=if($Part -eq 'middle'){'vegas-strip-tyomaa-jatko'}else{'vegas-strip-tyomaa'}
$rightName=if($Part -eq 'middle'){'03-paaty'}else{'02-keskiosa'}
$outName=if($Part -eq 'middle'){'02-keskiosa'}else{'01-alku'}
$raw=[Drawing.Bitmap]::new((Join-Path $work 'raaka-5120x1844.png'))
$below=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot "$belowName\$belowName-5120x1844.png"))
$right=[Drawing.Bitmap]::new((Join-Path $folder "$rightName-5120x1844.png"))
$bd=Pixel-Data $below;$rd=Pixel-Data $right
$previousData=[byte[]]::new(0)
if($Part -eq 'middle'){$previous=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-strip-tyomaa\vegas-strip-tyomaa-5120x1844.png'));$previousData=Pixel-Data $previous;$previous.Dispose()}
$manual=[double[]]::new(0)
if($null -ne $config.manual){$manual=[double[]]$config.manual}
$data=[WorksiteUpperRegistration]::Build((Pixel-Data $raw),$bd,$rd,$previousData,[double[]]$config.sy,[double[]]$config.dy,[double[]]$config.sx,[double[]]$config.dx,[int]$config.bayTop,[int]$config.emptyLeft,$manual)
$null=[CasinoLeftPixels]::Normalize($data)
[Buffer]::BlockCopy($bd,0,$data,1843*5120*4,5120*4)
for($y=0;$y -lt 1844;$y++){[Buffer]::BlockCopy($rd,$y*5120*4,$data,($y*5120+5119)*4,4)}
$final=[Drawing.Bitmap]::new(5120,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb);Set-PixelData $final $data
$final.Save((Join-Path $folder "$outName-5120x1844.png"),[Drawing.Imaging.ImageFormat]::Png)
$small=Resize-Canvas $final 1280 461;$small.Save((Join-Path $folder "$outName-esikatselu.png"),[Drawing.Imaging.ImageFormat]::Png)
$bs=Resize-Canvas $below 1280 461;$rs=Resize-Canvas $right 1280 461
$p=[Drawing.Bitmap]::new(1280,922,[Drawing.Imaging.PixelFormat]::Format32bppArgb);$g=[Drawing.Graphics]::FromImage($p);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImageUnscaled($small,0,0);$g.DrawImageUnscaled($bs,0,461);$g.Dispose();$p.Save((Join-Path $work 'pystykohdistus.png'),[Drawing.Imaging.ImageFormat]::Png);$p.Dispose()
$p=[Drawing.Bitmap]::new(2560,461,[Drawing.Imaging.PixelFormat]::Format32bppArgb);$g=[Drawing.Graphics]::FromImage($p);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImageUnscaled($small,0,0);$g.DrawImageUnscaled($rs,1280,0);$g.Dispose();$p.Save((Join-Path $work 'vaakakohdistus.png'),[Drawing.Imaging.ImageFormat]::Png);$p.Dispose()
foreach($im in @($raw,$below,$right,$final,$small,$bs,$rs)){$im.Dispose()}
Write-Output "$outName exported"
