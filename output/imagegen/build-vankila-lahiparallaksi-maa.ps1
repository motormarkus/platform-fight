$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$soilDir=Join-Path $PSScriptRoot 'vankila-lahiparallaksi-maa'
[void][System.IO.Directory]::CreateDirectory($soilDir)
$rawPath='C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-50c92d67-5e58-4b74-a0c9-31594c43ce1c.png'
$raw=[System.Drawing.Bitmap]::new($rawPath)
$native='{0}x{1}' -f $raw.Width,$raw.Height
# Remove almost-transparent matte fringe before filtering/resizing.
$rawAlpha=Clean-Alpha $raw
$soil=Resize-Canvas $raw 5120 1844
$raw.Dispose()
$exportAlpha=Clean-Alpha $soil
$finalPath=Join-Path $soilDir 'vankila-lahimaa-hiekka-sora-5120x1844.png'
$soil.Save($finalPath,[System.Drawing.Imaging.ImageFormat]::Png)
Add-Type -TypeDefinition @'
using System;
public static class PrisonSoilCheck {
 public static string Verify(byte[] d,int w,int h){
  int bottomErrors=0,topErrors=0,hidden=0,transparent=0,minY=h,maxFirst=0;
  for(int x=0;x<w;x++){
   int first=-1;
   for(int y=0;y<h;y++){
    int i=(y*w+x)*4;byte a=d[i+3];
    if(a==0){transparent++;if(d[i]!=0||d[i+1]!=0||d[i+2]!=0)hidden++;}
    if(first<0&&a>0)first=y;
    if(y<h/4&&a!=0)topErrors++;
    if(y>=h/2&&a!=255)bottomErrors++;
   }
   minY=Math.Min(minY,first);maxFirst=Math.Max(maxFirst,first);
  }
  if(bottomErrors+topErrors+hidden>0)throw new Exception("Alpha validation: "+bottomErrors+","+topErrors+","+hidden);
  return "Transparent pixels="+transparent+"; hidden RGB errors=0; upper quarter clear; lower half fully opaque; first terrain/vegetation pixels range y="+minY+".."+maxFirst+".";
 }
 public static string Mirror(byte[] a,byte[] b,int w,int h){
  int error=0;
  for(int y=0;y<h;y++)for(int c=0;c<4;c++){
   if(a[(y*w+w-1)*4+c]!=b[(y*w)*4+c])error++;
   if(b[(y*w+w-1)*4+c]!=a[(y*w)*4+c])error++;
  }
  if(error>0)throw new Exception("Mirror edge mismatch: "+error);
  return "A / mirrored A / A: RGBA boundary differences=0 at both seams, all 1844 rows.";
 }
}
'@
$saved=[System.Drawing.Bitmap]::new($finalPath)
$data=Pixel-Data $saved
$report=[PrisonSoilCheck]::Verify($data,5120,1844)
$mirrored=$saved.Clone([System.Drawing.Rectangle]::new(0,0,5120,1844),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$mirrored.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX)
$mirrorReport=[PrisonSoilCheck]::Mirror($data,(Pixel-Data $mirrored),5120,1844)
function Soil-Checker([System.Drawing.Bitmap]$image){
 $result=[System.Drawing.Bitmap]::new($image.Width,$image.Height,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[System.Drawing.Graphics]::FromImage($result)
 $brushes=@([System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(48,48,48)),[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(66,66,66)))
 for($y=0;$y -lt $image.Height;$y+=32){for($x=0;$x -lt $image.Width;$x+=32){$idx=([int][Math]::Floor($x/32)+[int][Math]::Floor($y/32))%2;$g.FillRectangle($brushes[$idx],$x,$y,32,32)}}
 $g.DrawImageUnscaled($image,0,0);$g.Dispose();$brushes[0].Dispose();$brushes[1].Dispose();return $result
}
$small=Resize-Canvas $saved 1280 461
$checked=Soil-Checker $small
$checked.Save((Join-Path $soilDir 'maa-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$checked.Dispose();$small.Dispose()
$loop=[System.Drawing.Bitmap]::new(3000,360,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$loopA=Resize-Canvas $saved 1000 360
$loopB=$loopA.Clone([System.Drawing.Rectangle]::new(0,0,1000,360),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$loopB.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX)
$g=[System.Drawing.Graphics]::FromImage($loop);$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.DrawImageUnscaled($loopA,0,0);$g.DrawImageUnscaled($loopB,1000,0);$g.DrawImageUnscaled($loopA,2000,0);$g.Dispose();$loopA.Dispose();$loopB.Dispose()
$checked=Soil-Checker $loop
$checked.Save((Join-Path $soilDir 'peilattu-toisto-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$checked.Dispose();$loop.Dispose()
# Demonstrate overlap without changing the game's road or scene.
$roadPath=Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'Assets\Sprites\Taustat\vankila_sisa_2.png'
$road=[System.Drawing.Bitmap]::new($roadPath)
$roadHash=(Get-FileHash -LiteralPath $roadPath -Algorithm SHA256).Hash
$comparison=[System.Drawing.Bitmap]::new(1672,1241,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($comparison);$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$groundWidth=[int][Math]::Round(5120.0*941/1844)
$g.DrawImage($saved,[System.Drawing.Rectangle]::new(-350,300,$groundWidth,941))
$g.DrawImageUnscaled($road,0,0);$g.Dispose()
$cropped=$comparison.Clone([System.Drawing.Rectangle]::new(0,500,1672,741),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$checked=Soil-Checker $cropped
$checked.Save((Join-Path $soilDir 'tie-ja-maa-limitys-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$checked.Dispose();$cropped.Dispose();$comparison.Dispose();$road.Dispose()
if((Get-FileHash -LiteralPath $roadPath -Algorithm SHA256).Hash -ne $roadHash){throw 'Road changed.'}
$mirrored.Dispose();$saved.Dispose();$soil.Dispose()
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vankila-lahiparallaksi-maa-generointiohje.txt') -Destination (Join-Path $soilDir 'generointiohje.txt')
@"
Vankilan lähiparallaksi: hiekkasoramaa

Pelikuva: vankila-lahimaa-hiekka-sora-5120x1844.png, RGBA PNG.
Tasainen hiekkasoramaa, muutamia pieniä kiviä ja niukat matalat ruohotuppaat. Yläosa on läpinäkyvä; maa jatkuu täysin peittävänä kuvan alareunaan. Ei tietä tai rakennuksia kuvassa.

Sijoita maakerros nykyisen tien taakse ja kaukaisen vankilataustan eteen. Jätä maata reilusti tien alle: maan alareunan pitää ulottua tien alareunaa alemmas myös kameran liikkuessa. Älä trimmaa läpinäkyviä marginaaleja automaattisesti.
Limitysesikatselussa kuvan korkeus on sovitettu tien 941 px korkeuteen, maakuvaa siirretty 300 px alaspäin ja asfaltti piirretty sen päälle. Esikatselu on rajattu maahan ja tiehen; nämä ovat sommitteluarvot, eivät Unityyn tehdyt muutokset.
Toisto: kuva A, vaakasuunnassa peilattu A, A, peilattu A. Molemmat liitokset vastaavat RGBA-arvoiltaan toisiaan. Tiedostoa voi peilata SpriteRenderer.flipX-asetuksella. Normaali A/A-toisto ei ole tämän kuvan tarkoitettu toistotapa.
Liikenopeudeksi voi kokeilla nykyisen vankilataustan ja tien väliin sijoittuvaa arvoa. Kamera- ja sijaintisovitus tarkistetaan pelissä. Tämän toimituksen aikana Unityn sceneä tai asetuksia ei muutettu.

Tekotapa: sisäänrakennettu image_gen, yksi pohjagenerointi ja maapinnan laajennus samasta kuvasta. Natiivikoko $native; 5120 x 1844 on skaalattu vientikoko. Lähes läpinäkyvä reunaväri siivottu ennen skaalausta. PNG:n alfa ja peilausliitokset tarkistettu.
Esikatseluissa on harmaa ruudukko; sitä ei ole varsinaisessa PNG:ssä.
"@ | Set-Content -LiteralPath (Join-Path $soilDir 'LUE-MINUT.txt') -Encoding UTF8
@("Native=$native",'Export=5120x1844 RGBA PNG',"Native alpha cleanup: $rawAlpha","Export alpha cleanup: $exportAlpha",$report,$mirrorReport,"Original road unchanged: SHA256 $roadHash") | Set-Content -LiteralPath (Join-Path $soilDir 'tarkistus.txt') -Encoding UTF8
Write-Output $finalPath
Write-Output $report
Write-Output $mirrorReport
