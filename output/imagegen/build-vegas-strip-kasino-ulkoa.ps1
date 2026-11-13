$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$stripDir=Join-Path $PSScriptRoot 'vegas-strip-kasino-ulkoa'
[void][System.IO.Directory]::CreateDirectory($stripDir)
$raw=[System.Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-1d26b69a-ff22-4567-96e3-7dcb12e45b37.png')
$native='{0}x{1}' -f $raw.Width,$raw.Height
$nativeAlpha=Clean-Alpha $raw
$final=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($final)
$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[System.Drawing.Imaging.ImageAttributes]::new();$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
# Native ground starts at y=517. Register this line to y=1199, leaving 35% playable foreground.
$g.DrawImage($raw,[System.Drawing.Rectangle]::new(0,0,5120,1199),[single]0,[single]0,[single]$raw.Width,[single]517,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$g.DrawImage($raw,[System.Drawing.Rectangle]::new(0,1199,5120,645),[single]0,[single]517,[single]$raw.Width,[single]($raw.Height-517),[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$attrs.Dispose();$g.Dispose();$raw.Dispose()
$exportAlpha=Clean-Alpha $final
$finalPath=Join-Path $stripDir 'vegas-strip-kasino-ulkoa-5120x1844.png'
$final.Save($finalPath,[System.Drawing.Imaging.ImageFormat]::Png)
Add-Type -TypeDefinition @'
using System;
public static class VegasExteriorCheck {
 public static string Check(byte[] d,int w,int h){
  int hidden=0,ground=0,air=0,opaque=0,transparent=0,minFirst=h,maxFirst=0;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int i=(y*w+x)*4;byte a=d[i+3];
   if(a==0){transparent++;if(d[i]!=0||d[i+1]!=0||d[i+2]!=0)hidden++;}
   if(a==255)opaque++;
   if(y>=1240&&a!=255)ground++;
   if(x>=3500&&y<1190&&a!=0)air++;
  }
  for(int x=3500;x<w;x++){
   int first=-1;for(int y=0;y<h;y++)if(d[(y*w+x)*4+3]>0){first=y;break;}
   minFirst=Math.Min(first,minFirst);maxFirst=Math.Max(first,maxFirst);
  }
  if(hidden+ground+air>0)throw new Exception("Alpha validation: "+hidden+","+ground+","+air);
  if(minFirst<1195||maxFirst>1202)throw new Exception("Ground registration: "+minFirst+".."+maxFirst);
  return "Transparent="+transparent+"; opaque="+opaque+"; hidden RGB errors=0; open right-side air is transparent; ground fully opaque below y1240. Ground upper alpha edge y="+minFirst+".."+maxFirst+" (~65%); playable band ~35%.";
 }
}
'@
$saved=[System.Drawing.Bitmap]::new($finalPath)
$report=[VegasExteriorCheck]::Check((Pixel-Data $saved),5120,1844)
function Strip-Checker([System.Drawing.Bitmap]$im){
 $result=[System.Drawing.Bitmap]::new($im.Width,$im.Height,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[System.Drawing.Graphics]::FromImage($result)
 $brushes=@([System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(45,45,45)),[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(62,62,62)))
 for($y=0;$y -lt $im.Height;$y+=32){for($x=0;$x -lt $im.Width;$x+=32){$idx=([int][Math]::Floor($x/32)+[int][Math]::Floor($y/32))%2;$g.FillRectangle($brushes[$idx],$x,$y,32,32)}}
 $g.DrawImageUnscaled($im,0,0);$g.Dispose();$brushes[0].Dispose();$brushes[1].Dispose();return $result
}
$small=Resize-Canvas $saved 1280 461
$checked=Strip-Checker $small
$checked.Save((Join-Path $stripDir 'etuala-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$checked.Dispose()
$composite=[System.Drawing.Bitmap]::new(1280,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($composite)
foreach($layerName in @('01-yotaivas-5120x1844.png','02-hotellit-ja-rakennukset-5120x1844.png','03-kasvillisuus-5120x1844.png')){
 $layer=[System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot ('las-vegas-parallaksi-v2\'+$layerName)))
 $layerSmall=Resize-Canvas $layer 1280 461
 $g.DrawImageUnscaled($layerSmall,0,0);$layerSmall.Dispose();$layer.Dispose()
}
$g.DrawImageUnscaled($small,0,0);$g.Dispose()
$composite.Save((Join-Path $stripDir 'strip-parallakseilla-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$composite.Dispose();$small.Dispose();$saved.Dispose();$final.Dispose()
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vegas-strip-kasino-ulkoa-generointiohje.txt') -Destination (Join-Path $stripDir 'generointiohje.txt')
@"
Vegasin Strip – kasino ulkopuolelta

Pelikuva: vegas-strip-kasino-ulkoa-5120x1844.png. RGBA PNG, 5120 x 1844.
Kasinon julkisivu, sisäpuolen ovien mallia vastaavat pariovet, valaistu CASINO-katos, jalkakäytävä, matala reunakynnys ja asfaltin etuala. Yläosa rakennuksen oikealla puolella on läpinäkyvä olemassa oleville Vegas-parallakseille.
Kävelyalue alkaa noin 65% kuvan korkeudesta (y1199), joten alimmainen noin 35% muodostuu jalkakäytävästä, reunakynnyksestä ja asfaltista. Ovien oma ohut kynnys on jalkakäytävän tasalla.
Kuva on uusi ulkoalueen etualapala; sillä ei ole kuvallista saumaa sisätilan mattoon. Liitos sisätilaan tehdään pelissä ovensiirtymällä. Julkisivu jatkuu vasemman kuvarajan ulkopuolelle, kävelyala ja reunakiveys jatkuvat kumpaankin sivuun.

Piirtojärjestys: 01-yotaivas, 02-hotellit-ja-rakennukset, 03-kasvillisuus, tämän kuvan etuala. Tiedostossa strip-parallakseilla-esikatselu.png näkyy yksi sommitteluesimerkki nykyisillä Vegas-tasoilla; varsinainen pelikuva sisältää vain etualan.
Tämän toimituksen aikana Unityn sceneä tai tuontiasetuksia ei muutettu.

Tekotapa: sisäänrakennettu image_gen, referensseinä kasinon pääovet ja Vegas-parallaksien koottu esikatselu. Natiivikuva $native. Skaalattu vientikokoon, kävelyalueen korkeus kohdistettu, lähes läpinäkyvät reunavärit siivottu. 5120 x 1844 ei ole generoinnin natiivikoko.
"@ | Set-Content -LiteralPath (Join-Path $stripDir 'LUE-MINUT.txt') -Encoding UTF8
@("Native=$native",'Final=5120x1844 RGBA PNG',"Native alpha cleanup=$nativeAlpha","Export alpha cleanup=$exportAlpha",$report) | Set-Content -LiteralPath (Join-Path $stripDir 'tarkistus.txt') -Encoding UTF8
Write-Output $finalPath
Write-Output $report
