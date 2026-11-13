$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $root 'kasino-layer-utils.ps1')
$existing=Join-Path $root 'kasino-sisatilat-vasen-parallaksi-3tasoa'
$work=Join-Path $root 'kasino-sisatilat-baari-tyo'
$null=New-Item -ItemType Directory -Path $work -Force
foreach($name in @('01-kaukotausta','02-keskitaso','03-etuala')){
 $src=[System.Drawing.Bitmap]::FromFile((Join-Path $existing ($name+'-vasen-5120x1844.png')))
 $small=Resize-Canvas $src 1280 461
 $null=Clean-Alpha $small
 $small.Save((Join-Path $work ($name+'-vanha.png')),[System.Drawing.Imaging.ImageFormat]::Png)
 $guide=[System.Drawing.Bitmap]::new(1376,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[System.Drawing.Graphics]::FromImage($guide);$g.Clear([System.Drawing.Color]::FromArgb(128,128,128))
 $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
 $g.DrawImage($small,[System.Drawing.Rectangle]::new(1280,0,96,461),0,0,96,461,[System.Drawing.GraphicsUnit]::Pixel)
 $guide.Save((Join-Path $work ($name+'-liitosohje.png')),[System.Drawing.Imaging.ImageFormat]::Png)
 $guide.Dispose();$g.Dispose();$small.Dispose();$src.Dispose()
}
