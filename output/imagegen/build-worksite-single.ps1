$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
Add-Type -TypeDefinition @'
using System;
public static class WorksiteSinglePixels {
 public static void JoinLeft(byte[] dst,int w,int h,byte[] source,int sw,int sh,int collar){
  int offset=h-sh;
  for(int y=0;y<sh;y++)for(int x=0;x<collar;x++){
   double t=Math.Max(0,Math.Min(1,(x-24.0)/(collar-25.0))); t=t*t*(3-2*t);
   int o=((y+offset)*w+x)*4, s=(y*sw+x)*4;
   double sa=source[s+3]/255.0,da=dst[o+3]/255.0;
   double a=sa*(1-t)+da*t;
   for(int c=0;c<3;c++)dst[o+c]=a>0?(byte)Math.Round((source[s+c]*sa*(1-t)+dst[o+c]*da*t)/a):(byte)0;
   dst[o+3]=(byte)Math.Round(a*255);
  }
 }
 public static string Check(byte[] d,int w,int h,byte[] refEdge,int sw,int sh,int ground){
  long clear=0,hidden=0,holes=0,mismatch=0;int minx=w,maxx=-1,miny=h;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int p=(y*w+x)*4;
   if(d[p+3]==0){clear++;if(d[p]!=0||d[p+1]!=0||d[p+2]!=0)hidden++;}
   if(y>=ground+20&&d[p+3]!=255)holes++;
   if(y<ground-100&&d[p+3]>240){minx=Math.Min(minx,x);maxx=Math.Max(maxx,x);miny=Math.Min(miny,y);}
  }
  for(int y=0;y<sh;y++)for(int c=0;c<4;c++)if(d[((y+h-sh)*w)*4+c]!=refEdge[(y*sw)*4+c])mismatch++;
  if(mismatch!=0||hidden!=0||holes!=0)throw new Exception("QA failed: edge="+mismatch+", hidden="+hidden+", floor holes="+holes);
  return "RGBA edge differences vs previous street: "+mismatch+"\r\nTransparent pixels: "+clear+"; hidden RGB: "+hidden+"\r\nOpaque street holes: "+holes+"\r\nBuilding silhouette above street: x="+minx+".."+maxx+", roof top y="+miny;
 }
}
'@
$folder=Join-Path $PSScriptRoot 'vegas-tyomaa-yhtena'
$native=[Drawing.Bitmap]::new((Join-Path $folder 'tyomaa-natiivi.png'))
$w=15360;$h=5632;$sourceCropWidth=$native.Width/1.15;$sourceCropX=($native.Width-$sourceCropWidth)/2
$final=[Drawing.Bitmap]::new($w,$h,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($final);$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[Drawing.Imaging.ImageAttributes]::new();$attrs.SetWrapMode([Drawing.Drawing2D.WrapMode]::TileFlipXY)
$srcY=@(0,608,643,650,724);$dstY=@(0,4984,5174,5203,5632)
for($n=0;$n -lt 4;$n++){
 $g.DrawImage($native,[Drawing.Rectangle]::new(0,$dstY[$n],$w,($dstY[$n+1]-$dstY[$n])),[single]$sourceCropX,[single]$srcY[$n],[single]$sourceCropWidth,[single]($srcY[$n+1]-$srcY[$n]),[Drawing.GraphicsUnit]::Pixel,$attrs)
}
$attrs.Dispose();$g.Dispose();$native.Dispose()
$street=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-strip-tyomaa\vegas-strip-tyomaa-5120x1844.png'))
$pixels=Pixel-Data $final;$oldPixels=Pixel-Data $street
[WorksiteSinglePixels]::JoinLeft($pixels,$w,$h,$oldPixels,5120,1844,640)
$normalized=[CasinoLeftPixels]::Normalize($pixels)
Set-PixelData $final $pixels
$qa=[WorksiteSinglePixels]::Check($pixels,$w,$h,$oldPixels,5120,1844,4984)
$filename='vegas-tyomaa-yhtenainen-15360x5632.png'
$final.Save((Join-Path $folder $filename),[Drawing.Imaging.ImageFormat]::Png)
$small=Resize-Canvas $final 2400 880
$preview=[Drawing.Bitmap]::new(2400,880,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($preview);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImageUnscaled($small,0,0);$g.Dispose();$small.Dispose()
$preview.Save((Join-Path $folder 'esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$preview.Dispose()
$chapel=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-strip-haakappeli\vegas-strip-haakappeli-5120x1844.png'))
$seam=[Drawing.Bitmap]::new(1280,800,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($seam);$g.Clear([Drawing.Color]::FromArgb(34,40,58))
$g.DrawImage($chapel,[Drawing.Rectangle]::new(0,0,640,800),[Drawing.Rectangle]::new(4480,1044,640,800),[Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($final,[Drawing.Rectangle]::new(640,0,640,800),[Drawing.Rectangle]::new(0,4832,640,800),[Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$seam.Save((Join-Path $folder 'vasen-katuliitos-tarkistus.png'),[Drawing.Imaging.ImageFormat]::Png);$seam.Dispose();$chapel.Dispose()
@("15360 x 5632 RGBA PNG",'Single whole-scene native generation, 2172 x 724; exported at requested combined width.','Worksite widened 15 percent by centered crop and rescaling. Roof fully inside canvas.','Y registration: source 0,608,643,650,724 -> export 0,4984,5174,5203,5632.','Left street matches previous first tile and chapel right edge; images aligned along bottom.',$qa,$normalized) | Set-Content -LiteralPath (Join-Path $folder 'tarkistus.txt') -Encoding utf8
@'
YHTENAINEN TYOMAA

Pelitiedosto: vegas-tyomaa-yhtenainen-15360x5632.png
Yksi lapinakyva RGBA PNG. Leveys 3 x 5120 = 15360 pikselia; korkeus 5632 pikselia. Katto nakyy kokonaan. Rakennuksen leveytta kasvatettu 15 prosenttia uuden generoinnin alkuperaisesta sommittelusta, ja pystysuunnalle annettu lisaa tilaa.

Aseta kokonaisuus samalle PPU-arvolle kuin aiempi katu ja kohdista ALAREUNAT. Vasen reuna liittyy aiemman kappelikuvan oikeaan reunaan. PNG:n kadun takareuna on noin y=4984, reunakiven kirkas ylasyrja y=5174 ja alareuna y=5203. Ylhaalta mitatut koordinaatit.

Kuva tehtiin yhdella sisaanrakennetun image_gen-tyokalun generoinnilla vanhaa kuvaa tyyliviitteena kayttaen. Se ei ole kuuden vanhan palan yhdistelma. Natiivikuva 2172x724 on skaalattu 15360x5632 vientikokoon, joten vienti ei ole natiivia 15K-generointia. Kadun vasemmalla reunalla on kapea, asteittain sulautuva sovitus aiempaan katumateriaaliin.

Tausta ja rakenteiden valit ovat aidosti lapinakyvia. Esikatselun siniharmaa tausta ei ole pelitiedostossa. Unity-sceneja, tuontiasetuksia tai aiempia kuvia ei ole muutettu. Esikatselu.png on vain katselua varten. Generointiohje.txt sisaltaa alkuperaisen generointikehotteen; lopullinen koko ja 15 prosentin suurennus tehtiin kayttajan myohemman tarkennuksen mukaan.
'@ | Set-Content -LiteralPath (Join-Path $folder 'LUE-MINUT.txt') -Encoding utf8
$final.Dispose();$street.Dispose()
Write-Output $filename
Write-Output $qa
