$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$out=Join-Path $root 'las-vegas-parallaksi-v2'
$nativeDir=Join-Path $root 'las-vegas-parallaksi-v2-alkuperaiset'
$null=New-Item -ItemType Directory -Path $out,$nativeDir -Force
$rawRoot='C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472'
$assets=@(
 @{name='01-yotaivas';raw='exec-e68b5c93-e51a-4e75-9298-65bdb98836e5.png';alpha=$false},
 @{name='02-hotellit-ja-rakennukset';raw='exec-40563e5b-a341-4f7c-9424-1a6b3d7440ec.png';alpha=$true},
 @{name='03-kasvillisuus';raw='exec-b4089630-0e40-412d-a876-32b58032b6a5.png';alpha=$true}
)
Add-Type -TypeDefinition @'
using System;
public static class VegasV2ParallaxPixels {
 public static string Normalize(byte[] data){
  int clear=0,solid=0,edge=0;
  for(int i=0;i<data.Length;i+=4){
   if(data[i+3]<=16){data[i]=data[i+1]=data[i+2]=data[i+3]=0;clear++;}
   else if(data[i+3]>=240){data[i+3]=255;solid++;}
   else edge++;
  }
  return "alpha0="+clear+", alpha255="+solid+", antialias="+edge;
 }
 public static string Verify(byte[] data,int w,int h,bool transparent){
  int badSky=0,clear=0,hidden=0;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int i=(y*w+x)*4;byte a=data[i+3];
   if(a==0){clear++;if(data[i]!=0||data[i+1]!=0||data[i+2]!=0)hidden++;}
   if(transparent&&y<h*0.3&&a!=0)badSky++;
   if(!transparent&&a!=255)badSky++;
  }
  if(badSky!=0||hidden!=0||transparent&&clear==0)throw new Exception("Alpha verification failed");
  return "Transparent pixels="+clear+"; top30% or opaque-sky errors="+badSky+"; hidden RGB errors="+hidden;
 }
}
'@
function Pixels([System.Drawing.Bitmap]$im){
 $r=[System.Drawing.Rectangle]::new(0,0,$im.Width,$im.Height)
 $bd=$im.LockBits($r,[System.Drawing.Imaging.ImageLockMode]::ReadOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $d=[byte[]]::new($bd.Stride*$im.Height);[System.Runtime.InteropServices.Marshal]::Copy($bd.Scan0,$d,0,$d.Length);$im.UnlockBits($bd);return ,$d
}
function Normalize([System.Drawing.Bitmap]$im){
 $r=[System.Drawing.Rectangle]::new(0,0,$im.Width,$im.Height)
 $bd=$im.LockBits($r,[System.Drawing.Imaging.ImageLockMode]::ReadWrite,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $d=[byte[]]::new($bd.Stride*$im.Height);[System.Runtime.InteropServices.Marshal]::Copy($bd.Scan0,$d,0,$d.Length)
 $result=[VegasV2ParallaxPixels]::Normalize($d)
 [System.Runtime.InteropServices.Marshal]::Copy($d,0,$bd.Scan0,$d.Length);$im.UnlockBits($bd);return $result
}
function Resize-Image([System.Drawing.Bitmap]$im,[int]$w,[int]$h){
 $dst=[System.Drawing.Bitmap]::new($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[System.Drawing.Graphics]::FromImage($dst);$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
 $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
 $a=[System.Drawing.Imaging.ImageAttributes]::new();$a.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
 $g.DrawImage($im,[System.Drawing.Rectangle]::new(0,0,$w,$h),0,0,$im.Width,$im.Height,[System.Drawing.GraphicsUnit]::Pixel,$a)
 $g.Dispose();$a.Dispose();return $dst
}
$reports=@('Three matching 5120 x 1844 layers. Exports are resampled, not native 5K detail.')
$layers=@();$metadata=@()
foreach($asset in $assets){
 $src=[System.Drawing.Bitmap]::FromFile((Join-Path $rawRoot $asset.raw))
 $nativeReport=Normalize $src
 $src.Save((Join-Path $nativeDir ($asset.name+'.png')),[System.Drawing.Imaging.ImageFormat]::Png)
 $final=Resize-Image $src 5120 1844
 $finalReport=Normalize $final
 $path=Join-Path $out ($asset.name+'-5120x1844.png')
 $final.Save($path,[System.Drawing.Imaging.ImageFormat]::Png)
 $saved=[System.Drawing.Bitmap]::FromFile($path)
 if($saved.Width -ne 5120 -or $saved.Height -ne 1844){throw 'Wrong export dimensions'}
 $verification=[VegasV2ParallaxPixels]::Verify((Pixels $saved),5120,1844,$asset.alpha)
 $flipped=$saved.Clone();$flipped.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX)
 $mismatch=0
 for($y=0;$y -lt 1844;$y++){
  if($saved.GetPixel(5119,$y).ToArgb() -ne $flipped.GetPixel(0,$y).ToArgb()){$mismatch++}
  if($flipped.GetPixel(5119,$y).ToArgb() -ne $saved.GetPixel(0,$y).ToArgb()){$mismatch++}
 }
 if($mismatch -ne 0){throw 'Mirrored join mismatch'}
 $reports+=@("$($asset.name): native $($src.Width)x$($src.Height); export 5120x1844",$finalReport,$verification,"Mirrored A / flip(A) / A joins: $mismatch RGBA mismatches.")
 $metadata+=@{file=[IO.Path]::GetFileName($path);nativeWidth=$src.Width;nativeHeight=$src.Height;width=5120;height=1844;transparent=$asset.alpha}
 $layers+=,(Resize-Image $saved 1280 461)
 $flipped.Dispose();$saved.Dispose();$final.Dispose();$src.Dispose()
}
$composite=[System.Drawing.Bitmap]::new(1280,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$cg=[System.Drawing.Graphics]::FromImage($composite)
foreach($layer in $layers){$cg.DrawImageUnscaled($layer,0,0)}
$cg.Dispose();$composite.Save((Join-Path $out 'koottu-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$mirror=$composite.Clone();$mirror.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX)
$repeat=[System.Drawing.Bitmap]::new(3840,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$rg=[System.Drawing.Graphics]::FromImage($repeat);$rg.DrawImageUnscaled($composite,0,0);$rg.DrawImageUnscaled($mirror,1280,0);$rg.DrawImageUnscaled($composite,2560,0);$rg.Dispose()
$repeat.Save((Join-Path $out 'peilattu-toisto-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$reports | Set-Content -LiteralPath (Join-Path $out 'tarkistus.txt') -Encoding utf8
@{layersBackToFront=$metadata;repeatPattern='A, horizontal mirror of A, A';canvasWidth=5120;canvasHeight=1844} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'tasot.json') -Encoding utf8
@('LAS VEGAS - KOLME PARALLAKSITASOA','','Piirtojarjestys:','01-yotaivas: takimmaisena, peittava taivaskuva.','02-hotellit-ja-rakennukset: taivaan eteen, muut alueet lapinakyvia.','03-kasvillisuus: rakennusten eteen, muut alueet lapinakyvia.','','Kaikki kuvat 5120 x 1844. Kohdista tasojen alareunat.','Toista vuorotellen alkuperaisena ja vaakasuunnassa peilattuna: A - peili(A) - A.','Peilattuja versioita ei tarvitse tallentaa erikseen.','Kuvat on suurennettu generointikoosta. Pelkistetyt rakennusmuodot, pienet hotellien ikkunavalot ja neonkorostukset.','Ei tekstia, logoja tai taustaan piirrettyja ihmishahmoja.') | Set-Content -LiteralPath (Join-Path $out 'LUE-MINUT.txt') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $root 'las-vegas-parallaksi-v2-generointiohjeet.txt') -Destination (Join-Path $out 'generointiohjeet.txt') -Force
$reports
foreach($layer in $layers){$layer.Dispose()};$composite.Dispose();$mirror.Dispose();$repeat.Dispose()

