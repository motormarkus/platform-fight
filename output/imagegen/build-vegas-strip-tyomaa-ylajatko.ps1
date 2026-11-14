$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
Add-Type -TypeDefinition @'
using System;
public static class WorksiteUpper {
 static double S(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
 public static byte[] Build(byte[] src,byte[] repair,byte[] lower){
  const int w=5120,h=1844;byte[] d=(byte[])src.Clone();
  for(int y=1360;y<h;y++)for(int x=2080;x<2470;x++){
   int i=(y*w+x)*4,j=((y-1144)*650+x-1850)*4;
   double t=S((x-2080)/30.0)*(1-S((x-2440)/30.0))*S((y-1360)/22.0);
   double a=d[i+3]*(1-t)+repair[j+3]*t;
   for(int c=0;c<3;c++)d[i+c]=a>0?(byte)Math.Round((d[i+c]*d[i+3]*(1-t)+repair[j+c]*repair[j+3]*t)/a):(byte)0;
   d[i+3]=(byte)Math.Round(a);
  }
  byte[] copy=(byte[])d.Clone();
  double[] sx={0,174,212,450,501,663,699,1196,1399,1414,1451,1777,1826,2017,2054,2400,5119};
  double[] dx={0,175,203,447,493,665,695,1207,1399,1417,1446,1780,1825,2022,2051,2400,5119};
  for(int y=1560;y<h;y++){
   double amount=S((y-1560)/283.0);int seg=0;
   for(int x=0;x<w;x++){
    while(seg<dx.Length-2&&x>dx[seg+1])seg++;
    double mapped=sx[seg]+(x-dx[seg])/(dx[seg+1]-dx[seg])*(sx[seg+1]-sx[seg]);
    double xx=x+(mapped-x)*amount;int x0=Math.Max(0,Math.Min(w-1,(int)Math.Floor(xx))),x1=Math.Min(w-1,x0+1);double f=xx-x0;
    int i=(y*w+x)*4,a=(y*w+x0)*4,b=(y*w+x1)*4;
    double aa=copy[a+3]*(1-f)+copy[b+3]*f;d[i+3]=(byte)Math.Round(aa);
    for(int c=0;c<3;c++)d[i+c]=aa>0?(byte)Math.Round((copy[a+c]*copy[a+3]*(1-f)+copy[b+c]*copy[b+3]*f)/aa):(byte)0;
   }
  }
  for(int y=0;y<h;y++)for(int x=2450;x<w;x++){int i=(y*w+x)*4;d[i]=d[i+1]=d[i+2]=d[i+3]=0;}
  for(int y=h-5;y<h;y++)for(int x=0;x<w;x++){
   int i=(y*w+x)*4,j=x*4;double t=S((y-(h-5))/4.0),a=d[i+3]*(1-t)+lower[j+3]*t;
   for(int c=0;c<3;c++)d[i+c]=a>0?(byte)Math.Round((d[i+c]*d[i+3]*(1-t)+lower[j+c]*lower[j+3]*t)/a):(byte)0;
   d[i+3]=(byte)Math.Round(a);
  }
  return d;
 }
}
'@
$folder=Join-Path $PSScriptRoot 'vegas-strip-tyomaa-ylajatko';[void][IO.Directory]::CreateDirectory($folder)
$raw=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-strip-tyomaa-ylajatko-tyo\yla-raaka-5120x1844.png'))
$fixRaw=[Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-80a05f50-9af0-4d6d-9310-714cef9a058b.png')
$null=Clean-Alpha $fixRaw;$fix=Resize-Canvas $fixRaw 650 700;$null=Clean-Alpha $fix
$lower=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-strip-tyomaa-paaty\vegas-strip-tyomaa-paaty-5120x1844.png'))
$ld=Pixel-Data $lower;$data=[WorksiteUpper]::Build((Pixel-Data $raw),(Pixel-Data $fix),$ld)
$null=[CasinoLeftPixels]::Normalize($data)
# Preserve the already finished lower edge's exact antialiasing.
[Buffer]::BlockCopy($ld,0,$data,1843*5120*4,5120*4)
$final=[Drawing.Bitmap]::new(5120,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb);Set-PixelData $final $data
$final.Save((Join-Path $folder 'vegas-strip-tyomaa-ylajatko-5120x1844.png'),[Drawing.Imaging.ImageFormat]::Png)
$small=Resize-Canvas $final 1280 461;$small.Save((Join-Path $folder 'etuala-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$small.Dispose()
$pair=[Drawing.Bitmap]::new(1280,922,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($pair);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImage($final,[Drawing.Rectangle]::new(0,0,1280,461));$g.DrawImage($lower,[Drawing.Rectangle]::new(0,461,1280,461));$g.Dispose()
$pair.Save((Join-Path $folder 'ylajatko-ja-paaty-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$pair.Dispose()
$close=[Drawing.Bitmap]::new(2560,320,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($close);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImage($final,[Drawing.Rectangle]::new(0,0,2560,160),[Drawing.Rectangle]::new(0,1524,5120,320),[Drawing.GraphicsUnit]::Pixel);$g.DrawImage($lower,[Drawing.Rectangle]::new(0,160,2560,160),[Drawing.Rectangle]::new(0,0,5120,320),[Drawing.GraphicsUnit]::Pixel);$g.Dispose()
$close.Save((Join-Path $folder 'pystyliitos-lahikuva.png'),[Drawing.Imaging.ImageFormat]::Png);$close.Dispose()
$edge=0;for($i=0;$i -lt 5120*4;$i++){if($data[1843*5120*4+$i] -ne $ld[$i]){$edge++}}
if($edge -ne 0){throw 'Vertical edge mismatch'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vegas-strip-tyomaa-ylajatko-generointiohje.txt') -Destination (Join-Path $folder 'generointiohje.txt')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vegas-strip-tyomaa-ylajatko-pilarikorjaus.txt') -Destination (Join-Path $folder 'liitoskorjauksen-generointiohje.txt')
@'
Tyomaan ylapala, 5120 x 1844 RGBA PNG.
Sijoita suoraan vegas-strip-tyomaa-paaty-5120x1844.png-kuvan YLAPUOLELLE samalla x-kohdalla ja skaalalla. Taman kuvan alareuna liittyy paatypalan ylareunaan.
Rakennus jatkuu vasemmalla puoliskolla ylospain; oikea puoli lapinakyva. Betonitasoja, telineita, tikkaita, lankkuja ja kaapelia. Ei katua tassa ylapalassa.
Pilarien ja vinotukien x-kohdistus sovitettu pystysaumaan. Ylimaarainen roikkuva alapilari poistettu paikallisella kuvatyokalu-muokkauksella. Reunapikselit ovat yhteensopivat kaikissa RGBA-kanavissa.
Sisaanrakennettu image_gen. Natiivi 1900 x 828 sisalsi alareunassa liitosviitteen; viite poistettu ja uusi osa skaalattu 5120 x 1844 vientikokoon.
'@ | Set-Content -LiteralPath (Join-Path $folder 'LUE-MINUT.txt') -Encoding UTF8
"5120x1844 RGBA; vertical adjacent-edge channel differences=$edge" | Set-Content -LiteralPath (Join-Path $folder 'tarkistus.txt') -Encoding UTF8
$final.Dispose();$raw.Dispose();$fixRaw.Dispose();$fix.Dispose();$lower.Dispose()
Write-Output "Vertical edge differences=$edge"
