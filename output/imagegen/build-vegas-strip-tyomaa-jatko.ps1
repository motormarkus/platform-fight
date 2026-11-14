$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
Add-Type -TypeDefinition @'
using System;
public static class WorksiteSecondTile {
 static double S(double x){x=Math.Max(0,Math.Min(1,x));return x*x*(3-2*x);}
 public static byte[] Build(byte[] raw,byte[] old){
  const int w=5120,h=1844; byte[] d=new byte[w*h*4];
  double[] sy={0,67,95,243,322,472,669,822,1200,1398,1428,1844};
  double[] dy={0,72,96,249,329,487,676,823,1194,1386,1416,1844};
  int seg=0;
  for(int y=0;y<h;y++){
   while(seg<dy.Length-2&&y>dy[seg+1])seg++;
   double yy=sy[seg]+(y-dy[seg])/(dy[seg+1]-dy[seg])*(sy[seg+1]-sy[seg]);
   int y0=Math.Min(h-1,(int)Math.Floor(yy)),y1=Math.Min(h-1,y0+1);double f=yy-y0;
   for(int x=0;x<w;x++){
    int i=(y*w+x)*4,a=(y0*w+x)*4,b=(y1*w+x)*4;
    double aa=raw[a+3]*(1-f)+raw[b+3]*f;d[i+3]=(byte)Math.Round(aa);
    if(aa>0)for(int c=0;c<3;c++)d[i+c]=(byte)Math.Round((raw[a+c]*raw[a+3]*(1-f)+raw[b+c]*raw[b+3]*f)/aa);
   }
  }
  for(int y=0;y<h;y++)for(int x=0;x<128;x++){
   double t=1-S((x-12)/116.0);int i=(y*w+x)*4,j=(y*w+w-1-x)*4;
   double a=d[i+3]*(1-t)+old[j+3]*t;
   for(int c=0;c<3;c++)d[i+c]=a>0?(byte)Math.Round((d[i+c]*d[i+3]*(1-t)+old[j+c]*old[j+3]*t)/a):(byte)0;
   d[i+3]=(byte)Math.Round(a);
  }
  return d;
 }
 public static string Check(byte[] d,byte[] old){
  int edge=0,holes=0,clear=0,hidden=0;
  for(int y=0;y<1844;y++)for(int c=0;c<4;c++)if(d[y*5120*4+c]!=old[(y*5120+5119)*4+c])edge++;
  for(int y=0;y<1844;y++)for(int x=0;x<5120;x++){
   int i=(y*5120+x)*4;if(y>=1300&&d[i+3]!=255)holes++;
   if(d[i+3]==0){clear++;if(d[i]!=0||d[i+1]!=0||d[i+2]!=0)hidden++;}
  }
  if(edge+holes+hidden>0)throw new Exception("Validation failed");
  return "5120x1844 RGBA. Adjacent-edge differences="+edge+"; floor holes="+holes+"; clear pixels="+clear+"; hidden RGB="+hidden;
 }
}
'@
$folder=Join-Path $PSScriptRoot 'vegas-strip-tyomaa-jatko'
[void][IO.Directory]::CreateDirectory($folder)
$oldPath=Join-Path $PSScriptRoot 'vegas-strip-tyomaa\vegas-strip-tyomaa-5120x1844.png'
$hash=(Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash
$old=[Drawing.Bitmap]::new($oldPath)
$raw=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-strip-tyomaa-jatko-tyo\tyomaa-raaka-5120x1844.png'))
$od=Pixel-Data $old;$data=[WorksiteSecondTile]::Build((Pixel-Data $raw),$od)
$null=[CasinoLeftPixels]::Normalize($data)
$report=[WorksiteSecondTile]::Check($data,$od)
$final=[Drawing.Bitmap]::new(5120,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
Set-PixelData $final $data
$final.Save((Join-Path $folder 'vegas-strip-tyomaa-jatko-5120x1844.png'),[Drawing.Imaging.ImageFormat]::Png)
$left=Resize-Canvas $old 1280 461;$right=Resize-Canvas $final 1280 461
$right.Save((Join-Path $folder 'etuala-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
$pair=[Drawing.Bitmap]::new(2560,461,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($pair);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImageUnscaled($left,0,0);$g.DrawImageUnscaled($right,1280,0);$g.Dispose()
$pair.Save((Join-Path $folder 'liitos-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$pair.Dispose()
$close=[Drawing.Bitmap]::new(1024,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($close);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImage($old,[Drawing.Rectangle]::new(0,0,512,1844),[Drawing.Rectangle]::new(4608,0,512,1844),[Drawing.GraphicsUnit]::Pixel);$g.DrawImage($final,[Drawing.Rectangle]::new(512,0,512,1844),[Drawing.Rectangle]::new(0,0,512,1844),[Drawing.GraphicsUnit]::Pixel);$g.Dispose()
$close.Save((Join-Path $folder 'liitos-lahikuva.png'),[Drawing.Imaging.ImageFormat]::Png);$close.Dispose()
$scene=[Drawing.Bitmap]::new(2560,461,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($scene)
foreach($name in @('01-yotaivas-5120x1844.png','02-hotellit-ja-rakennukset-5120x1844.png','03-kasvillisuus-5120x1844.png')){
 $layer=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot ('las-vegas-parallaksi-v2\'+$name)));$small=Resize-Canvas $layer 1280 461
 $g.DrawImageUnscaled($small,0,0);$small.RotateFlip([Drawing.RotateFlipType]::RotateNoneFlipX);$g.DrawImageUnscaled($small,1280,0);$small.Dispose();$layer.Dispose()
}
$g.DrawImageUnscaled($left,0,0);$g.DrawImageUnscaled($right,1280,0);$g.Dispose()
$scene.Save((Join-Path $folder 'tyomaa-ja-jatko-parallakseilla.png'),[Drawing.Imaging.ImageFormat]::Png)
$single=$scene.Clone([Drawing.Rectangle]::new(1280,0,1280,461),[Drawing.Imaging.PixelFormat]::Format32bppArgb);$single.Save((Join-Path $folder 'parallakseilla-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
$single.Dispose();$scene.Dispose();$left.Dispose();$right.Dispose();$final.Dispose();$old.Dispose();$raw.Dispose()
if((Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash -ne $hash){throw 'Source changed'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vegas-strip-tyomaa-jatko-generointiohje.txt') -Destination (Join-Path $folder 'generointiohje.txt')
@'
Tyomaan toinen katutason pala, 5120 x 1844 RGBA PNG.
Aseta vasen reuna vegas-strip-tyomaa-5120x1844.png-kuvan oikeaan reunaan samalla korkeudella ja skaalalla.
Tyomaa jatkuu koko ruudun lapi ja sen yli. Betonimylly, kaapelikela, sakkilava, telineet ja tikkaat. Kaukotausta on lapinakyva.
Betonipalkin, seinan, aidan ja reunakiven y-kohdistus sovitettu edelliseen kuvaan. Liitoksen reunapikselit tarkistettu, vanha kuva muuttumaton.
Sisaanrakennettu image_gen; natiivi 2167 x 726 sisalsi kapean liitosviitteen. Viite rajattu pois ja kuva skaalattu 5120 x 1844 vientikokoon. Alfa siivottu ja kapea liitosalue sovitettu.
'@ | Set-Content -LiteralPath (Join-Path $folder 'LUE-MINUT.txt') -Encoding UTF8
@($report,"Source SHA256 unchanged=$hash") | Set-Content -LiteralPath (Join-Path $folder 'tarkistus.txt') -Encoding UTF8
Write-Output $report
