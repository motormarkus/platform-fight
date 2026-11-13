Add-Type -AssemblyName System.Drawing
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$raw='C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-a3bbe6cc-48a4-44e6-8b0e-6df6379b9363.png'
$src=[System.Drawing.Bitmap]::FromFile($raw)
$previous=[System.Drawing.Bitmap]::FromFile((Join-Path $root 'vankila-terminaali-ikkunat-lapinakyvat-5120x1844.png'))
$dst=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($dst)
$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[System.Drawing.Imaging.ImageAttributes]::new()
$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
# Native generated guide strip is discarded; map architectural levels to the preceding panel.
$sourceYs=@(0,190,234,342,401,414,428,471,683)
$targetYs=@(0,173,210,289,332,343,355,386,461)
$cropX=241
for($j=0;$j -lt $sourceYs.Count-1;$j++){
 $sy=[single]($sourceYs[$j]*$src.Height/683.0)
 $sh=[single](($sourceYs[$j+1]-$sourceYs[$j])*$src.Height/683.0)
 $dr=[System.Drawing.Rectangle]::new(0,$targetYs[$j]*4,5120,($targetYs[$j+1]-$targetYs[$j])*4)
 $g.DrawImage($src,$dr,[single]$cropX,$sy,[single]($src.Width-$cropX),$sh,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
}
# Align window crossbars locally, without bending the diagonal end-wall geometry.
$windowSourceY=@(0,20,59,65,190)
$windowTargetY=@(0,21,104,108,173)
foreach($windowX in @(@(251,702),@(945,1397))){
 $sx=[single]($windowX[0]*$src.Width/2048.0)
 $sw=[single](($windowX[1]-$windowX[0])*$src.Width/2048.0)
 $dx=[int][Math]::Round(($sx-$cropX)*5120/($src.Width-$cropX))
 $dw=[int][Math]::Round($sw*5120/($src.Width-$cropX))
 for($j=0;$j -lt $windowSourceY.Count-1;$j++){
  $sy=[single]($windowSourceY[$j]*$src.Height/683.0)
  $sh=[single](($windowSourceY[$j+1]-$windowSourceY[$j])*$src.Height/683.0)
  $dr=[System.Drawing.Rectangle]::new($dx,$windowTargetY[$j]*4,$dw,($windowTargetY[$j+1]-$windowTargetY[$j])*4)
  $g.DrawImage($src,$dr,$sx,$sy,$sw,$sh,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
 }
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
public static class TerminalPixels {
 static byte Clamp(double x){return (byte)Math.Max(0,Math.Min(255,Math.Round(x)));}
 public static string Finish(byte[] image,byte[] previous,int w,int h){
  int transparent=0;
  // Remove the uniform temporary window chroma color; neutralize antialias spill.
  for(int y=0;y<700;y++)for(int x=0;x<w;x++){
   int i=(y*w+x)*4;double b=image[i],g=image[i+1],r=image[i+2],excess=g-Math.Max(r,b);
   if(excess>20 && g>1.25*Math.Max(r,b)){
    double a=Math.Max(0,255-excess);
    if(a<36){image[i]=0;image[i+1]=0;image[i+2]=0;image[i+3]=0;transparent++;}
    else {image[i+3]=Clamp(a);image[i]=Clamp(b*255/a);image[i+2]=Clamp(r*255/a);image[i+1]=Clamp((image[i]+image[i+2])/2.0);}
   }
  }
  // Match the prior panel's edge color, smoothly reducing correction into the new wall and floor.
  for(int y=0;y<h;y++){
   int p=(y*w+w-1)*4,q=y*w*4;
   double[] delta={previous[p]-image[q],previous[p+1]-image[q+1],previous[p+2]-image[q+2]};
   for(int x=0;x<320;x++){
    int i=(y*w+x)*4;if(image[i+3]==0)continue;
    double t=x/319.0,weight=1-t*t*(3-2*t);
    for(int c=0;c<3;c++) image[i+c]=Clamp(image[i+c]+delta[c]*weight);
   }
  }
  // Continue the naturally repeating hazard paint at the same thickness and phase.
  for(int y=343*4;y<355*4;y++)for(int x=0;x<3410;x++){
   int i=(y*w+x)*4,p=(y*w+(w-116+x%116))*4;
   double blend=x<3314?1:(3410-x)/96.0;
   for(int c=0;c<3;c++)image[i+c]=Clamp(previous[p+c]*blend+image[i+c]*(1-blend));
  }
  // Lock the boundary column to the actual previously delivered high-resolution PNG.
  for(int y=0;y<h;y++){int i=y*w*4,p=(y*w+w-1)*4;for(int c=0;c<4;c++)image[i+c]=previous[p+c];}
  int errors=0;for(int y=0;y<h;y++)for(int c=0;c<4;c++)if(image[y*w*4+c]!=previous[(y*w+w-1)*4+c])errors++;
  return "Transparent window pixels="+transparent+"; RGBA boundary mismatches="+errors;
 }
}
'@
[TerminalPixels]::Finish($arrays[0],$arrays[1],5120,1844)
$bd=$dst.LockBits($rect,[System.Drawing.Imaging.ImageLockMode]::WriteOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
[System.Runtime.InteropServices.Marshal]::Copy($arrays[0],0,$bd.Scan0,$arrays[0].Length)
$dst.UnlockBits($bd)
$dst.Save((Join-Path $root 'vankila-terminaali-oikea-jatko-5120x1844.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview=[System.Drawing.Bitmap]::new(2560,461)
$pg=[System.Drawing.Graphics]::FromImage($preview)
$pg.Clear([System.Drawing.Color]::FromArgb(45,90,110))
$pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$pg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$pa=[System.Drawing.Imaging.ImageAttributes]::new()
$pa.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
$pg.DrawImage($previous,[System.Drawing.Rectangle]::new(0,0,1280,461),0,0,5120,1844,[System.Drawing.GraphicsUnit]::Pixel,$pa)
$pg.DrawImage($dst,[System.Drawing.Rectangle]::new(1280,0,1280,461),0,0,5120,1844,[System.Drawing.GraphicsUnit]::Pixel,$pa)
$preview.Save((Join-Path $root 'vankila-terminaali-liitoksen-tarkistus.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$pa.Dispose();$pg.Dispose();$preview.Dispose();$dst.Dispose();$previous.Dispose();$src.Dispose()
