Add-Type -AssemblyName System.Drawing
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$src=[System.Drawing.Bitmap]::FromFile('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-c790ef88-8972-44f6-8be3-e2a647bdd40d.png')
$previous=[System.Drawing.Bitmap]::FromFile((Join-Path $root 'vankilan-ruokala-korkea-tiski-5120x1844.png'))
$dst=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($dst)
$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[System.Drawing.Imaging.ImageAttributes]::new()
$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
$sourceYs=@(0,271,298,310,420,686)
$targetYs=@(0,170,187,194,263,461)
$cropX=[single](96*$src.Width/1376.0)
for($j=0;$j -lt $sourceYs.Count-1;$j++){
 $sy=[single]($sourceYs[$j]*$src.Height/686.0)
 $sh=[single](($sourceYs[$j+1]-$sourceYs[$j])*$src.Height/686.0)
 $dr=[System.Drawing.Rectangle]::new(0,$targetYs[$j]*4,5120,($targetYs[$j+1]-$targetYs[$j])*4)
 $g.DrawImage($src,$dr,$cropX,$sy,[single]($src.Width-$cropX),$sh,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
}
$g.Dispose();$attrs.Dispose()
$rect=[System.Drawing.Rectangle]::new(0,0,5120,1844)
$arrays=@()
foreach($im in @($dst,$previous)){
 $bd=$im.LockBits($rect,[System.Drawing.Imaging.ImageLockMode]::ReadOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $bytes=[byte[]]::new($bd.Stride*$im.Height)
 [System.Runtime.InteropServices.Marshal]::Copy($bd.Scan0,$bytes,0,$bytes.Length)
 $im.UnlockBits($bd)
 $arrays+=,$bytes
}
Add-Type -TypeDefinition @'
using System;
public static class CafeteriaEndPixels {
 static byte Clamp(double x){return (byte)Math.Max(0,Math.Min(255,Math.Round(x)));}
 public static string Finish(byte[] image,byte[] previous,int w,int h){
  int transparent=0;
  for(int i=0;i<image.Length;i+=4){
   if(image[i+3]<=16){image[i]=image[i+1]=image[i+2]=image[i+3]=0;transparent++;}
   else if(image[i+3]>=240)image[i+3]=255;
  }
  for(int y=0;y<h;y++){
   int p=(y*w+w-1)*4,q=y*w*4;
   double[] delta={previous[p]-image[q],previous[p+1]-image[q+1],previous[p+2]-image[q+2]};
   for(int x=0;x<384;x++){
    int i=(y*w+x)*4;if(image[i+3]==0)continue;
    double t=x/383.0,weight=1-t*t*(3-2*t);
    for(int c=0;c<3;c++)image[i+c]=Clamp(image[i+c]+delta[c]*weight);
   }
  }
  for(int y=0;y<h;y++){int i=y*w*4,p=(y*w+w-1)*4;for(int c=0;c<4;c++)image[i+c]=previous[p+c];}
  int errors=0;for(int y=0;y<h;y++)for(int c=0;c<4;c++)if(image[y*w*4+c]!=previous[(y*w+w-1)*4+c])errors++;
  return "Transparent pixels="+transparent+"; RGBA boundary mismatches="+errors;
 }
}
'@
[CafeteriaEndPixels]::Finish($arrays[0],$arrays[1],5120,1844)
$bd=$dst.LockBits($rect,[System.Drawing.Imaging.ImageLockMode]::WriteOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
[System.Runtime.InteropServices.Marshal]::Copy($arrays[0],0,$bd.Scan0,$arrays[0].Length)
$dst.UnlockBits($bd)
$dst.Save((Join-Path $root 'vankilan-ruokala-oikea-paaty-5120x1844.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview=[System.Drawing.Bitmap]::new(2560,461)
$pg=[System.Drawing.Graphics]::FromImage($preview)
$pg.Clear([System.Drawing.Color]::FromArgb(45,90,110))
$pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$pg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$pa=[System.Drawing.Imaging.ImageAttributes]::new()
$pa.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
$pg.DrawImage($previous,[System.Drawing.Rectangle]::new(0,0,1280,461),0,0,5120,1844,[System.Drawing.GraphicsUnit]::Pixel,$pa)
$pg.DrawImage($dst,[System.Drawing.Rectangle]::new(1280,0,1280,461),0,0,5120,1844,[System.Drawing.GraphicsUnit]::Pixel,$pa)
$preview.Save((Join-Path $root 'vankilan-ruokala-liitos-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$pa.Dispose();$pg.Dispose();$preview.Dispose();$dst.Dispose();$previous.Dispose();$src.Dispose()
