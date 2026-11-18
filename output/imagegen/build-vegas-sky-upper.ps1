$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
Add-Type -TypeDefinition @'
using System;
public static class VegasSkyUpperPixels {
 public static void Join(byte[] upper,int w,int h,byte[] old,int collar){
  var means=new double[collar,3];
  for(int r=0;r<collar;r++)for(int x=0;x<w;x++)for(int c=0;c<3;c++)means[r,c]+=old[(r*w+x)*4+c]/(double)w;
  for(int y=h-collar;y<h;y++){
   int r=h-1-y;double t=r/(double)(collar-1);t=t*t*(3-2*t);
   for(int x=0;x<w;x++){
    int i=(y*w+x)*4,o=(r*w+x)*4;
    for(int c=0;c<3;c++){
     double continuation=Math.Max(0,Math.Min(255,old[o+c]+means[0,c]-means[r,c]));
     upper[i+c]=(byte)Math.Round(upper[i+c]*t+continuation*(1-t));
    }
    upper[i+3]=255;
   }
  }
 }
 public static string Verify(byte[] upper,int w,int h,byte[] old){
  long edge=0,holes=0;
  for(int x=0;x<w;x++)for(int c=0;c<4;c++)if(upper[((h-1)*w+x)*4+c]!=old[x*4+c])edge++;
  for(int i=3;i<upper.Length;i+=4)if(upper[i]!=255)holes++;
  if(edge!=0||holes!=0)throw new Exception("Sky verification failed");
  return "Vertical seam RGBA channel differences="+edge+"; nonopaque pixels="+holes;
 }
 public static long CompareOld(byte[] combined,byte[] old,int offsetBytes){long n=0;for(int i=0;i<old.Length;i++)if(combined[i+offsetBytes]!=old[i])n++;return n;}
}
'@
$folder=Join-Path $PSScriptRoot 'vegas-yotaivas-ylajatko'
$project=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$source=Join-Path $project 'Assets\Sprites\Taustat\vegas_taivas.png'
$rawPath='C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-d94b3c27-c8a0-480e-bea2-ee0d26b0f1f1.png'
$raw=[Drawing.Bitmap]::new($rawPath);$nativeDescription="$($raw.Width)x$($raw.Height) including the reference strip"
$old=[Drawing.Bitmap]::new($source);$oldHash=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$w=5120;$newH=3788;$totalH=5632
$upper=[Drawing.Bitmap]::new($w,$newH,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($upper);$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[Drawing.Imaging.ImageAttributes]::new();$attrs.SetWrapMode([Drawing.Drawing2D.WrapMode]::TileFlipXY)
$g.DrawImage($raw,[Drawing.Rectangle]::new(0,0,$w,$newH),[single]0,[single]0,[single]$raw.Width,[single]($raw.Height*947/1043),[Drawing.GraphicsUnit]::Pixel,$attrs)
$attrs.Dispose();$g.Dispose();$raw.Dispose()
$u=Pixel-Data $upper;$o=Pixel-Data $old
[VegasSkyUpperPixels]::Join($u,$w,$newH,$o,384)
Set-PixelData $upper $u
$qa=[VegasSkyUpperPixels]::Verify($u,$w,$newH,$o)
$upper.Save((Join-Path $folder 'vegas-yotaivas-ylajatko-5120x3788.png'),[Drawing.Imaging.ImageFormat]::Png)
$combined=[Drawing.Bitmap]::new($w,$totalH,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$combinedPixels=[byte[]]::new($w*$totalH*4)
[Buffer]::BlockCopy($u,0,$combinedPixels,0,$u.Length)
[Buffer]::BlockCopy($o,0,$combinedPixels,$u.Length,$o.Length)
Set-PixelData $combined $combinedPixels
$c=Pixel-Data $combined;$oldDifferences=[VegasSkyUpperPixels]::CompareOld($c,$o,$newH*$w*4)
if($oldDifferences -ne 0){throw 'Original sky changed'}
$combined.Save((Join-Path $folder 'vegas-yotaivas-korkea-5120x5632.png'),[Drawing.Imaging.ImageFormat]::Png)
$mirror=$combined.Clone();$mirror.RotateFlip([Drawing.RotateFlipType]::RotateNoneFlipX)
$wide=[Drawing.Bitmap]::new(15360,5632,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($wide);$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy;$g.DrawImageUnscaled($combined,0,0);$g.DrawImageUnscaled($mirror,5120,0);$g.DrawImageUnscaled($combined,10240,0);$g.Dispose()
$horizontal=0
for($y=0;$y -lt 5632;$y++){
 if($wide.GetPixel(5119,$y).ToArgb() -ne $wide.GetPixel(5120,$y).ToArgb()){$horizontal++}
 if($wide.GetPixel(10239,$y).ToArgb() -ne $wide.GetPixel(10240,$y).ToArgb()){$horizontal++}
}
if($horizontal -ne 0){throw 'Horizontal sky join changed'}
$wide.Save((Join-Path $folder 'vegas-yotaivas-koko-tyomaan-leveys-15360x5632.png'),[Drawing.Imaging.ImageFormat]::Png)
$singlePreview=Resize-Canvas $combined 960 1056
$singlePreview.Save((Join-Path $folder 'taivas-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$singlePreview.Dispose()
$preview=Resize-Canvas $wide 2400 880
$preview.Save((Join-Path $folder 'levea-taivas-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
$site=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-tyomaa-yhtena\vegas-tyomaa-yhtenainen-15360x5632.png'))
$siteSmall=Resize-Canvas $site 2400 880
$g=[Drawing.Graphics]::FromImage($preview);$g.DrawImageUnscaled($siteSmall,0,0);$g.Dispose()
$preview.Save((Join-Path $folder 'tyomaan-kanssa-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
@("Native generation: $nativeDescription",$qa,"Original bottom 5120x1844 retained unchanged: RGBA channel differences=$oldDifferences","A / mirror(A) / A horizontal seam mismatched pixels=$horizontal","Source SHA256=$oldHash",'Asset generation with builtin image_gen. Resampled export sizes; not native 5K/15K generation.') | Set-Content -LiteralPath (Join-Path $folder 'tarkistus.txt') -Encoding utf8
@'
VEGASIN YOTAIVAAN SAUMATON YLAJATKO

Lahde: Assets/Sprites/Taustat/vegas_taivas.png, 5120x1844. Tama on projektin kaytossa oleva kauimmainen parallaksitaso.

1. vegas-yotaivas-ylajatko-5120x3788.png: pelkka uusi ylaosa. Aseta suoraan alkuperaisen taivaan ylapuolelle samalla skaalalla ja samalla vaakasuuntaisella kohdistuksella. Uuden alareuna vastaa alkuperaisen ylareunaa kaikista RGBA-kanavista.
2. vegas-yotaivas-korkea-5120x5632.png: ylaosa ja alkuperainen taivas valmiiksi yhdessa. Alkuperainen kuva on sailytetty sellaisenaan alimmassa 1844 pikselissa.
3. vegas-yotaivas-koko-tyomaan-leveys-15360x5632.png: sama korkea taivas kolmesti, jarjestyksessa A / vaakapeili(A) / A. Tama on saman kokoinen kuin viimeisin yhtenainen tyomaakuva. Peilauksen vuoksi molemmat vaakasaumat tasmaavat.

Taivas on kokonaan peittava, koska se on kauimmainen taso. Hotellit ja kasvillisuus sailytetaan erillisina aiempina parallaksikerroksina. Esikatselu tyomaan kanssa nayttaa vain uuden taivaan ja tyomaan.

Ylaosan ja alkuperaisen liitos on sovitettu 384 pikselin matkalla, alkuperaista muuttamatta. Jos kaytat yhtenaista versiota, kohdista alareuna vanhan taivaan alareunaan ja kayta samaa PPU-arvoa. Jos peilaat vaakasuunnassa, peilaa koko pystypino samalla tavalla.

Sisaanrakennettu image_gen-kuvatyokalu; uusi osa skaalattu vientikokoon. Generointiohje tiedostossa generointiohje.txt. Unity-sceneja, tuontiasetuksia tai alkuperaista taivastiedostoa ei ole muutettu.
'@ | Set-Content -LiteralPath (Join-Path $folder 'LUE-MINUT.txt') -Encoding utf8
foreach($im in @($upper,$old,$combined,$mirror,$wide,$preview,$site,$siteSmall)){$im.Dispose()}
"Native: $nativeDescription";$qa;"Original sky differences=$oldDifferences; mirror seams=$horizontal"
