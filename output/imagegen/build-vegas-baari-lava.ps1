$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$outDir=Join-Path $PSScriptRoot 'vegas-baari-sisatilat-lava'
[void][System.IO.Directory]::CreateDirectory($outDir)
$oldPath=Join-Path $PSScriptRoot 'vegas-baari-sisatilat-vasen\vegas-baari-sisatilat-vasen-5120x1844.png'
$oldHash=(Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash
$old=[System.Drawing.Bitmap]::new($oldPath)
$cropped=[System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-baari-lava-tyo\raaka-5120x1844.png'))
$final=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($final);$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[System.Drawing.Imaging.ImageAttributes]::new();$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
# Measured neon, horizontal brass bands, counter top and footrail alignment.
$srcRows=@(0,24,57,131,173,203,675,710,737,765,1072,1134,1166,1844)
$dstRows=@(0,24,54,124,162,191,634,662,689,718,994,1054,1084,1844)
for($i=0;$i -lt $srcRows.Count-1;$i++){
 $g.DrawImage($cropped,[System.Drawing.Rectangle]::new(0,$dstRows[$i],5120,($dstRows[$i+1]-$dstRows[$i])),[single]0,[single]$srcRows[$i],[single]5120,[single]($srcRows[$i+1]-$srcRows[$i]),[System.Drawing.GraphicsUnit]::Pixel,$attrs)
}
$g.Dispose();$attrs.Dispose();$cropped.Dispose()
$data=Pixel-Data $final;$oldData=Pixel-Data $old
Add-Type -TypeDefinition @'
using System;
public static class VegasStageJoin {
 static double Smooth(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
 public static void Join(byte[] d,byte[] old,int w,int h){
  // Continue the existing clear floor without a texture or floorboard break.
  for(int y=1088;y<h;y++)for(int x=0;x<w;x++){
   int p=(y*w+x)*4,q=(y*w+w-1-x)*4;double a=Smooth((y-1088)/32.0);
   for(int c=0;c<4;c++)d[p+c]=(byte)Math.Round(d[p+c]*(1-a)+old[q+c]*a);
  }
  // Thin collar only: preserve the generated upright immediately beside the seam.
  for(int y=0;y<h;y++)for(int x=0;x<64;x++){
   int p=(y*w+x)*4,q=(y*w+w-1-x)*4;double a=1-Smooth((x-6)/57.0);
   for(int c=0;c<4;c++)d[p+c]=(byte)Math.Round(d[p+c]*(1-a)+old[q+c]*a);
  }
 }
 public static string Check(byte[] d,byte[] old,int w,int h){
  int edge=0,alpha=0;
  for(int y=0;y<h;y++)for(int c=0;c<4;c++)if(d[(y*w)*4+c]!=old[(y*w+w-1)*4+c])edge++;
  for(int i=3;i<d.Length;i+=4)if(d[i]!=255)alpha++;
  if(edge+alpha>0)throw new Exception("Join errors="+edge+"; non-opaque="+alpha);
  return "Left-to-previous-right boundary: 0 RGBA differences across all 1844 rows. All pixels opaque. Neon centers y54 and y124, counter highlights y662 and y689, footrail center y994. Lower floor texture continues from the preceding room. Visual join review required in addition to edge values.";
 }
}
'@
[VegasStageJoin]::Join($data,$oldData,5120,1844)
Set-PixelData $final $data
$finalPath=Join-Path $outDir 'vegas-baari-sisatilat-lava-5120x1844.png'
$final.Save($finalPath,[System.Drawing.Imaging.ImageFormat]::Png)
$saved=[System.Drawing.Bitmap]::new($finalPath)
$report=[VegasStageJoin]::Check((Pixel-Data $saved),$oldData,5120,1844)
$newSmall=Resize-Canvas $saved 1280 461;$oldSmall=Resize-Canvas $old 1280 461
$newSmall.Save((Join-Path $outDir 'esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$pair=[System.Drawing.Bitmap]::new(2560,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($pair);$g.DrawImageUnscaled($oldSmall,0,0);$g.DrawImageUnscaled($newSmall,1280,0);$g.Dispose()
$pair.Save((Join-Path $outDir 'liitos-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png);$pair.Dispose()
$close=[System.Drawing.Bitmap]::new(1024,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($close)
$g.DrawImage($old,[System.Drawing.Rectangle]::new(0,0,512,1844),[System.Drawing.Rectangle]::new(4608,0,512,1844),[System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($saved,[System.Drawing.Rectangle]::new(512,0,512,1844),[System.Drawing.Rectangle]::new(0,0,512,1844),[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$close.Save((Join-Path $outDir 'liitos-lahikuva.png'),[System.Drawing.Imaging.ImageFormat]::Png);$close.Dispose()
$edge=$saved.Clone([System.Drawing.Rectangle]::new(4864,0,256,1844),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$edge.Save((Join-Path $outDir 'oikean-reunan-jatkoviite.png'),[System.Drawing.Imaging.ImageFormat]::Png);$edge.Dispose()
$saved.Dispose();$newSmall.Dispose();$oldSmall.Dispose();$final.Dispose();$old.Dispose()
if((Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash -ne $oldHash){throw 'Previous interior changed.'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vegas-baari-lava-generointiohje.txt') -Destination (Join-Path $outDir 'generointiohje.txt')
@"
Vegasin baarin sisätilat: tiskin jatko ja esiintymislava

Pelikuva: vegas-baari-sisatilat-lava-5120x1844.png, täysin peittävä PNG.
Sijoita tämän vasen reuna kuvan vegas-baari-sisatilat-vasen-5120x1844.png oikeaan reunaan. Sama PPU, skaala, pivot ja korkeus. Päätyovet ja jukeboxi pysyvät aiemmassa vasemmassa palassa.
Tiski jatkuu vasemmalta uuden palan puoliväliin; sen jälkeen alkaa esiintymislava. Lavalla on rummut, vahvistimet, kitara ja mikrofoniteline. Kuva ei sisällä tekstiä tai hahmoja.
Alhaalla on noin 40 prosenttia vapaata kävelylattiaa. Oikeaa päätyä ei suljeta; lava ja huone voivat jatkua seuraavaan palaan.
Liitoksen vaakalinjat on kohdistettu aiempaan kuvaan: neon, hyllyjen ylälista, tiskin kansi ja jalkatuki. Alalattian pinta jatkuu aiemmasta palasta peilattuna. Liittyvän reunan RGBA-arvot vastaavat täysin aiemman kuvan oikeaa reunaa; lisäksi tarkistetaan kuvapari ja lähikuva visuaalisesti.

Tekotapa: sisäänrakennettu image_gen, referenssiin perustuva oikean jatkon generointi. Natiivikuva 2167 x 726 sisälsi vasemmalla apuliuskan; tämä rajattu pois ja uusi osa skaalattu 5120 x 1844:ään. Vaakalinjojen korkeuskohdistus, katkeamattoman lattiapinnan sovitus ja 64 px sauman sovitus tehty viennissä. 5120 x 1844 on vientikoko, ei generoinnin natiiviresoluutio.
Oikean reunan viitekuva on 256 px leveä rajaus tästä lopullisesta pelipalasta tulevaa jatkoa varten. Vanhaa sisäkuvaa ja Unityn scenejä ei muutettu.
"@ | Set-Content -LiteralPath (Join-Path $outDir 'LUE-MINUT.txt') -Encoding UTF8
@('Native=2167x726 including left guide','Final=5120x1844',$report,"Previous image SHA256 unchanged: $oldHash") | Set-Content -LiteralPath (Join-Path $outDir 'tarkistus.txt') -Encoding UTF8
Write-Output $finalPath
Write-Output $report
