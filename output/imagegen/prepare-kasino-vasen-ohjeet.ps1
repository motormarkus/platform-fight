$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$existing=Join-Path $root 'kasino-sisatilat-parallaksi-3tasoa'
$work=Join-Path $root 'kasino-sisatilat-vasen-tyo'
$null=New-Item -ItemType Directory -Path $work -Force
foreach($name in @('01-kaukotausta','02-keskitaso','03-etuala')){
 $src=[System.Drawing.Bitmap]::FromFile((Join-Path $existing ($name+'-5120x1844.png')))
 $small=[System.Drawing.Bitmap]::new(1280,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[System.Drawing.Graphics]::FromImage($small)
 $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
 $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 $g.DrawImage($src,[System.Drawing.Rectangle]::new(0,0,1280,461))
 $g.Dispose()
 $small.Save((Join-Path $work ($name+'-vanha.png')),[System.Drawing.Imaging.ImageFormat]::Png)
 $guide=[System.Drawing.Bitmap]::new(1376,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $gg=[System.Drawing.Graphics]::FromImage($guide)
 $gg.Clear([System.Drawing.Color]::FromArgb(128,128,128))
 $gg.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
 $gg.DrawImage($small,[System.Drawing.Rectangle]::new(1280,0,96,461),0,0,96,461,[System.Drawing.GraphicsUnit]::Pixel)
 $gg.Dispose()
 $guide.Save((Join-Path $work ($name+'-liitosohje.png')),[System.Drawing.Imaging.ImageFormat]::Png)
 $guide.Dispose();$small.Dispose();$src.Dispose()
}
Get-ChildItem -LiteralPath $work -Filter '*liitosohje.png'|Select-Object Name,Length
