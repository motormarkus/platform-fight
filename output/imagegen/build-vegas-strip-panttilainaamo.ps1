$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$outDir=Join-Path $PSScriptRoot 'vegas-strip-panttilainaamo'
[void][System.IO.Directory]::CreateDirectory($outDir)
$oldPath=Join-Path $PSScriptRoot 'vegas-strip-baari-vasen\vegas-strip-baari-vasen-5120x1844.png'
$oldHash=(Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash
$old=[System.Drawing.Bitmap]::new($oldPath)
$raw=[System.Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-45410057-a38a-4ff2-835c-3b2173ad426a.png')
$native='{0}x{1}' -f $raw.Width,$raw.Height
$null=Clean-Alpha $raw
$scaled=Resize-Canvas $raw 5120 1844 ($raw.Width*1280/1376.0)
$raw.Dispose()
$final=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($final);$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[System.Drawing.Imaging.ImageAttributes]::new();$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
# Align the start of the open pavement with the previous foreground's central street section.
$g.DrawImage($scaled,[System.Drawing.Rectangle]::new(0,0,5120,1210),[single]0,[single]0,[single]5120,[single]1221,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$g.DrawImage($scaled,[System.Drawing.Rectangle]::new(0,1210,5120,634),[single]0,[single]1221,[single]5120,[single]623,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$attrs.Dispose();$g.Dispose();$scaled.Dispose()
$null=Clean-Alpha $final
$data=Pixel-Data $final;$oldData=Pixel-Data $old
Add-Type -TypeDefinition @'
using System;
public static class VegasPawnJoin {
 static double Smooth(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
 public static void Ground(byte[] d,byte[] old,int w,int h){
  // The facade is cropped by the left canvas edge. Remove generated edge-alpha falloff there.
  for(int y=0;y<1240;y++)for(int x=0;x<8;x++)d[(y*w+x)*4+3]=255;
  for(int y=1240;y<h;y++)for(int x=0;x<w;x++){
   int dist=w-1-x,i=(y*w+x)*4,j=(y*w+dist)*4;
   double blend=Smooth((y-1240)/32.0);
   // Exact continuity in the adjacent strip; retain the painted storefront foot before fading to the established road.
   blend=Math.Max(blend,1-Smooth((dist-24)/72.0));
   for(int c=0;c<4;c++)d[i+c]=(byte)Math.Round(d[i+c]*(1-blend)+old[j+c]*blend);
  }
 }
 public static string Check(byte[] d,byte[] old,int w,int h){
  int edge=0,hidden=0,floor=0,gap=0,tall=0;int minGround=h,maxGround=0;
  for(int y=1240;y<h;y++)for(int c=0;c<4;c++)if(d[(y*w+w-1)*4+c]!=old[y*w*4+c])edge++;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int i=(y*w+x)*4;
   if(d[i+3]==0&&(d[i]!=0||d[i+1]!=0||d[i+2]!=0))hidden++;
   if(y>=1240&&d[i+3]!=255)floor++;
   if(x>=2800&&x<w&&y<1190&&d[i+3]!=0)gap++;
   if(x<2400&&y<100&&d[i+3]!=255)tall++;
  }
  for(int x=2800;x<w;x++){int first=-1;for(int y=0;y<h;y++)if(d[(y*w+x)*4+3]>0){first=y;break;}minGround=Math.Min(minGround,first);maxGround=Math.Max(maxGround,first);}
  if(edge+hidden+floor+gap+tall>0)throw new Exception("Validation failed: "+edge+","+hidden+","+floor+","+gap+","+tall);
  return "Ground join from y1240 to bottom: RGBA differences=0. All lower ground opaque. Open street gap truly transparent above the pavement; upper shop facade cropped by top edge. Hidden RGB errors=0. Ground alpha silhouette in open gap y="+minGround+".."+maxGround+". Above the ground, the prior bar ends at its existing left corner; this is an intentional air-to-building boundary.";
 }
}
'@
[VegasPawnJoin]::Ground($data,$oldData,5120,1844)
$null=[CasinoLeftPixels]::Normalize($data)
Set-PixelData $final $data
$finalPath=Join-Path $outDir 'vegas-strip-panttilainaamo-5120x1844.png'
$final.Save($finalPath,[System.Drawing.Imaging.ImageFormat]::Png)
$saved=[System.Drawing.Bitmap]::new($finalPath)
$report=[VegasPawnJoin]::Check((Pixel-Data $saved),$oldData,5120,1844)
$newSmall=Resize-Canvas $saved 1280 461;$oldSmall=Resize-Canvas $old 1280 461
$newSmall.Save((Join-Path $outDir 'panttilainaamo-etuala-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$joined=[System.Drawing.Bitmap]::new(2560,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($joined);$g.Clear([System.Drawing.Color]::FromArgb(45,45,45));$g.DrawImageUnscaled($newSmall,0,0);$g.DrawImageUnscaled($oldSmall,1280,0);$g.Dispose()
$joined.Save((Join-Path $outDir 'liitos-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png);$joined.Dispose()
$close=[System.Drawing.Bitmap]::new(1024,804,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($close);$g.Clear([System.Drawing.Color]::FromArgb(45,45,45))
$g.DrawImage($saved,[System.Drawing.Rectangle]::new(0,0,512,804),[System.Drawing.Rectangle]::new(4608,1040,512,804),[System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($old,[System.Drawing.Rectangle]::new(512,0,512,804),[System.Drawing.Rectangle]::new(0,1040,512,804),[System.Drawing.GraphicsUnit]::Pixel);$g.Dispose()
$close.Save((Join-Path $outDir 'liitos-lahikuva.png'),[System.Drawing.Imaging.ImageFormat]::Png);$close.Dispose()
$scene=[System.Drawing.Bitmap]::new(3840,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($scene)
foreach($name in @('01-yotaivas-5120x1844.png','02-hotellit-ja-rakennukset-5120x1844.png','03-kasvillisuus-5120x1844.png')){
 $layer=[System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot ('las-vegas-parallaksi-v2\'+$name)));$small=Resize-Canvas $layer 1280 461
 $g.DrawImageUnscaled($small,0,0);$g.DrawImageUnscaled($small,2560,0);$small.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX);$g.DrawImageUnscaled($small,1280,0);$small.Dispose();$layer.Dispose()
}
$casino=[System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-strip-kasino-ulkoa-v2\vegas-strip-kasino-ulkoa-v2-5120x1844.png'))
$casinoSmall=Resize-Canvas $casino 1280 461
$g.DrawImageUnscaled($newSmall,0,0);$g.DrawImageUnscaled($oldSmall,1280,0);$g.DrawImageUnscaled($casinoSmall,2560,0);$g.Dispose();$casinoSmall.Dispose();$casino.Dispose()
$scene.Save((Join-Path $outDir 'strip-kolme-palaa-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$pair=$scene.Clone([System.Drawing.Rectangle]::new(0,0,2560,461),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$pair.Save((Join-Path $outDir 'strip-parallakseilla-liitos-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png);$pair.Dispose()
$single=$scene.Clone([System.Drawing.Rectangle]::new(0,0,1280,461),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$single.Save((Join-Path $outDir 'panttilainaamo-parallakseilla-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$single.Dispose();$scene.Dispose();$newSmall.Dispose();$oldSmall.Dispose();$saved.Dispose();$final.Dispose();$old.Dispose()
if((Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash -ne $oldHash){throw 'Adjacent bar image modified.'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vegas-strip-panttilainaamo-generointiohje.txt') -Destination (Join-Path $outDir 'generointiohje.txt')
@"
Vegasin Strip: panttilainaamo baarin vasemmalle puolelle

Pelikuva: vegas-strip-panttilainaamo-5120x1844.png. Koko 5120 x 1844, RGBA PNG.
Sijoita kuvan oikea reuna suoraan baaripalan vegas-strip-baari-vasen-5120x1844.png vasempaan reunaan samoilla PPU-, skaala-, pivot- ja korkeusasetuksilla.
Järjestys vasemmalta: panttilainaamo, pitkä avoin katuosuus, aiempi baari ja kasino. Panttilainaamon korkea julkisivu jatkuu kuvan yläreunan ja vasemman reunan ulkopuolelle.
Kuvan oikealla puoliskolla on vain jalkakäytävä, reunakynnys ja asfaltti. Yläosa on läpinäkyvä olemassa olevalle Vegas-parallaksille. Aiemman baarin pääty pysyy alkuperäisessä baarikuvassa.
Kadun kokonaiskävelyalue noin 35%, kapea jalkakäytävä ja paljon asfalttia. Reunakiven ja asfaltin korkeudet säilyvät aiemmasta palasta. Katupinnan liittyvä reuna on tarkistettu RGBA-arvoista ja kuvaparin esikatselusta.
Parallaksiesikatseluissa käytetään olemassa olevia Vegas-tasoja vuorotellen peilattuina: panttilainaamopala A, baaripala peili(A), kasinopala A. Niitä ei ole yhdistetty pelikäyttöiseen etualan PNG:hen.
Vanhoja kuvia ja Unityn sceneä tai asetuksia ei muutettu.

Tekotapa: sisäänrakennettu image_gen, vasemmalle jatkaminen. Natiivikuva $native sisälsi oikealla apuliuskan. Uusi vasen osuus rajattu ja skaalattu vientikokoon 5120 x 1844, yläreunan alfa siivottu. Katupinta jatkettu aiemman palan peilatusta pinnasta, jotta korkeudet, pintakuvio ja liitos säilyvät; liittymä uuden julkisivun juureen on häivytetty. Vientikoko ei ole generoinnin natiiviresoluutio.
"@ | Set-Content -LiteralPath (Join-Path $outDir 'LUE-MINUT.txt') -Encoding UTF8
@("Native=$native including guide strip",'Final=5120x1844 RGBA PNG',$report,"Adjacent bar SHA256 unchanged: $oldHash") | Set-Content -LiteralPath (Join-Path $outDir 'tarkistus.txt') -Encoding UTF8
Write-Output $finalPath
Write-Output $report
