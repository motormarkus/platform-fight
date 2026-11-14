$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$folder=Join-Path $PSScriptRoot 'kasino-lahiparallaksi-poydat-v2'
[void][System.IO.Directory]::CreateDirectory($folder)
$src=[System.Drawing.Bitmap]::FromFile('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-895841d2-b2b9-4819-ae82-2d84d10c59db.png')
$null=Clean-Alpha $src
$props=Resize-Canvas $src 5120 1844
$null=Clean-Alpha $props
$pd=Pixel-Data $props
$last=0
for($y=1843;$y -ge 0;$y--){$found=$false;for($x=0;$x -lt 5120;$x++){if($pd[4*($y*5120+$x)+3] -gt 16){$found=$true;break}};if($found){$last=$y;break}}
$shift=1335-$last
$bg=[System.Drawing.Bitmap]::FromFile((Join-Path $PSScriptRoot 'kasino-kaukotausta-pramea-v2\kasino-kaukotausta-pramea-v2-5120x1844.png'))
$floor=$bg.Clone([System.Drawing.Rectangle]::new(0,1199,5120,645),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$dst=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($dst)
$g.DrawImageUnscaled($floor,0,1199)
$g.DrawImageUnscaled($props,0,$shift)
$g.Dispose()
$dst.Save((Join-Path $folder 'kalusteet-maton-paalla-5120x1844.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview=Resize-Canvas $dst 2089 753
$preview.Save((Join-Path $folder 'kosketusvarjojen-muokkausviite.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
$dst.Dispose();$src.Dispose();$props.Dispose();$bg.Dispose();$floor.Dispose()
Write-Output "Contact baseline=1335, floor starts=1199, translation=$shift"
