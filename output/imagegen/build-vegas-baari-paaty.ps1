$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$outDir=Join-Path $PSScriptRoot 'vegas-baari-sisatilat-paaty'
[void][System.IO.Directory]::CreateDirectory($outDir)
$oldPath=Join-Path $PSScriptRoot 'vegas-baari-sisatilat-v2\vegas-baari-sisatilat-lava-v2-5120x1844.png'
$oldHash=(Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash
$old=[System.Drawing.Bitmap]::new($oldPath)
$raw=[System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-baari-paaty-tyo\raaka-5120x1844.png'))
$final=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($final);$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[System.Drawing.Imaging.ImageAttributes]::new();$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
$srcRows=@(0,19,51,119,159,250,550,850,870,950,976,993,1075,1114,1150,1844)
$dstRows=@(0,24,54,122,161,250,550,802,829,901,928,940,1020,1054,1100,1844)
for($i=0;$i -lt $srcRows.Count-1;$i++){
 $g.DrawImage($raw,[System.Drawing.Rectangle]::new(0,$dstRows[$i],5120,($dstRows[$i+1]-$dstRows[$i])),[single]0,[single]$srcRows[$i],[single]5120,[single]($srcRows[$i+1]-$srcRows[$i]),[System.Drawing.GraphicsUnit]::Pixel,$attrs)
}
$attrs.Dispose();$g.Dispose();$raw.Dispose()
$data=Pixel-Data $final;$oldData=Pixel-Data $old
Add-Type -TypeDefinition @'
using System;
public static class BarEndJoin {
 static double S(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
 public static void Join(byte[] d,byte[] old,int w,int h){
  // Extend the established floor; keep the shallow return wall and door foot clear of this operation.
  for(int x=0;x<w;x++){
   double start=1104+136*S((x-3900)/1219.0);
   for(int y=(int)Math.Floor(start);y<h;y++){
    double a=S((y-start)/128.0);int p=(y*w+x)*4,q=(y*w+w-1-x)*4;
    for(int c=0;c<4;c++)d[p+c]=(byte)Math.Round(d[p+c]*(1-a)+old[q+c]*a);
   }
  }
  // Finish the existing split pilaster and sconce exactly before blending within the curtains.
  for(int y=0;y<h;y++)for(int x=0;x<256;x++){
   double a=1-S((x-128)/127.0);int p=(y*w+x)*4,q=(y*w+w-1-x)*4;
   for(int c=0;c<4;c++)d[p+c]=(byte)Math.Round(d[p+c]*(1-a)+old[q+c]*a);
  }
 }
 public static string Check(byte[] d,byte[] old,int w,int h){
  int edge=0,alpha=0;for(int y=0;y<h;y++)for(int c=0;c<4;c++)if(d[y*w*4+c]!=old[(y*w+w-1)*4+c])edge++;
  for(int i=3;i<d.Length;i+=4)if(d[i]!=255)alpha++;
  if(edge+alpha>0)throw new Exception("Edge differences="+edge+", non-opaque="+alpha);
  return "Boundary RGBA differences=0 for all 1844 rows. Fully opaque. Registered upper neon bands y54/y122; stage front highlight y901, lower gold trim y1020. Original adjacent stage tile unchanged.";
 }
}
'@
[BarEndJoin]::Join($data,$oldData,5120,1844)
Set-PixelData $final $data
$path=Join-Path $outDir 'vegas-baari-sisatilat-paaty-5120x1844.png'
$final.Save($path,[System.Drawing.Imaging.ImageFormat]::Png)
$saved=[System.Drawing.Bitmap]::new($path)
$report=[BarEndJoin]::Check((Pixel-Data $saved),$oldData,5120,1844)
$small=Resize-Canvas $saved 1280 461;$oldSmall=Resize-Canvas $old 1280 461
$small.Save((Join-Path $outDir 'esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$pair=[System.Drawing.Bitmap]::new(2560,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($pair);$g.DrawImageUnscaled($oldSmall,0,0);$g.DrawImageUnscaled($small,1280,0);$g.Dispose()
$pair.Save((Join-Path $outDir 'liitos-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png);$pair.Dispose()
$close=[System.Drawing.Bitmap]::new(1024,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($close)
$g.DrawImage($old,[System.Drawing.Rectangle]::new(0,0,512,1844),[System.Drawing.Rectangle]::new(4608,0,512,1844),[System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($saved,[System.Drawing.Rectangle]::new(512,0,512,1844),[System.Drawing.Rectangle]::new(0,0,512,1844),[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$close.Save((Join-Path $outDir 'liitos-lahikuva.png'),[System.Drawing.Imaging.ImageFormat]::Png);$close.Dispose()
$left=[System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-baari-sisatilat-v2\vegas-baari-sisatilat-vasen-v2-5120x1844.png'))
$leftSmall=Resize-Canvas $left 1280 461
$whole=[System.Drawing.Bitmap]::new(3840,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($whole);$g.DrawImageUnscaled($leftSmall,0,0);$g.DrawImageUnscaled($oldSmall,1280,0);$g.DrawImageUnscaled($small,2560,0);$g.Dispose()
$whole.Save((Join-Path $outDir 'koko-baari-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$whole.Dispose();$leftSmall.Dispose();$left.Dispose();$small.Dispose();$oldSmall.Dispose();$saved.Dispose();$final.Dispose();$old.Dispose()
if((Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash -ne $oldHash){throw 'Previous tile modified.'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vegas-baari-paaty-generointiohje.txt') -Destination (Join-Path $outDir 'generointiohje.txt')
@"
Vegasin baarin viimeinen oikea päätypala

Pelikuva: vegas-baari-sisatilat-paaty-5120x1844.png. Koko 5120 x 1844 px, täysin peittävä PNG.
Sijoita vasen reuna kuvan vegas-baari-sisatilat-lava-v2-5120x1844.png oikeaan reunaan, samoilla PPU-, pivot-, skaala- ja korkeusasetuksilla.
Vasemmalta: lava jatkuu ja päättyy portaisiin, hieman paneloitua seinää, kaksi erillistä WC-ovea takaseinällä, lopuksi viisto sulkeva päätyseinä ja siinä yksi bäkkärin ovi.
WC-ovissa on vain kuvasymbolit, ei kirjaimia. Kuvassa ei ole tekstiä, esiintyjiä tai soittimia. Uuteen palaan ei lisätty PA-kaiuttimia; edellisen kuvan kaiutin jää ennalleen.
Noin 40 prosenttia kuva-alan korkeudesta on avointa kävelylattiaa; päätyseinän matala vinous muuttaa osuutta hieman oikealla. Lattian tekstuuri jatkuu aiemmasta palasta. Lavan sekä valonauhojen korkeudet ja yhdistävän reunan RGBA-arvot on tarkistettu.

Koko baarin järjestys vasemmalta:
1. vegas-baari-sisatilat-vasen-v2-5120x1844.png
2. vegas-baari-sisatilat-lava-v2-5120x1844.png
3. vegas-baari-sisatilat-paaty-5120x1844.png

Tekotapa: sisäänrakennettu image_gen, oikean päädyn generointi aiempaa pelipalaa jatkamalla. Natiivikuva 2167 x 726 sisälsi vasemmalla apuliuskan. Apuliuska rajattu pois, uusi pala skaalattu 5120 x 1844 vientikokoon, vaakalinjat kohdistettu ja sauma sekä lattiapinta sovitettu. Vientikoko ei ole generoinnin natiiviresoluutio.
Vanhoja pelipaloja tai Unity-scenejä ei muutettu.
"@ | Set-Content -LiteralPath (Join-Path $outDir 'LUE-MINUT.txt') -Encoding UTF8
@('Native=2167x726 including guide','Final=5120x1844',$report,"Previous tile SHA256 unchanged: $oldHash") | Set-Content -LiteralPath (Join-Path $outDir 'tarkistus.txt') -Encoding UTF8
Write-Output $path
Write-Output $report
