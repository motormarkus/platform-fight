$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
Add-Type -TypeDefinition @'
using System;
public static class CasinoContactPatch {
 static double Smooth(double v){v=Math.Max(0,Math.Min(1,v));return v*v*(3-2*v);}
 public static string Apply(byte[] dst,byte[] bg,byte[] edited,int w,int h){
  int changed=0;
  int[,] ranges={{190,1605},{2110,3955},{4075,5065}};
  for(int y=1250;y<1440;y++)for(int x=0;x<w;x++){
   int i=4*(y*w+x); bool floor=true;
   for(int c=0;c<4;c++)if(dst[i+c]!=bg[i+c])floor=false;
   if(!floor)continue;
   double mask=0;
   for(int k=0;k<3;k++)mask=Math.Max(mask,Math.Min(Smooth((x-ranges[k,0])/55.0),Smooth((ranges[k,1]-x)/55.0)));
   mask*=Smooth((y-1250)/35.0)*Smooth((1440-y)/70.0);
   if(mask<=0)continue;
   // Use only the red-carpet shadow field from the image edit; preserve original objects.
   if(edited[i+2]<edited[i+1]*1.35 || edited[i+2]<edited[i]*1.25)continue;
   double norm=0;
   for(int sy=1460;sy<1490;sy++){
    int si=4*(sy*w+x);norm+=Math.Max(1,(int)edited[si+2])/(double)Math.Max(1,(int)bg[si+2]);
   }
   norm/=30;
   double ratio=edited[i+2]/(double)Math.Max(1,(int)bg[i+2])/Math.Max(0.1,norm);
   ratio=Math.Max(0.48,Math.Min(1,ratio));
   double f=1-(1-ratio)*mask;
   if(f<0.995){for(int c=0;c<3;c++)dst[i+c]=(byte)Math.Round(dst[i+c]*f);changed++;}
  }
  int clear=0,holes=0,hidden=0;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int i=4*(y*w+x);
   if(dst[i+3]==0){clear++;if(dst[i]!=0||dst[i+1]!=0||dst[i+2]!=0)hidden++;}
   if(y>=1199&&dst[i+3]!=255)holes++;
  }
  if(holes>0||hidden>0)throw new Exception("Alpha verification failed");
  return "Shadow-adjusted carpet pixels="+changed+"; transparent pixels="+clear+"; floor alpha holes="+holes+"; hidden RGB="+hidden;
 }
}
'@
$folder=Join-Path $PSScriptRoot 'kasino-lahiparallaksi-poydat-v2'
$base=[System.Drawing.Bitmap]::FromFile((Join-Path $folder 'kalusteet-maton-paalla-5120x1844.png'))
$bg=[System.Drawing.Bitmap]::FromFile((Join-Path $PSScriptRoot 'kasino-kaukotausta-pramea-v2\kasino-kaukotausta-pramea-v2-5120x1844.png'))
$raw=[System.Drawing.Bitmap]::FromFile('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-32c03aeb-e87b-42d2-acda-989d2a5e204c.png')
$ed=Resize-Canvas $raw 5120 1844
$data=Pixel-Data $base
$bd=Pixel-Data $bg
$gd=Pixel-Data $ed
$report=[CasinoContactPatch]::Apply($data,$bd,$gd,5120,1844)
Set-PixelData $base $data
$base.Save((Join-Path $folder 'kasino-lahiparallaksi-poydat-v2-5120x1844.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview=Resize-Canvas $base 1280 461
$preview.Save((Join-Path $folder 'esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
$combo=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($combo)
$g.DrawImageUnscaled($bg,0,0);$g.DrawImageUnscaled($base,0,0);$g.Dispose()
$preview=Resize-Canvas $combo 1280 461
$preview.Save((Join-Path $folder 'taustan-kanssa-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
$crop=$base.Clone([System.Drawing.Rectangle]::new(200,1100,1450,380),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$crop.Save((Join-Path $folder 'lattiakosketus-lahikuva.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$crop.Dispose()
@'
Kasinon lahiparallaksi, korjattu lattiakosketus, 5120 x 1844 RGBA PNG.
Kalusteita siirretty 137 pikselia alemmas edellisesta versiosta: ne seisovat nyt matolla eivatka sen takareunalla. Jalkojen alin rivi on y=1335, lattian alku y=1199.
Alkuperaiset kalusteet sailytetty. Image_gen-tyokalulla tuotetuista kosketusvarjoista kaytetty vain lattian tummennus jalkojen ymparilla. Muuten sama alkuperainen matto.
Ylaosan tyhja tila lapinakyva, matto yhtenainen ja taysin peittava. Kaukotaustakuvaa ei muutettu.
taustan-kanssa-esikatselu.png nayttaa kerrokset yhdistettyna; se ei ole varsinainen pelikerros.
Tuotanto: sisaanrakennettu image_gen, mekaaninen kohdistus ja paikallinen varjojen yhdistaminen. Kalusteiden natiivi 2089 x 753, vienti skaalattu 5120 x 1844 kokoon.
'@ | Set-Content -LiteralPath (Join-Path $folder 'LUE-MINUT.txt') -Encoding UTF8
"5120x1844 RGBA; floor y1199; furniture contact y1335. $report" | Set-Content -LiteralPath (Join-Path $folder 'tarkistus.txt') -Encoding UTF8
$base.Dispose();$bg.Dispose();$raw.Dispose();$ed.Dispose();$combo.Dispose()
Write-Output $report
