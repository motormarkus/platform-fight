$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$out=Join-Path $root 'kasino-sisatilat-parallaksi-3tasoa'
$native=Join-Path $root 'kasino-sisatilat-parallaksi-alkuperaiset'
$null=New-Item -ItemType Directory -Path $out,$native -Force
$rawRoot='C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472'
$assets=@(
 @{name='01-kaukotausta';raw='exec-e578abee-1b9a-4d13-8cff-0cd8a0de37d4.png';alpha=$false;factor=0.2;description='Kauimmainen sali, katto, pienet peliautomaatit ja kaukaiset valaisimet.'},
 @{name='02-keskitaso';raw='exec-00f5fb86-2cb7-4233-844d-1f756d9a06fa.png';alpha=$true;factor=0.5;description='Pelipoydat, kaksi pylvasta, palmut ja suuri kattovalaisin. Lapinakyva ymparisto.'},
 @{name='03-etuala';raw='exec-bd2e7776-b48e-4a14-a025-c19b02a7e950.png';alpha=$true;factor=1.0;description='Kavelylattia, lahimmat peliautomaatit, pylvaat, hissi ja oikea paatyseina.'}
)
Add-Type -TypeDefinition @'
using System;
public static class CasinoInteriorLayerPixels {
 public static string Normalize(byte[] d){
  int clear=0,solid=0,edge=0;
  for(int i=0;i<d.Length;i+=4){
   if(d[i+3]<=16){d[i]=d[i+1]=d[i+2]=d[i+3]=0;clear++;}
   else if(d[i+3]>=240){d[i+3]=255;solid++;}
   else edge++;
  }
  return "alpha0="+clear+"; alpha255="+solid+"; antialias="+edge;
 }
 public static string Verify(byte[] d,int w,int h,int layer){
  int clear=0,solid=0,hidden=0,regionError=0;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int i=(y*w+x)*4;byte a=d[i+3];
   if(a==0){clear++;if(d[i]!=0||d[i+1]!=0||d[i+2]!=0)hidden++;}
   if(a==255)solid++;
   if(layer==0&&a!=255)regionError++;
   if(layer==1&&y>h*0.63&&a!=0)regionError++;
   if(layer==2&&y>h*0.69&&a!=255)regionError++;
  }
  if(hidden>0||regionError>0||(layer>0&&clear==0))throw new Exception("Layer alpha check failed: hidden="+hidden+", region="+regionError);
  return "Transparent="+clear+"; opaque="+solid+"; hidden RGB errors="+hidden+"; checked-region errors="+regionError;
 }
}
'@
function Pixel-Bytes([System.Drawing.Bitmap]$im){
 $bd=$im.LockBits([System.Drawing.Rectangle]::new(0,0,$im.Width,$im.Height),[System.Drawing.Imaging.ImageLockMode]::ReadOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $d=[byte[]]::new($bd.Stride*$im.Height)
 [System.Runtime.InteropServices.Marshal]::Copy($bd.Scan0,$d,0,$d.Length)
 $im.UnlockBits($bd)
 return ,$d
}
function Normalize-Alpha([System.Drawing.Bitmap]$im){
 $bd=$im.LockBits([System.Drawing.Rectangle]::new(0,0,$im.Width,$im.Height),[System.Drawing.Imaging.ImageLockMode]::ReadWrite,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $d=[byte[]]::new($bd.Stride*$im.Height)
 [System.Runtime.InteropServices.Marshal]::Copy($bd.Scan0,$d,0,$d.Length)
 $report=[CasinoInteriorLayerPixels]::Normalize($d)
 [System.Runtime.InteropServices.Marshal]::Copy($d,0,$bd.Scan0,$d.Length)
 $im.UnlockBits($bd)
 return $report
}
function Resize-Image([System.Drawing.Bitmap]$im,[int]$w,[int]$h){
 $dst=[System.Drawing.Bitmap]::new($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[System.Drawing.Graphics]::FromImage($dst)
 $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
 $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
 $attrs=[System.Drawing.Imaging.ImageAttributes]::new()
 $attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
 $g.DrawImage($im,[System.Drawing.Rectangle]::new(0,0,$w,$h),0,0,$im.Width,$im.Height,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
 $attrs.Dispose();$g.Dispose()
 return $dst
}
function Checker([System.Drawing.Graphics]$g,[int]$w,[int]$h){
 $dark=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(60,65,75))
 $light=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(88,93,103))
 for($y=0;$y -lt $h;$y+=24){for($x=0;$x -lt $w;$x+=24){
  $brush=if((([int]($x/24)+[int]($y/24))%2)-eq 0){$dark}else{$light}
  $g.FillRectangle($brush,$x,$y,24,24)
 }}
 $dark.Dispose();$light.Dispose()
}
$reports=@('Three full-canvas aligned 5120x1844 PNG layers. Built-in image_gen edits followed by alpha cleanup and resizing.','Original visible scene is the reference; hidden regions are reconstructed. The layers are not a pixel-identical reconstruction of the original image.','Final exports are resampled, not native 5K detail.')
$metadata=@();$previews=@();$layerIndex=0
foreach($asset in $assets){
 $src=[System.Drawing.Bitmap]::FromFile((Join-Path $rawRoot $asset.raw))
 $nativeStats=Normalize-Alpha $src
 $src.Save((Join-Path $native ($asset.name+'.png')),[System.Drawing.Imaging.ImageFormat]::Png)
 $final=Resize-Image $src 5120 1844
 $stats=Normalize-Alpha $final
 $file=$asset.name+'-5120x1844.png'
 $final.Save((Join-Path $out $file),[System.Drawing.Imaging.ImageFormat]::Png)
 $saved=[System.Drawing.Bitmap]::FromFile((Join-Path $out $file))
 if($saved.Width -ne 5120 -or $saved.Height -ne 1844){throw 'Wrong saved dimensions'}
 $verification=[CasinoInteriorLayerPixels]::Verify((Pixel-Bytes $saved),5120,1844,$layerIndex)
 $reports+="$($asset.name): native=$($src.Width)x$($src.Height); $verification"
 $metadata+=@{file=$file;width=5120;height=1844;transparent=$asset.alpha;sortingOrder=$layerIndex;sharedPivot=@(0.5,0.5);suggestedRelativeMotion=$asset.factor;description=$asset.description;nativeSize=@($src.Width,$src.Height)}
 $small=Resize-Image $final 1280 461
 $null=Normalize-Alpha $small
 $previews+=,$small
 $sheet=[System.Drawing.Bitmap]::new(1280,461)
 $g=[System.Drawing.Graphics]::FromImage($sheet)
 Checker $g 1280 461
 $g.DrawImageUnscaled($small,0,0)
 $sheet.Save((Join-Path $out ($asset.name+'-esikatselu.png')),[System.Drawing.Imaging.ImageFormat]::Png)
 $g.Dispose();$sheet.Dispose();$saved.Dispose();$final.Dispose();$src.Dispose()
 $layerIndex++
}
foreach($config in @(@{file='koottu-esikatselu.png';far=0;mid=0},@{file='liike-esikatselu.png';far=-8;mid=-24})){
 $composite=[System.Drawing.Bitmap]::new(1280,461)
 $g=[System.Drawing.Graphics]::FromImage($composite)
 $g.DrawImageUnscaled($previews[0],$config.far,0)
 if($config.far -ne 0){$g.DrawImageUnscaled($previews[0],$config.far+1280,0)}
 $g.DrawImageUnscaled($previews[1],$config.mid,0)
 $g.DrawImageUnscaled($previews[2],0,0)
 $composite.Save((Join-Path $out $config.file),[System.Drawing.Imaging.ImageFormat]::Png)
 $g.Dispose();$composite.Dispose()
}
foreach($im in $previews){$im.Dispose()}
$metadata|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $out 'tasot.json')
$reports|Set-Content -LiteralPath (Join-Path $out 'tarkistus.txt')
$readme=@'
KASINON SISATILAT - KOLME PARALLAKSIKERROSTA

01-kaukotausta: kauimmainen sali ja katto. Taysin peittava.
02-keskitaso: pelipoydat, pylvaat, palmut ja suuri valaisin. PNG:n tyhja alue on aidosti lapinakyva.
03-etuala: kavelylattia (noin35%), lahimmat automaatit, hissi ja paatyseina. Takana oleva sali poistettu lapinakyvaksi.

Kaikki ovat5120x1844. Aseta samaan lahtosijaintiin, sama pixels-per-unit ja sama pivot. Piirtojarjestys01,02,03. Ala leikkaa lapinakyvia marginaaleja pois.
Liikenopeuksien aloitusarvot suhteessa etualaan:0.2,0.5,1.0. Saada pelin kameralla.
Pelikenttaa ei ole muutettu eika Unityyn tehty asetuksia.

Piilossa olleet tausta-alueet on taydennetty image_genilla. Kerrokset eivat ole pikselilleen sama kuva kuin lahde. Tiedostot on suurennettu generoinnin alkuperaiskoosta5120x1844-kokoon.
koottu-esikatselu.png nayttaa kerrokset yhdessa. liike-esikatselu.png nayttaa pienen sivuttaisen parallax-siirtyman.
Tausta ei ole saumattomaksi loopiksi vahvistettu. Liike-esikatselun pienen reunataynnon tarkoitus on vain tarkistaa kerrosten aukot; rajaa tai jatka taustaa pelin kameran liikealueelle.

Generointitapa: built-in image_gen. Koko ohjesarja tiedostossa generointiohjeet.txt.
'@
Set-Content -LiteralPath (Join-Path $out 'LUE-MINUT.txt') -Value $readme
$promptFiles=@('kasino-sisatilat-parallaksi-generointiohjeet.txt','kasino-sisatilat-parallaksi-3tasoa-generointiohjeet.txt','kasino-sisatilat-keskitaso-kohdistusohje.txt')
$prompts=@()
foreach($file in $promptFiles){$prompts+=Get-Content -LiteralPath (Join-Path $root $file) -Raw}
Set-Content -LiteralPath (Join-Path $out 'generointiohjeet.txt') -Value $prompts
$reports
