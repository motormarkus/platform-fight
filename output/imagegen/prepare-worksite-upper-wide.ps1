param([ValidateSet('middle','left')][string]$Part='middle')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$folder=Join-Path $PSScriptRoot 'vegas-strip-tyomaa-ylatasot'
[void][IO.Directory]::CreateDirectory($folder)
$work=Join-Path $folder ('tyo-'+$Part);[void][IO.Directory]::CreateDirectory($work)
$oldUpperPath=Join-Path $PSScriptRoot 'vegas-strip-tyomaa-ylajatko\vegas-strip-tyomaa-ylajatko-5120x1844.png'
if($Part -eq 'middle'){
 Copy-Item -LiteralPath $oldUpperPath -Destination (Join-Path $folder '03-paaty-5120x1844.png')
 $belowPath=Join-Path $PSScriptRoot 'vegas-strip-tyomaa-jatko\vegas-strip-tyomaa-jatko-5120x1844.png'
 $rightPath=Join-Path $folder '03-paaty-5120x1844.png'
 $cornerPath=Join-Path $PSScriptRoot 'vegas-strip-tyomaa-paaty\vegas-strip-tyomaa-paaty-5120x1844.png'
}else{
 $belowPath=Join-Path $PSScriptRoot 'vegas-strip-tyomaa\vegas-strip-tyomaa-5120x1844.png'
 $rightPath=Join-Path $folder '02-keskiosa-5120x1844.png'
 $cornerPath=Join-Path $PSScriptRoot 'vegas-strip-tyomaa-jatko\vegas-strip-tyomaa-jatko-5120x1844.png'
}
$below=[Drawing.Bitmap]::new($belowPath);$right=[Drawing.Bitmap]::new($rightPath);$corner=[Drawing.Bitmap]::new($cornerPath)
$bs=Resize-Canvas $below 1280 461;$rs=Resize-Canvas $right 1280 461;$cs=Resize-Canvas $corner 1280 461
$bs.Save((Join-Path $work 'alapuolinen-viite.png'),[Drawing.Imaging.ImageFormat]::Png)
$rs.Save((Join-Path $work 'oikean-ylapalan-viite.png'),[Drawing.Imaging.ImageFormat]::Png)
$guide=[Drawing.Bitmap]::new(1376,557,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($guide);$g.Clear([Drawing.Color]::FromArgb(128,128,128));$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.DrawImage($rs,[Drawing.Rectangle]::new(1280,0,96,461),[Drawing.Rectangle]::new(0,0,96,461),[Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($bs,[Drawing.Rectangle]::new(0,461,1280,96),[Drawing.Rectangle]::new(0,0,1280,96),[Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($cs,[Drawing.Rectangle]::new(1280,461,96,96),[Drawing.Rectangle]::new(0,0,96,96),[Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$guide.Save((Join-Path $work 'taytettava-ylapala-ohje.png'),[Drawing.Imaging.ImageFormat]::Png)
foreach($im in @($below,$right,$corner,$bs,$rs,$cs,$guide)){$im.Dispose()}
Write-Output $work
