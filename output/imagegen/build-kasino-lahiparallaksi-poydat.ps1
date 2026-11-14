$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
Add-Type -TypeDefinition @'
using System;
public static class NearCasinoLayer {
 public static int LastVisibleRow(byte[] p,int w,int h) {
  for(int y=h-1;y>=0;y--) for(int x=0;x<w;x++) if(p[4*(y*w+x)+3]>16) return y;
  return -1;
 }
 public static byte[] Compose(byte[] p,byte[] bg,int w,int h,int floor,int shift) {
  byte[] d=new byte[w*h*4]; int stride=w*4;
  for(int y=0;y<floor;y++) {
   int sy=y-shift;
   if(sy>=0&&sy<h) Array.Copy(p,sy*stride,d,y*stride,stride);
  }
  Array.Copy(bg,floor*stride,d,floor*stride,(h-floor)*stride);
  return d;
 }
 public static string Check(byte[] d,byte[] bg,int w,int h,int floor) {
  int clear=0,diff=0,hidden=0;
  for(int i=0;i<d.Length;i+=4) {
   if(d[i+3]==0){clear++;if(d[i]!=0||d[i+1]!=0||d[i+2]!=0)hidden++;}
   if(i>=floor*w*4)for(int c=0;c<4;c++)if(d[i+c]!=bg[i+c])diff++;
  }
  if(diff!=0||hidden!=0||clear<1000000)throw new Exception("Layer validation failed");
  return "Transparent pixels="+clear+"; carpet channel differences="+diff+"; hidden RGB pixels="+hidden;
 }
}
'@
$folder = Join-Path $PSScriptRoot 'kasino-lahiparallaksi-poydat'
[void][System.IO.Directory]::CreateDirectory($folder)
$src=[System.Drawing.Bitmap]::FromFile('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-895841d2-b2b9-4819-ae82-2d84d10c59db.png')
$null=Clean-Alpha $src
$props=Resize-Canvas $src 5120 1844
$null=Clean-Alpha $props
$pd=Pixel-Data $props
$background=[System.Drawing.Bitmap]::FromFile((Join-Path $PSScriptRoot 'kasino-kaukotausta-pramea-v2\kasino-kaukotausta-pramea-v2-5120x1844.png'))
$bd=Pixel-Data $background
$last=[NearCasinoLayer]::LastVisibleRow($pd,5120,1844)
$shift=1198-$last
$data=[NearCasinoLayer]::Compose($pd,$bd,5120,1844,1199,$shift)
$report=[NearCasinoLayer]::Check($data,$bd,5120,1844,1199)
$layer=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
Set-PixelData $layer $data
$layer.Save((Join-Path $folder 'kasino-lahiparallaksi-poydat-5120x1844.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview=Resize-Canvas $layer 1280 461
$preview.Save((Join-Path $folder 'esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
$combined=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($combined)
$g.DrawImageUnscaled($background,0,0)
$g.DrawImageUnscaled($layer,0,0)
$g.Dispose()
$preview=Resize-Canvas $combined 1280 461
$preview.Save((Join-Path $folder 'taustan-kanssa-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'kasino-lahiparallaksi-poydat-generointiohje.txt') -Destination (Join-Path $folder 'generointiohje.txt')
@'
Kasinon lahempi parallaksikerros, 5120 x 1844 RGBA PNG.
Blackjack-poyta, noppapoyta ja kolme slottikonetta. Ylaosa kalusteiden ymparilla on aidosti lapinakyva.
Punainen matto y=1199..1843 on kopioitu sellaisenaan taustakuvan kasino-kaukotausta-pramea-v2-5120x1844.png samoista pikseleista: lattia on tasmalleen sama.
Aseta taman kuvan taakse kasino-kaukotausta-pramea-v2-5120x1844.png. Kuvilla sama koko ja pystykohdistus; liikuttele kaukotaustaa hitaammin vaakasuunnassa.
taustan-kanssa-esikatselu.png on kerrosten yhdistelman esikatselu, ei varsinainen pelikerros.
Kalusteet tuotettu sisaanrakennetulla image_gen-tyokalulla, natiivi 2089 x 753; vientiin skaalattu 5120 x 1844. Kalusteiden alareuna kohdistettu lattian alkuun. Taustakuvaa ei muutettu.
Generointiohje on tiedostossa generointiohje.txt. Ei Unity-scenejen tai asetusten muutoksia.
'@ | Set-Content -LiteralPath (Join-Path $folder 'LUE-MINUT.txt') -Encoding UTF8
"5120x1844 RGBA; carpet starts y1199; props shifted vertically $shift px. $report" | Set-Content -LiteralPath (Join-Path $folder 'tarkistus.txt') -Encoding UTF8
$combined.Dispose();$layer.Dispose();$props.Dispose();$src.Dispose();$background.Dispose()
Write-Output $report
Write-Output (Join-Path $folder 'kasino-lahiparallaksi-poydat-5120x1844.png')
