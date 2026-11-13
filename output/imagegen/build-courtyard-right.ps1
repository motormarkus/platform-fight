Add-Type -AssemblyName System.Drawing
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$src=[System.Drawing.Bitmap]::FromFile('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-a8397913-3506-43d6-90f9-5bb2b4c8a5dd.png')
$previous=[System.Drawing.Bitmap]::FromFile((Join-Path $root 'vankilan-sisapiha-liikunta-5120x1844.png'))
$dst=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($dst)
$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[System.Drawing.Imaging.ImageAttributes]::new()
$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
$sourceYs=@(0,484,516,686)
$targetYs=@(0,300,321,461)
$cropX=[single](220*$src.Width/2048.0)
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
public static class CourtyardSeamPixels {
 static byte Clamp(double x){return (byte)Math.Max(0,Math.Min(255,Math.Round(x)));}
 static double Lerp(double a,double b,double t){return a+(b-a)*t;}
 static double ArcY(double x){
  double v=(x+1280-1229.5)/323.0;
  double y=358.2479313935469+49.126465718915235*Math.Sqrt(Math.Max(0,1-v*v));
  if(x<80){double t=Math.Max(0,x/80.0);y+=0.182*(2*t*t*t-3*t*t+1)+0.0104*80*(t*t*t-2*t*t+t);}
  return y;
 }
 static double SegmentDistance(double x,double y,double ax,double ay,double bx,double by){
  double vx=bx-ax,vy=by-ay,t=Math.Max(0,Math.Min(1,((x-ax)*vx+(y-ay)*vy)/(vx*vx+vy*vy)));
  return Math.Sqrt(Math.Pow(x-ax-t*vx,2)+Math.Pow(y-ay-t*vy,2));
 }
 static double SourceChannel(byte[] p,int w,double baseY,int c){
  double yy=baseY*4-0.5;int y=(int)Math.Floor(yy);double t=yy-y;
  y=Math.Max(0,Math.Min(1842,y));return Lerp(p[(y*w+w-1)*4+c],p[((y+1)*w+w-1)*4+c],t);
 }
 public static string Finish(byte[] image,byte[] previous,int w,int h){
  int transparent=0;
  for(int i=0;i<image.Length;i+=4){
   if(image[i+3]<=16){image[i]=image[i+1]=image[i+2]=image[i+3]=0;transparent++;}
   else if(image[i+3]>=240)image[i+3]=255;
  }
  for(int y=0;y<h;y++){
   int p=(y*w+w-1)*4,q=y*w*4;
   double by=(y+0.5)/4.0;
   bool paintBand=(by>356&&by<369)||(by>374&&by<383)||(by>402&&by<413);
   double[] delta=new double[3];
   for(int c=0;c<3;c++){
    double reference=previous[p+c];
    if(paintBand){double center=by<370?362.39:by<390?378.5:406.95;reference=SourceChannel(previous,w,center-8,c)*0.5+SourceChannel(previous,w,center+8,c)*0.5;}
    delta[c]=reference-image[q+c];
   }
   for(int x=0;x<384;x++){
    int i=(y*w+x)*4;if(image[i+3]==0)continue;
    double t=x/383.0,weight=1-t*t*(3-2*t);
    for(int c=0;c<3;c++)image[i+c]=Clamp(image[i+c]+delta[c]*weight);
   }
  }
  // Continue the fitted basketball arc and the incomplete trapezoidal key, at original scale.
  double diagStart=362.3890412818793,diagSlope=0.5217063122526245,bottom=378.5;
  double keyEnd=(bottom-diagStart)/diagSlope;
  for(int yy=320*4;yy<416*4;yy++)for(int xx=0;xx<280*4;xx++){
   double x=(xx+0.5)/4.0,y=(yy+0.5)/4.0;
   double dx=x+1280-1229.5,cy=358.2479313935469;
   if(y>cy&&x<80){double t=x/80.0;cy+=0.182*(2*t*t*t-3*t*t+1)+0.0104*80*(t*t*t-2*t*t+t);}
   double dy=y-cy,rx=323,ry=49.126465718915235;
   double f=dx*dx/(rx*rx)+dy*dy/(ry*ry)-1;
   double grad=2*Math.Sqrt(dx*dx/Math.Pow(rx,4)+dy*dy/Math.Pow(ry,4));
   double arcDist=Math.Abs(f/grad);
   double d=Math.Min(arcDist,Math.Min(SegmentDistance(x,y,0,diagStart,keyEnd,bottom),SegmentDistance(x,y,0,bottom,keyEnd,bottom)));
   double coverage=Math.Max(0,Math.Min(1,(2.15-d)/1.1));
   coverage*=Math.Max(0,Math.Min(1,y-320.5));
   int i=(yy*w+xx)*4;
   if(coverage>0){
    int tx=w-340+(xx%336),ty=1627;
    double wear=Math.Max(0.70,Math.Min(1.03,((previous[(ty*w+tx)*4]+previous[(ty*w+tx)*4+1]+previous[(ty*w+tx)*4+2])/3.0-115)/60));
    double a=coverage*0.84*wear;
    double[] color={166,177,187};for(int c=0;c<3;c++)image[i+c]=Clamp(Lerp(image[i+c],color[c],a));
   }
   // Carry the original seam paint profile along each joining line, fading into the new paint.
   double sourceY=0,weight=0;
   if(x<70 && Math.Abs(y-ArcY(x))<5){sourceY=406.952+(y-ArcY(x));double t=x/70;weight=1-t*t*(3-2*t);}
   if(x<keyEnd-2 && Math.Abs(y-(diagStart+diagSlope*x))<5){sourceY=362.389+(y-diagStart-diagSlope*x);weight=Math.Max(weight,1-x/40);}
   if(x<keyEnd-2 && Math.Abs(y-bottom)<4){sourceY=y;weight=Math.Max(weight,1-x/40);}
   if(weight>0)for(int c=0;c<3;c++)image[i+c]=Clamp(Lerp(image[i+c],SourceChannel(previous,w,sourceY,c),weight));
  }
  for(int y=0;y<h;y++){int i=y*w*4,p=(y*w+w-1)*4;for(int c=0;c<4;c++)image[i+c]=previous[p+c];}
  int errors=0;for(int y=0;y<h;y++)for(int c=0;c<4;c++)if(image[y*w*4+c]!=previous[(y*w+w-1)*4+c])errors++;
  return "Transparent pixels="+transparent+"; RGBA boundary mismatches="+errors;
 }
}
'@
[CourtyardSeamPixels]::Finish($arrays[0],$arrays[1],5120,1844)
$bd=$dst.LockBits($rect,[System.Drawing.Imaging.ImageLockMode]::WriteOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
[System.Runtime.InteropServices.Marshal]::Copy($arrays[0],0,$bd.Scan0,$arrays[0].Length)
$dst.UnlockBits($bd)
$dst.Save((Join-Path $root 'vankilan-sisapiha-jatko-5120x1844.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview=[System.Drawing.Bitmap]::new(2560,461)
$pg=[System.Drawing.Graphics]::FromImage($preview)
$pg.Clear([System.Drawing.Color]::FromArgb(45,90,110))
$pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$pg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$pa=[System.Drawing.Imaging.ImageAttributes]::new()
$pa.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
$pg.DrawImage($previous,[System.Drawing.Rectangle]::new(0,0,1280,461),0,0,5120,1844,[System.Drawing.GraphicsUnit]::Pixel,$pa)
$pg.DrawImage($dst,[System.Drawing.Rectangle]::new(1280,0,1280,461),0,0,5120,1844,[System.Drawing.GraphicsUnit]::Pixel,$pa)
$preview.Save((Join-Path $root 'vankilan-sisapiha-liitos-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$pa.Dispose();$pg.Dispose();$preview.Dispose();$dst.Dispose();$previous.Dispose();$src.Dispose()
