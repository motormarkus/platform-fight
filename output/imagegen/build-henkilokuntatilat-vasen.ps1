$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath='C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-5be596ad-f950-47c0-9dee-0d6519fb17d9.png'
$src=[System.Drawing.Bitmap]::FromFile($sourcePath)
Add-Type -TypeDefinition @'
using System;
public static class StaffAlphaClean {
 public static string Normalize(byte[] data){
  int clear=0,solid=0,edge=0;
  for(int i=0;i<data.Length;i+=4){
   if(data[i+3]<=16){data[i]=data[i+1]=data[i+2]=data[i+3]=0;clear++;}
   else if(data[i+3]>=240){data[i+3]=255;solid++;}
   else edge++;
  }
  return "Transparent="+clear+"; opaque="+solid+"; antialias="+edge;
 }
}
'@
function Normalize-Alpha([System.Drawing.Bitmap]$im){
 $rect=[System.Drawing.Rectangle]::new(0,0,$im.Width,$im.Height)
 $bd=$im.LockBits($rect,[System.Drawing.Imaging.ImageLockMode]::ReadWrite,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $bytes=[byte[]]::new($bd.Stride*$im.Height)
 [System.Runtime.InteropServices.Marshal]::Copy($bd.Scan0,$bytes,0,$bytes.Length)
 $result=[StaffAlphaClean]::Normalize($bytes)
 [System.Runtime.InteropServices.Marshal]::Copy($bytes,0,$bd.Scan0,$bytes.Length)
 $im.UnlockBits($bd)
 $result
}
$nativeReport=Normalize-Alpha $src
$src.Save((Join-Path $root 'vankilan-henkilokuntatilat-vasen-alkuperainen.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$dst=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($dst)
$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[System.Drawing.Imaging.ImageAttributes]::new()
$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
$g.DrawImage($src,[System.Drawing.Rectangle]::new(0,0,5120,1844),0,0,$src.Width,$src.Height,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$g.Dispose()
$finalReport=Normalize-Alpha $dst
$finalPath=Join-Path $root 'vankilan-henkilokuntatilat-vasen-5120x1844.png'
$dst.Save($finalPath,[System.Drawing.Imaging.ImageFormat]::Png)
$preview=[System.Drawing.Bitmap]::new($src.Width,$src.Height)
$pg=[System.Drawing.Graphics]::FromImage($preview)
$pg.Clear([System.Drawing.Color]::FromArgb(45,90,110))
$pg.DrawImageUnscaled($src,0,0)
$preview.Save((Join-Path $root 'vankilan-henkilokuntatilat-vasen-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$windowPoints=@(@(968,150),@(1050,150),@(1138,150),@(1830,150),@(1920,150),@(2010,150),@(1515,430),@(1640,430),@(1750,430))
$checks=@()
foreach($pt in $windowPoints){
 $alpha=$dst.GetPixel([int]($pt[0]*5120.0/$src.Width),[int]($pt[1]*1844.0/$src.Height)).A
 $checks+="Window sample $($pt[0]),$($pt[1]): alpha=$alpha"
 if($alpha -ne 0){throw "Window is not fully transparent at $($pt[0]),$($pt[1])"}
}
$saved=[System.Drawing.Bitmap]::FromFile($finalPath)
if($saved.Width -ne 5120 -or $saved.Height -ne 1844){throw 'Incorrect saved dimensions'}
$report=@("Native dimensions: $($src.Width)x$($src.Height)","Final dimensions: $($saved.Width)x$($saved.Height)","Export resampled with HighQualityBicubic; not native 5K detail.","Native: $nativeReport","Final: $finalReport")+$checks
$report
Set-Content -LiteralPath (Join-Path $root 'vankilan-henkilokuntatilat-vasen-tarkistus.txt') -Value $report
Add-Content -LiteralPath (Join-Path $root 'vankilan-henkilokuntatilat-vasen-prompt.txt') -Value @("","Raw source: $sourcePath","Native dimensions: $($src.Width)x$($src.Height)","Delivery: 5120x1844 PNG resampled, alpha normalized and transparent RGB cleared.")
$saved.Dispose();$pg.Dispose();$preview.Dispose();$attrs.Dispose();$dst.Dispose();$src.Dispose()
