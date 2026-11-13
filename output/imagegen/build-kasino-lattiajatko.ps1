$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$floorDir = Join-Path $PSScriptRoot 'kasino-lattiajatko'
[void][System.IO.Directory]::CreateDirectory($floorDir)
$sourcePath = Join-Path $PSScriptRoot 'kasino-sisatilat-baari-parallaksi-3tasoa\03-etuala-baari-5120x1844.png'
$rawPath = 'C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-76ce961f-a582-433d-8e64-3f03f1eee0ae.png'
$sourceHash = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
$source = [System.Drawing.Bitmap]::new($sourcePath)
$raw = [System.Drawing.Bitmap]::new($rawPath)
$nativeSize = '{0}x{1}' -f $raw.Width,$raw.Height
$floor = Resize-Canvas $raw 5120 1844
$raw.Dispose()
$floorData = Pixel-Data $floor
$sourceData = Pixel-Data $source
Add-Type -TypeDefinition @'
using System;
public static class CasinoFloorJoin {
 static double Smooth(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
 static byte Clamp(double v){return (byte)Math.Max(0,Math.Min(255,Math.Round(v)));}
 public static void Align(byte[] d,byte[] s,int w,int h){
  const int top=1195, borderBottom=1224;
  Array.Clear(d,0,top*w*4);
  double[,] correction=new double[h,3];
  for(int y=borderBottom;y<h;y++){
   int ya=Math.Max(borderBottom,y-16),yb=Math.Min(h-1,y+16);
   for(int c=0;c<3;c++){
    double a=0,b=0;int n=0;
    for(int yy=ya;yy<=yb;yy+=4)for(int x=0;x<128;x+=4){
     a+=s[(yy*w+x)*4+c];b+=d[(yy*w+w-128+x)*4+c];n++;
    }
    correction[y,c]=(a-b)/n;
   }
  }
  for(int y=top;y<h;y++)for(int x=0;x<w;x++){
   int i=(y*w+x)*4,dist=w-1-x;
   if(y>=borderBottom){
    double tint=Smooth((x-(w-1024))/1023.0);
    for(int c=0;c<3;c++)d[i+c]=Clamp(d[i+c]+correction[y,c]*tint);
    d[i+3]=255;
   }
   int period=dist%800, sx=period<400?period:799-period;
   int si=(y*w+sx)*4;
   if(y<=borderBottom){for(int c=0;c<4;c++)d[i+c]=s[si+c];}
   else if(y<borderBottom+64){
    double blend=1-Smooth((y-borderBottom)/64.0);
    for(int c=0;c<3;c++)d[i+c]=Clamp(d[i+c]*(1-blend)+s[si+c]*blend);
   }
   if(dist<192){
    double blend=1-Smooth((dist-16)/176.0);si=(y*w+dist)*4;
    for(int c=0;c<4;c++)d[i+c]=Clamp(d[i+c]*(1-blend)+s[si+c]*blend);
   }
  }
  // Register the five antialiased top rows to the adjacent panel's exact edge.
  for(int y=top;y<1200;y++)for(int x=0;x<w;x++)for(int c=0;c<4;c++)d[(y*w+x)*4+c]=s[y*w*4+c];
 }
 public static string Verify(byte[] d,byte[] s,int w,int h){
  int edges=0,clear=0,opaque=0,level=0;
  for(int y=0;y<h;y++)for(int c=0;c<4;c++)if(d[(y*w+w-1)*4+c]!=s[y*w*4+c])edges++;
  for(int y=0;y<1195;y++)for(int x=0;x<w;x++)for(int c=0;c<4;c++)if(d[(y*w+x)*4+c]!=0)clear++;
  for(int y=1224;y<h;y++)for(int x=0;x<w;x++)if(d[(y*w+x)*4+3]!=255)opaque++;
  for(int x=0;x<w;x++){int first=-1;for(int y=0;y<h;y++)if(d[(y*w+x)*4+3]>0){first=y;break;}if(first!=1195)level++;}
  if(edges+clear+opaque+level>0)throw new Exception("Validation failed: "+edges+","+clear+","+opaque+","+level);
  return "RGBA boundary differences: 0 across all 1844 rows. Transparent upper region: exact zero RGBA. Floor top: y=1195 in all 5120 columns. Lower carpet: fully opaque.";
 }
}
'@
[CasinoFloorJoin]::Align($floorData,$sourceData,5120,1844)
Set-PixelData $floor $floorData
$finalPath = Join-Path $floorDir '03-etuala-lattiajatko-5120x1844.png'
$floor.Save($finalPath,[System.Drawing.Imaging.ImageFormat]::Png)
$saved = [System.Drawing.Bitmap]::new($finalPath)
$report = [CasinoFloorJoin]::Verify((Pixel-Data $saved),$sourceData,5120,1844)
$saved.Dispose()
function Floor-Checker([System.Drawing.Bitmap]$image,[int]$size=32){
 $canvas=[System.Drawing.Bitmap]::new($image.Width,$image.Height,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[System.Drawing.Graphics]::FromImage($canvas)
 $colors=@([System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(46,46,46)),[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(57,57,57)))
 for($y=0;$y -lt $image.Height;$y+=$size){for($x=0;$x -lt $image.Width;$x+=$size){$idx=([int][Math]::Floor($x/$size)+[int][Math]::Floor($y/$size))%2;$g.FillRectangle($colors[$idx],$x,$y,$size,$size)}}
 $g.DrawImageUnscaled($image,0,0);$g.Dispose();$colors[0].Dispose();$colors[1].Dispose();return $canvas
}
$preview=Resize-Canvas $floor 1280 461
$checked=Floor-Checker $preview
$checked.Save((Join-Path $floorDir 'lattiajatko-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$checked.Dispose();$preview.Dispose()
$joined=[System.Drawing.Bitmap]::new(2560,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($joined)
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($floor,[System.Drawing.Rectangle]::new(0,0,1280,461));$g.DrawImage($source,[System.Drawing.Rectangle]::new(1280,0,1280,461));$g.Dispose()
$checked=Floor-Checker $joined
$checked.Save((Join-Path $floorDir 'liitos-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$checked.Dispose();$joined.Dispose()
$close=[System.Drawing.Bitmap]::new(1024,724,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($close)
$g.DrawImage($floor,[System.Drawing.Rectangle]::new(0,0,512,724),[System.Drawing.Rectangle]::new(4608,1120,512,724),[System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($source,[System.Drawing.Rectangle]::new(512,0,512,724),[System.Drawing.Rectangle]::new(0,1120,512,724),[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$checked=Floor-Checker $close
$checked.Save((Join-Path $floorDir 'liitos-lahikuva.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$checked.Dispose();$close.Dispose();$floor.Dispose();$source.Dispose()
if((Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash -ne $sourceHash){throw 'Reference source changed.'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'kasino-lattiajatko-generointiohje.txt') -Destination (Join-Path $floorDir 'generointiohje.txt')
@"
Kasinon etualan lattiajatko vasemmalle

03-etuala-lattiajatko-5120x1844.png on varsinainen pelikuva. Aseta se baaripalan 03-etuala-baari-5120x1844.png vasemmalle puolelle samoilla skaala-, PPU- ja pivot-asetuksilla. Älä rajaa läpinäkyvää yläosaa pois.
Koko 5120 x 1844, RGBA PNG. Yläosa on aidosti läpinäkyvä. Lattian yläreuna y=1195, sama kuin baaripalan vasemmassa reunassa. Lattiaa noin 35 prosenttia kuvan korkeudesta.
Tämä sisältää vain lattian ja vanhan kuvan kanssa kohdistetun kapean kultareunuksen. Uusia esineitä ei ole.
Esikatselujen harmaa ruudukko on vain esikatseluissa. Liitos-esikatselussa uusi pala on vasemmalla ja alkuperäinen baaripala oikealla.
Tekotapa: sisäänrakennettu image_gen, sen jälkeen koon sovitus, reunan kohdistus ja kapea liitosalueen värisovitus. Generoitu alkuperäinen oli $nativeSize; 5120 x 1844 on skaalattu vientikoko.
Alkuperäistä baarikuvaa ei muutettu.
"@ | Set-Content -LiteralPath (Join-Path $floorDir 'LUE-MINUT.txt') -Encoding UTF8
@("Native generated size: $nativeSize","Final: 5120x1844 RGBA PNG",$report,'Reference SHA256 unchanged: '+$sourceHash) | Set-Content -LiteralPath (Join-Path $floorDir 'tarkistus.txt') -Encoding UTF8
Write-Output "Saved: $finalPath"
Write-Output "Native: $nativeSize"
Write-Output $report
