$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$left=[System.Drawing.Bitmap]::FromFile((Join-Path $root 'kasinon-katto-vasen-5120x1844.png'))
$right=[System.Drawing.Bitmap]::FromFile((Join-Path $root 'kasinon-katto-oikea-5120x1844.png'))
$sky=[System.Drawing.Bitmap]::FromFile((Join-Path $root 'las-vegas-parallaksi-v2/01-yotaivas-5120x1844.png'))
foreach($pic in @($left,$right)){
 if($pic.Width -ne 5120 -or $pic.Height -ne 1844){throw 'Unexpected final size'}
 foreach($xy in @(@(400,200),@(1600,400),@(2400,400))){if($pic.GetPixel($xy[0],$xy[1]).A -ne 0){throw 'Sky area is not transparent'}}
 foreach($xy in @(@(400,1600),@(2000,1600),@(4000,1600))){if($pic.GetPixel($xy[0],$xy[1]).A -ne 255){throw 'Floor is not opaque'}}
}
$preview=[System.Drawing.Bitmap]::new(2560,461)
$g=[System.Drawing.Graphics]::FromImage($preview)
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.DrawImage($sky,[System.Drawing.Rectangle]::new(0,0,1280,461))
$sky.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX)
$g.DrawImage($sky,[System.Drawing.Rectangle]::new(1280,0,1280,461))
$g.DrawImage($left,[System.Drawing.Rectangle]::new(0,0,1280,461))
$g.DrawImage($right,[System.Drawing.Rectangle]::new(1280,0,1280,461))
$preview.Save((Join-Path $root 'kasinon-katto-yo-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose();$preview.Dispose();$sky.Dispose();$left.Dispose();$right.Dispose()
$report=@'

EXPORT AND VALIDATION
Mode: built-in image_gen, followed by mechanical alpha normalization, resizing and seam registration.
LEFT native generated image: 2089 x 753, exec-253e6781-38bb-4e57-b876-1dcec7cd95b2.png.
RIGHT native generated image: 2167 x 726 including guide overlap, exec-83e59760-f356-41ba-9b13-0a1b717c06cf.png.
Both final foreground files are 5120 x 1844 RGBA PNG exports. These are resampled, not native 5K generated detail.
The right guide overlap was removed; structure heights and seam colours were registered to the left panel. See build-kasinon-katto-oikea.ps1 for mapping.
Sky areas are alpha=0 and walking roof alpha=255 at checked samples in BOTH panels.
Joining edge: 0 differing RGBA pixels out of 1844 rows. Cyan neon and parapet-base profile offsets=0 px; cap profile=-1 px, visually continuous at native inspection scale.
Combined view and seam close-up visually reviewed: no visible height step, floor and neon continue through the join.
The night preview composites the accepted Vegas sky only for viewing. Neither delivered roof PNG contains that background.
Layout: left helipad; right wide elevator, visible hoist machinery and tall closing end wall.
'@
Add-Content -LiteralPath (Join-Path $root 'kasinon-katto-generointiohjeet.txt') -Value $report
Add-Content -LiteralPath (Join-Path $root 'kasinon-katto-tarkistus.txt') -Value $report
Get-Item -LiteralPath (Join-Path $root 'kasinon-katto-vasen-5120x1844.png'),(Join-Path $root 'kasinon-katto-oikea-5120x1844.png'),(Join-Path $root 'kasinon-katto-yo-esikatselu.png') | Select-Object Name,Length
