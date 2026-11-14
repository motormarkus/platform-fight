$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$dir=Join-Path $PSScriptRoot 'kasino-kaukotausta-punainen'
[void][IO.Directory]::CreateDirectory($dir)
$raw=[Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-4e895077-c624-43e5-93bf-445cb43e5962.png')
$native='{0}x{1}' -f $raw.Width,$raw.Height
$final=[Drawing.Bitmap]::new(5120,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($final);$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy;$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attr=[Drawing.Imaging.ImageAttributes]::new();$attr.SetWrapMode([Drawing.Drawing2D.WrapMode]::TileFlipXY)
$g.DrawImage($raw,[Drawing.Rectangle]::new(0,0,5120,1199),[single]0,[single]0,[single]2089,[single]495,[Drawing.GraphicsUnit]::Pixel,$attr)
$g.DrawImage($raw,[Drawing.Rectangle]::new(0,1199,5120,645),[single]0,[single]499,[single]2089,[single]254,[Drawing.GraphicsUnit]::Pixel,$attr)
$g.Dispose()
# An exact repeating coffer, sampled from the generated top band, prevents
# truncated decorative motifs or doubled panel borders at a mirrored join.
$tile=[Drawing.Bitmap]::new(512,260,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($tile);$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy;$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.DrawImage($raw,[Drawing.Rectangle]::new(0,0,512,260),[single]8,[single]0,[single]190,[single](260*495.0/1199),[Drawing.GraphicsUnit]::Pixel,$attr)
$g.Dispose();$attr.Dispose()
$d=Pixel-Data $final;$t=Pixel-Data $tile
Add-Type -TypeDefinition @'
using System;
public static class RepeatableCasinoDistant {
 static double S(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
 public static void Finish(byte[] d,byte[] tile){
  const int w=5120,h=1844;
  for(int y=0;y<260;y++)for(int x=0;x<512;x++){
   int sx=x<256?x:511-x;
   if(x>=256)for(int c=0;c<4;c++)tile[(y*512+x)*4+c]=tile[(y*512+sx)*4+c];
  }
  for(int y=0;y<260;y++)for(int x=0;x<w;x++){
   int i=(y*w+x)*4,j=(y*512+x%512)*4;
   double a=1-S((y-244)/16.0);
   for(int c=0;c<4;c++)d[i+c]=(byte)Math.Round(d[i+c]*(1-a)+tile[j+c]*a);
  }
  // The right end repeats the quiet left band in reverse. The 8% zones are
  // identical under mirroring; no prop or pilaster lies in either zone.
  for(int y=0;y<h;y++)for(int dist=0;dist<640;dist++){
   int i=(y*w+w-1-dist)*4,j=(y*w+dist)*4;
   double a=1-S((dist-410)/229.0);
   for(int c=0;c<4;c++)d[i+c]=(byte)Math.Round(d[i+c]*(1-a)+d[j+c]*a);
  }
  for(int i=3;i<d.Length;i+=4)d[i]=255;
 }
 public static string Check(byte[] d){
  const int w=5120,h=1844;int edge=0,band=0,alpha=0;
  for(int y=0;y<h;y++){
   for(int c=0;c<4;c++)if(d[y*w*4+c]!=d[(y*w+w-1)*4+c])edge++;
   for(int x=0;x<410;x++)for(int c=0;c<4;c++)if(d[(y*w+x)*4+c]!=d[(y*w+w-1-x)*4+c])band++;
  }
  for(int i=3;i<d.Length;i+=4)if(d[i]!=255)alpha++;
  if(edge+band+alpha>0)throw new Exception("Edge="+edge+", 8 percent bands="+band+", alpha="+alpha);
  return "5120x1844 opaque PNG. Carpet begins at y1199 (65.0217%); lower carpet band=645px. Opposite edge differences=0 across all rows and RGBA channels. Left 410px and mirrored right 410px are byte-identical. Top coffer cycle=512px. No transparency.";
 }
}
'@
[RepeatableCasinoDistant]::Finish($d,$t)
Set-PixelData $final $d
$path=Join-Path $dir 'kasino-kaukotausta-punainen-5120x1844.png'
$final.Save($path,[Drawing.Imaging.ImageFormat]::Png)
$saved=[Drawing.Bitmap]::new($path)
$report=[RepeatableCasinoDistant]::Check((Pixel-Data $saved))
$small=Resize-Canvas $saved 1280 461
$small.Save((Join-Path $dir 'esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
foreach($mirror in @($false,$true)){
 $pair=[Drawing.Bitmap]::new(2560,461,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $second=$small.Clone([Drawing.Rectangle]::new(0,0,1280,461),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 if($mirror){$second.RotateFlip([Drawing.RotateFlipType]::RotateNoneFlipX)}
 $g=[Drawing.Graphics]::FromImage($pair);$g.DrawImageUnscaled($small,0,0);$g.DrawImageUnscaled($second,1280,0);$g.Dispose()
 $name=if($mirror){'peilattu-liitos-esikatselu.png'}else{'suora-liitos-esikatselu.png'}
 $pair.Save((Join-Path $dir $name),[Drawing.Imaging.ImageFormat]::Png)
 $pair.Dispose();$second.Dispose()
}
$close=[Drawing.Bitmap]::new(1024,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($close)
$g.DrawImage($saved,[Drawing.Rectangle]::new(0,0,512,1844),[Drawing.Rectangle]::new(4608,0,512,1844),[Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($saved,[Drawing.Rectangle]::new(512,0,512,1844),[Drawing.Rectangle]::new(0,0,512,1844),[Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$close.Save((Join-Path $dir 'liitos-lahikuva.png'),[Drawing.Imaging.ImageFormat]::Png)
$close.Dispose();$small.Dispose();$saved.Dispose();$final.Dispose();$tile.Dispose();$raw.Dispose()
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'kasino-kaukotausta-punainen-generointiohje.txt') -Destination (Join-Path $dir 'generointiohje.txt')
@"
Kasinon kaukotausta, punainen kokolattiamatto

kasino-kaukotausta-punainen-5120x1844.png
5120 x 1844 pikseliä, peittävä PNG. Suoraan edestä nähty kaukainen seinä, pieniä peliautomaatteja ja pöytiä, hillitty iltavalaistus. Ei tekstiä tai hahmoja. Punainen matto alkaa riviltä 1199 (65,02% korkeudesta) ja jatkuu kuvan alareunaan.
Molemmissa reunoissa noin 8% leveydeltä rauhallista seinää ja mattoa, ei kalusteita, pylväitä tai valaisimia. Vasemman 410 pikselin levyinen alue ja peilattu oikea 410 pikselin alue ovat identtiset; reunojen RGBA-arvot täsmäävät. Kattokoriste jatkuu samanlaisena 512 pikselin jaksoissa.
Voit jatkaa kuvaa joko sellaisenaan A | A tai vuorotellen peilattuna A | peili(A) | A. Käytä samaa PPU-, skaala- ja korkeusasetusta. Tarkista tarvittaessa mukana tulevat suoran ja peilatun liitoksen esikatselut.
Tekotapa: sisäänrakennettu image_gen, natiivikuva $native. Vientikuva skaalattu 5120 x 1844 kokoon; maton ja seinän vaakaraja kohdistettu, reunat ja kattokuvion toisto viimeistelty alkuperäisistä maalatuista alueista. Tämä on skaalattu vientikoko, ei natiivisti generoitu 5K-kuva. Aiempia kuvia tai Unityn scenejä ei muutettu.
"@ | Set-Content -LiteralPath (Join-Path $dir 'LUE-MINUT.txt') -Encoding UTF8
@($report,"Native generation=$native") | Set-Content -LiteralPath (Join-Path $dir 'tarkistus.txt') -Encoding UTF8
Write-Output $path
Write-Output $report
