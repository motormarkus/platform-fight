Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
public static class CasinoLeftPixels {
 public static string Normalize(byte[] d){int c=0,o=0;for(int i=0;i<d.Length;i+=4){if(d[i+3]<=16){d[i]=d[i+1]=d[i+2]=d[i+3]=0;c++;}else if(d[i+3]>=240){d[i+3]=255;o++;}}return "clear="+c+"; opaque="+o;}
}
'@
function Pixel-Data([System.Drawing.Bitmap]$im){
 $bd=$im.LockBits([System.Drawing.Rectangle]::new(0,0,$im.Width,$im.Height),[System.Drawing.Imaging.ImageLockMode]::ReadOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $d=[byte[]]::new($bd.Stride*$im.Height);[System.Runtime.InteropServices.Marshal]::Copy($bd.Scan0,$d,0,$d.Length);$im.UnlockBits($bd);return ,$d
}
function Set-PixelData([System.Drawing.Bitmap]$im,[byte[]]$d){
 $bd=$im.LockBits([System.Drawing.Rectangle]::new(0,0,$im.Width,$im.Height),[System.Drawing.Imaging.ImageLockMode]::WriteOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 [System.Runtime.InteropServices.Marshal]::Copy($d,0,$bd.Scan0,$d.Length);$im.UnlockBits($bd)
}
function Clean-Alpha([System.Drawing.Bitmap]$im){$d=Pixel-Data $im;$r=[CasinoLeftPixels]::Normalize($d);Set-PixelData $im $d;return $r}
function Resize-Canvas([System.Drawing.Bitmap]$src,[int]$width,[int]$height,[double]$cropW=-1){
 if($cropW -lt 0){$cropW=$src.Width}
 $dst=[System.Drawing.Bitmap]::new($width,$height,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[System.Drawing.Graphics]::FromImage($dst);$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
 $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
 $attrs=[System.Drawing.Imaging.ImageAttributes]::new();$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
 $g.DrawImage($src,[System.Drawing.Rectangle]::new(0,0,$width,$height),[single]0,[single]0,[single]$cropW,[single]$src.Height,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
 $attrs.Dispose();$g.Dispose();return $dst
}
