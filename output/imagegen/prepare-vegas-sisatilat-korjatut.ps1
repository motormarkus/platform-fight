$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$window=[Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-1948739c-7ca4-4c43-8af1-157d64b9d26e.png')
$wd=Pixel-Data $window
Add-Type -TypeDefinition @'
using System;
public static class ChapelWindowAlpha {
 static double Clamp(double t){return Math.Max(0,Math.Min(1,t));}
 static double Line(double x,double y,double ax,double ay,double bx,double by){double dx=bx-ax,dy=by-ay,t=Clamp(((x-ax)*dx+(y-ay)*dy)/(dx*dx+dy*dy));return Math.Sqrt(Math.Pow(x-ax-t*dx,2)+Math.Pow(y-ay-t*dy,2));}
 public static void Clear(byte[] d,int w,int h){
  double cx=588,cy=477,rx=391,ry=369,left=197,right=978,bottom=1316;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   double xx=x*1126.0/w,yy=y*1397.0/h;
   double inside=Math.Min(Math.Min(xx-left,right-xx),bottom-yy);
   if(yy<cy)inside=Math.Min(inside,(1-Math.Sqrt(Math.Pow((xx-cx)/rx,2)+Math.Pow((yy-cy)/ry,2)))*ry);
   if(inside<=0)continue;
   double bars=Math.Min(Math.Abs(xx-588)-5.5,Math.Min(Math.Abs(yy-477)-5.5,Math.Abs(yy-860)-5.5));
   if(yy<=cy+6){foreach(double[] p in new[]{new[]{378.0,161.0},new[]{244.0,286.0},new[]{798.0,161.0},new[]{928.0,286.0}})bars=Math.Min(bars,Line(xx,yy,588,477,p[0],p[1])-3.3);}
   int i=(y*w+x)*4;
   double clear=Clamp(inside/1.5)*Clamp(bars/1.5);
   if(bars<0)d[i+3]=255;else d[i+3]=(byte)Math.Round(d[i+3]*(1-clear));
   if(d[i+3]<=16)d[i]=d[i+1]=d[i+2]=d[i+3]=0;
  }
 }
 public static void Insert(byte[] dst,int w,byte[] src,int sw,int sh,int ox){
  for(int y=0;y<349;y++)for(int x=0;x<sw;x++){
   double f=Clamp(x/14.0)*Clamp((sw-1-x)/14.0)*Clamp(y/6.0)*Clamp((348-y)/9.0);
   f=f*f*(3-2*f);
   int i=(y*w+x+ox)*4,j=(y*sw+x)*4;
   for(int c=0;c<4;c++)dst[i+c]=(byte)Math.Round(dst[i+c]*(1-f)+src[j+c]*f);
   if(dst[i+3]<=16)dst[i]=dst[i+1]=dst[i+2]=dst[i+3]=0;
  }
 }
}
'@
[ChapelWindowAlpha]::Clear($wd,$window.Width,$window.Height)
Set-PixelData $window $wd
$window.Save((Join-Path $PSScriptRoot 'vegas-kappeli-sisa-tyo\ikkuna-puhdas.png'),[Drawing.Imaging.ImageFormat]::Png)
$winSmall=Resize-Canvas $window 290 360
$chapel=[Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-e42378e1-41c5-4dc1-881a-9f01a8c798a2.png')
$cd=Pixel-Data $chapel;$sd=Pixel-Data $winSmall
[ChapelWindowAlpha]::Insert($cd,$chapel.Width,$sd,290,360,620)
[ChapelWindowAlpha]::Insert($cd,$chapel.Width,$sd,290,360,1280)
Set-PixelData $chapel $cd
$chapelNative=Join-Path $PSScriptRoot 'vegas-kappeli-sisa-tyo\vasen-korjattu-natiivi.png'
$chapel.Save($chapelNative,[Drawing.Imaging.ImageFormat]::Png)
$chapel.Dispose();$winSmall.Dispose();$window.Dispose()
function Prepare([string]$slug,[string]$rawPath){
 $dir=Join-Path $PSScriptRoot ($slug+'-sisatilat-v2');$work=Join-Path $PSScriptRoot ($slug+'-sisa-tyo')
 [void][IO.Directory]::CreateDirectory($dir)
 $raw=[Drawing.Bitmap]::new($rawPath);$native='{0}x{1}' -f $raw.Width,$raw.Height
 $left=Resize-Canvas $raw 5120 1844;$null=Clean-Alpha $left
 $left.Save((Join-Path $dir ($slug+'-vasen-5120x1844.png')),[Drawing.Imaging.ImageFormat]::Png)
 $small=Resize-Canvas $left 1280 461;$small.Save((Join-Path $dir 'vasen-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
 $guide=[Drawing.Bitmap]::new(1376,461,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[Drawing.Graphics]::FromImage($guide);$g.Clear([Drawing.Color]::FromArgb(128,128,128));$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
 $g.DrawImage($small,[Drawing.Rectangle]::new(0,0,96,461),[Drawing.Rectangle]::new(1184,0,96,461),[Drawing.GraphicsUnit]::Pixel)
 $g.Dispose();$guide.Save((Join-Path $work 'oikea-jatko-ohje-v2.png'),[Drawing.Imaging.ImageFormat]::Png)
 $native | Set-Content -LiteralPath (Join-Path $work 'vasen-v2-natiivikoko.txt')
 $guide.Dispose();$small.Dispose();$left.Dispose();$raw.Dispose()
}
Prepare 'vegas-kappeli' $chapelNative
Prepare 'vegas-panttilainaamo' 'C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-ffdf7e1d-90d3-4ee2-a807-b7309c0961a4.png'
