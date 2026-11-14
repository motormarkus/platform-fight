$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$dir=Join-Path $PSScriptRoot 'vegas-kappeli-maalaus-sprite'
[void][IO.Directory]::CreateDirectory($dir)
$src=[Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-02d5a283-d0ac-4a4f-8400-92715c08c2de.png')
$native='{0}x{1}' -f $src.Width,$src.Height
$null=Clean-Alpha $src
$d=Pixel-Data $src
Add-Type -TypeDefinition @'
using System;
public static class ChapelPaintingSpriteCheck {
 public static string Check(byte[] d,int w,int h){
  int inner=0,border=0,hidden=0,clear=0;int minx=w,miny=h,maxx=0,maxy=0;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int i=(y*w+x)*4;byte a=d[i+3];
   if(a==0){clear++;if(d[i]!=0||d[i+1]!=0||d[i+2]!=0)hidden++;}
   if(a>0){minx=Math.Min(minx,x);maxx=Math.Max(maxx,x);miny=Math.Min(miny,y);maxy=Math.Max(maxy,y);}
   if((x<30||x>=w-30||y<30||y>=h-30)&&a!=0)border++;
   if(x>=140&&x<w-140&&y>=140&&y<h-145&&a!=255)inner++;
  }
  if(inner+border+hidden>0)throw new Exception("Inner transparency="+inner+", outer border="+border+", hidden RGB="+hidden);
  return "RGBA PNG "+w+"x"+h+"; fully clear pixels="+clear+"; artwork inside fully opaque; external 30px margin fully transparent; hidden RGB=0; visible bounds="+minx+","+miny+".."+maxx+","+maxy+". Native dimensions retained, no enlargement.";
 }
}
'@
$report=[ChapelPaintingSpriteCheck]::Check($d,$src.Width,$src.Height)
$path=Join-Path $dir ('kappelin-oljymaalaus-sprite-'+$native+'.png')
$src.Save($path,[Drawing.Imaging.ImageFormat]::Png)
$preview=Resize-Canvas $src 1200 ([int][Math]::Round(1200*$src.Height/[double]$src.Width))
$preview.Save((Join-Path $dir 'esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose();$src.Dispose()
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vegas-kappeli-oljymaalaus-sprite-generointiohje.txt') -Destination (Join-Path $dir 'generointiohje.txt')
@"
Kappelin öljymaalaus: erillinen kehystetty sprite

Tiedosto: kappelin-oljymaalaus-sprite-$native.png
Koko $native pikseliä, natiivi generointitarkkuus, RGBA PNG.
Renessanssihenkinen öljymaalaus: Jumala ja aikuinen enkeli ojentavat etusormensa toisiaan kohti. Kultainen kehys sopii kappelin messinki-, kerma- ja roosaväreihin. Maalaus ja kehys ovat peittäviä; tausta kehyksen ympärillä on aidosti läpinäkyvä. Sprite on suoraan edestä ja kokonaan näkyvissä.
Maalausta ei yhdistetty kappelin seinään, eikä huonekuvia tai Unity-scenejä muokattu. Sijoita haluttuun kohtaan omana spritenään.
Tekotapa: sisäänrakennettu image_gen. Alfa siivottu täysin tyhjiksi ja peittäviksi arvoiksi ääripäissä; kuvan mittoja ei suurennettu. Erillinen alfa-tarkistus.png näyttää spriten siniharmaata taustaa vasten vain tarkastelua varten.
"@ | Set-Content -LiteralPath (Join-Path $dir 'LUE-MINUT.txt') -Encoding UTF8
$report | Set-Content -LiteralPath (Join-Path $dir 'tarkistus.txt') -Encoding UTF8
Write-Output $path
Write-Output $report
