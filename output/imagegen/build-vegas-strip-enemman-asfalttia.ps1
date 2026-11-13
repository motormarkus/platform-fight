$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$outDir=Join-Path $PSScriptRoot 'vegas-strip-kasino-ulkoa-v2'
[void][System.IO.Directory]::CreateDirectory($outDir)
$originalPath=Join-Path $PSScriptRoot 'vegas-strip-kasino-ulkoa\vegas-strip-kasino-ulkoa-5120x1844.png'
$originalHash=(Get-FileHash -LiteralPath $originalPath -Algorithm SHA256).Hash
$original=[System.Drawing.Bitmap]::new($originalPath)
$raw=[System.Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-a4db50bc-2742-44de-9552-478555100c80.png')
$native='{0}x{1}' -f $raw.Width,$raw.Height
$null=Clean-Alpha $raw
$scaled=Resize-Canvas $raw 5120 1844
$raw.Dispose()
$edit=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($edit)
$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[System.Drawing.Imaging.ImageAttributes]::new();$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
$g.DrawImage($scaled,[System.Drawing.Rectangle]::new(0,0,5120,1236),[single]0,[single]0,[single]5120,[single]1236,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$g.DrawImage($scaled,[System.Drawing.Rectangle]::new(0,1236,5120,174),[single]0,[single]1236,[single]5120,[single]134,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$g.DrawImage($scaled,[System.Drawing.Rectangle]::new(0,1410,5120,434),[single]0,[single]1370,[single]5120,[single]474,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$attrs.Dispose();$g.Dispose();$scaled.Dispose()
$null=Clean-Alpha $edit
$sourceData=Pixel-Data $original
$editData=Pixel-Data $edit
Add-Type -TypeDefinition @'
using System;
public static class VegasAsphaltEdit {
 public static void Preserve(byte[] dst,byte[] src,int w,int h){
  const int keep=1236,blend=32;
  Buffer.BlockCopy(src,0,dst,0,keep*w*4);
  for(int y=keep;y<keep+blend;y++){
   double a=(y-keep)/(double)(blend-1);a=a*a*(3-2*a);
   for(int x=0;x<w;x++)for(int c=0;c<4;c++){
    int i=(y*w+x)*4+c;dst[i]=(byte)Math.Round(src[i]*(1-a)+dst[i]*a);
   }
  }
 }
 public static string Verify(byte[] d,byte[] s,int w,int h){
  int top=0,alpha=0,hidden=0;
  for(int i=0;i<1236*w*4;i++)if(d[i]!=s[i])top++;
  for(int y=1236;y<h;y++)for(int x=0;x<w;x++)if(d[(y*w+x)*4+3]!=255)alpha++;
  for(int i=0;i<d.Length;i+=4)if(d[i+3]==0&&(d[i]!=0||d[i+1]!=0||d[i+2]!=0))hidden++;
  if(top+alpha+hidden>0)throw new Exception("Validation failed: "+top+","+alpha+","+hidden);
  return "Original upper 1236 rows unchanged in every RGBA channel (facade, doors, lights, sign, transparency, ground back edge). Lower edit fully opaque. Hidden RGB errors=0. Asphalt begins about y1410 (bottom 23.5%). Combined playable band remains ~35%.";
 }
}
'@
[VegasAsphaltEdit]::Preserve($editData,$sourceData,5120,1844)
Set-PixelData $edit $editData
$finalPath=Join-Path $outDir 'vegas-strip-kasino-ulkoa-v2-5120x1844.png'
$edit.Save($finalPath,[System.Drawing.Imaging.ImageFormat]::Png)
$saved=[System.Drawing.Bitmap]::new($finalPath)
$report=[VegasAsphaltEdit]::Verify((Pixel-Data $saved),$sourceData,5120,1844)
$small=Resize-Canvas $saved 1280 461
$small.Save((Join-Path $outDir 'etuala-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$composite=[System.Drawing.Bitmap]::new(1280,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($composite)
foreach($name in @('01-yotaivas-5120x1844.png','02-hotellit-ja-rakennukset-5120x1844.png','03-kasvillisuus-5120x1844.png')){
 $layer=[System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot ('las-vegas-parallaksi-v2\'+$name)))
 $layerSmall=Resize-Canvas $layer 1280 461
 $g.DrawImageUnscaled($layerSmall,0,0);$layerSmall.Dispose();$layer.Dispose()
}
$g.DrawImageUnscaled($small,0,0);$g.Dispose()
$composite.Save((Join-Path $outDir 'strip-parallakseilla-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$composite.Dispose();$small.Dispose();$saved.Dispose();$edit.Dispose();$original.Dispose()
if((Get-FileHash -LiteralPath $originalPath -Algorithm SHA256).Hash -ne $originalHash){throw 'Original file modified.'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vegas-strip-enemman-asfalttia-generointiohje.txt') -Destination (Join-Path $outDir 'generointiohje.txt')
@"
Vegasin Strip, kasinon ulkopuoli v2 – enemmän asfalttia

Pelikuva: vegas-strip-kasino-ulkoa-v2-5120x1844.png, RGBA PNG.
Jalkakäytävä on kapeampi ja reunakynnys matalampi. Asfaltti alkaa noin y1410 eli vie noin 23,5% koko kuvan korkeudesta. Koko kävelyalue säilyy noin 35%:ssa.
Alkuperäiset rivit y0–1235 säilyvät täsmälleen samoina kaikissa RGBA-kanavissa. Rakennus, pariovet, kyltti, valaistus, taustan läpinäkyvyys ja kävelyalueen takareuna eivät muuttuneet.
Korvaa ulkoalueen aiempi etualakuva tällä samoilla PPU-, pivot- ja sijaintiasetuksilla. Vanhaa kuvatiedostoa ei ole korvattu.
Parallaksien esikatselu on vain sommitteluesimerkki; varsinaisessa PNG:ssä kaukotausta on läpinäkyvä. Unityn sceneä tai asetuksia ei muutettu.

Tekotapa: sisäänrakennetun image_gen-työkalun kohdennettu editointi, natiivikoko $native. Lopullisessa 5120 x 1844 viennissä alkuperäinen yläosa on palautettu sellaisenaan ja alareunan materiaaleja on kohdistettu. Uusi katupinta on skaalattu generointikoosta.
"@ | Set-Content -LiteralPath (Join-Path $outDir 'LUE-MINUT.txt') -Encoding UTF8
@("Native edit=$native",'Final=5120x1844 RGBA PNG',$report,"Original unchanged SHA256=$originalHash") | Set-Content -LiteralPath (Join-Path $outDir 'tarkistus.txt') -Encoding UTF8
Write-Output $finalPath
Write-Output $report
