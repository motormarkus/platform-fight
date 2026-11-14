$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
Add-Type -TypeDefinition @'
using System;
public static class VegasConstructionJoin {
 static double S(double x){x=Math.Max(0,Math.Min(1,x));return x*x*(3-2*x);}
 public static byte[] Build(byte[] raw,byte[] old){
  const int w=5120,h=1844; byte[] d=new byte[w*h*4];
  for(int y=0;y<h;y++){
   int sy=Math.Min(h-1,y+1);
   Buffer.BlockCopy(raw,sy*w*4,d,y*w*4,w*4);
  }
  // Remove low-alpha remnants only in the intentionally empty street air.
  for(int y=0;y<1196;y++)for(int x=0;x<2300;x++){
   int i=(y*w+x)*4;d[i]=d[i+1]=d[i+2]=d[i+3]=0;
  }
  // A narrow seam collar; preserve all generated paving joints beyond it.
  for(int y=0;y<h;y++)for(int x=0;x<128;x++){
   double a=1-S((x-12)/116.0);
   int i=(y*w+x)*4,j=(y*w+(w-1-x))*4;
   for(int c=0;c<4;c++)d[i+c]=(byte)Math.Round(d[i+c]*(1-a)+old[j+c]*a);
  }
  return d;
 }
 public static string Check(byte[] d,byte[] old){
  const int w=5120,h=1844;int edge=0,ground=0,hidden=0,air=0,clear=0;
  for(int y=0;y<h;y++)for(int c=0;c<4;c++)if(d[y*w*4+c]!=old[(y*w+w-1)*4+c])edge++;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int i=(y*w+x)*4;
   if(d[i+3]==0){clear++;if(d[i]!=0||d[i+1]!=0||d[i+2]!=0)hidden++;}
   if(y>=1300&&d[i+3]!=255)ground++;
   if(x<2250&&y<1196&&d[i+3]!=0)air++;
  }
  if(edge+ground+hidden+air>0)throw new Exception("Check failed: "+edge+","+ground+","+hidden+","+air);
  return "5120x1844 RGBA. Adjacent-edge RGBA differences="+edge+"; ground alpha holes="+ground+"; empty left air errors="+air+"; hidden RGB="+hidden+"; transparent pixels="+clear+".";
 }
}
'@
$folder=Join-Path $PSScriptRoot 'vegas-strip-tyomaa'
[void][IO.Directory]::CreateDirectory($folder)
$oldPath=Join-Path $PSScriptRoot 'vegas-strip-haakappeli\vegas-strip-haakappeli-5120x1844.png'
$hash=(Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash
$old=[Drawing.Bitmap]::new($oldPath)
$raw=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-strip-tyomaa-tyo\tyomaa-raaka-5120x1844.png'))
$od=Pixel-Data $old
$data=[VegasConstructionJoin]::Build((Pixel-Data $raw),$od)
$null=[CasinoLeftPixels]::Normalize($data)
$report=[VegasConstructionJoin]::Check($data,$od)
$final=[Drawing.Bitmap]::new(5120,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
Set-PixelData $final $data
$path=Join-Path $folder 'vegas-strip-tyomaa-5120x1844.png'
$final.Save($path,[Drawing.Imaging.ImageFormat]::Png)
$left=Resize-Canvas $old 1280 461;$right=Resize-Canvas $final 1280 461
$right.Save((Join-Path $folder 'etuala-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
$pair=[Drawing.Bitmap]::new(2560,461,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($pair);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImageUnscaled($left,0,0);$g.DrawImageUnscaled($right,1280,0);$g.Dispose()
$pair.Save((Join-Path $folder 'liitos-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$pair.Dispose()
$close=[Drawing.Bitmap]::new(1024,804,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($close);$g.Clear([Drawing.Color]::FromArgb(34,40,58))
$g.DrawImage($old,[Drawing.Rectangle]::new(0,0,512,804),[Drawing.Rectangle]::new(4608,1040,512,804),[Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($final,[Drawing.Rectangle]::new(512,0,512,804),[Drawing.Rectangle]::new(0,1040,512,804),[Drawing.GraphicsUnit]::Pixel);$g.Dispose()
$close.Save((Join-Path $folder 'liitos-lahikuva.png'),[Drawing.Imaging.ImageFormat]::Png);$close.Dispose()
$scene=[Drawing.Bitmap]::new(2560,461,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($scene)
foreach($name in @('01-yotaivas-5120x1844.png','02-hotellit-ja-rakennukset-5120x1844.png','03-kasvillisuus-5120x1844.png')){
 $layer=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot ('las-vegas-parallaksi-v2\'+$name)))
 $small=Resize-Canvas $layer 1280 461
 $g.DrawImageUnscaled($small,1280,0)
 $small.RotateFlip([Drawing.RotateFlipType]::RotateNoneFlipX);$g.DrawImageUnscaled($small,0,0)
 $small.Dispose();$layer.Dispose()
}
$g.DrawImageUnscaled($left,0,0);$g.DrawImageUnscaled($right,1280,0);$g.Dispose()
$scene.Save((Join-Path $folder 'kappeli-ja-tyomaa-parallakseilla.png'),[Drawing.Imaging.ImageFormat]::Png)
$single=$scene.Clone([Drawing.Rectangle]::new(1280,0,1280,461),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$single.Save((Join-Path $folder 'tyomaa-parallakseilla-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
$single.Dispose();$scene.Dispose();$left.Dispose();$right.Dispose();$final.Dispose();$old.Dispose();$raw.Dispose()
if((Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash -ne $hash){throw 'Chapel source changed'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vegas-strip-tyomaa-generointiohje.txt') -Destination (Join-Path $folder 'generointiohje.txt')
@'
Las Vegas Strip: avoin katu ja rakennustyomaan alku.
Pelikuva: vegas-strip-tyomaa-5120x1844.png, 5120 x 1844 RGBA PNG.
Sijoita taman kuvan vasen reuna kappelikuvan vegas-strip-haakappeli-5120x1844.png oikeaan reunaan samalla korkeudella, PPU:lla ja skaalalla.
Ensin tilava tyhja katuosuus, sitten metalliaita, tyomaan sisaanmeno, harkkolava ja teraspalkkeja. Keskenerainen betonirunko ja telineet jatkuvat yla- ja oikean reunan yli.
Jalkakaytava, reunakivi ja asfaltti liittyvat kappelikuvan katuun. Kaikki kaukotausta ja avoimet rakennusaukot lapinakyvia erillista parallaksia varten. Kuvaparin parallaksiesikatseluissa aiemmat Vegas-tasot on yhdistetty vain havainnollistamista varten, ei varsinaiseen etualan PNG:hen.
Tuotanto: sisaanrakennettu image_gen-kuvatyokalu. Natiivi 2167 x 726 sisalsi vasemmalla liitosviitteen; viite rajattu pois ja uusi kuva skaalattu 5120 x 1844 kokoon. Alfa siivottu, y-kohdistus -1 pikseli ja kapea 128 pikselin liitosalue sovitettu edellisen kuvan reunaan.
Kappelin alkuperainen kuva ja Unity-scenet/asetukset sailytetty muuttamattomina.
'@ | Set-Content -LiteralPath (Join-Path $folder 'LUE-MINUT.txt') -Encoding UTF8
@($report,"Chapel SHA256 unchanged=$hash") | Set-Content -LiteralPath (Join-Path $folder 'tarkistus.txt') -Encoding UTF8
Write-Output $path
Write-Output $report
