Add-Type -AssemblyName System.Drawing
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$raw='C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-08b35d58-51f2-4cc8-9621-63364df9bbfa.png'
$src=[System.Drawing.Bitmap]::FromFile($raw)
$src.Save((Join-Path $root 'vankilan-robottipaja-vasen-alkuperainen.png'),[System.Drawing.Imaging.ImageFormat]::Png)
function Resize-Image($im,[int]$w,[int]$h){
 $out=[System.Drawing.Bitmap]::new($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[System.Drawing.Graphics]::FromImage($out)
 $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
 $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
 $a=[System.Drawing.Imaging.ImageAttributes]::new();$a.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
 $g.DrawImage($im,[System.Drawing.Rectangle]::new(0,0,$w,$h),0,0,$im.Width,$im.Height,[System.Drawing.GraphicsUnit]::Pixel,$a)
 $g.Dispose();$a.Dispose();return $out
}
$final=Resize-Image $src 5120 1844
$final.Save((Join-Path $root 'vankilan-robottipaja-vasen-5120x1844.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$base=Resize-Image $final 1280 461
$base.Save((Join-Path $root 'robottipaja-vasen-base.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$guide=[System.Drawing.Bitmap]::new(1376,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($guide);$g.Clear([System.Drawing.Color]::FromArgb(128,128,128));$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.DrawImage($base,[System.Drawing.Rectangle]::new(0,0,96,461),1184,0,96,461,[System.Drawing.GraphicsUnit]::Pixel)
$guide.Save((Join-Path $root 'robottipaja-oikea-ohje.png'),[System.Drawing.Imaging.ImageFormat]::Png)
"LEFT native=$($src.Width)x$($src.Height); export=$($final.Width)x$($final.Height)" | Tee-Object -FilePath (Join-Path $root 'vankilan-robottipaja-tarkistus.txt')
$g.Dispose();$guide.Dispose();$base.Dispose();$final.Dispose();$src.Dispose()
