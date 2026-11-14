$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$outDir=Join-Path $PSScriptRoot 'vegas-baari-bakkari-v4'
[void][IO.Directory]::CreateDirectory($outDir)
$sourcePath=Join-Path $PSScriptRoot 'vegas-baari-bakkari-v3\vegas-baari-bakkari-v3-5120x1844.png'
$hash=(Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
$source=[Drawing.Bitmap]::new($sourcePath)
$raw=[Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-2e00efc2-f0dc-423e-ab94-cb64e2831139.png')
$native='{0}x{1}' -f $raw.Width,$raw.Height
$crop=Resize-Canvas $raw 1320 1000
$final=$source.Clone([Drawing.Rectangle]::new(0,0,5120,1844),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$before=Pixel-Data $source;$pixels=Pixel-Data $final;$edit=Pixel-Data $crop
Add-Type -TypeDefinition @'
using System;
public static class LongerBassNeck {
 static double S(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
 public static void Add(byte[] d,byte[] b){
  for(int y=24;y<514;y++)for(int x=565;x<740;x++){
   double a=S((x-565)/24.0)*S((739-x)/20.0)*S((y-24)/16.0)*S((513-y)/34.0);
   int i=((y+70)*5120+x+1120)*4,j=(y*1320+x)*4;
   for(int c=0;c<3;c++)d[i+c]=(byte)Math.Round(d[i+c]*(1-a)+b[j+c]*a);
  }
 }
 public static string Check(byte[] before,byte[] after){
  int changed=0,outside=0,alpha=0;
  for(int y=0;y<1844;y++)for(int x=0;x<5120;x++){
   int i=(y*5120+x)*4;bool delta=false;
   for(int c=0;c<4;c++)if(before[i+c]!=after[i+c])delta=true;
   if(delta){changed++;if(x<1685||x>=1860||y<94||y>=584)outside++;}
   if(after[i+3]!=255)alpha++;
  }
  if(outside!=0||alpha!=0)throw new Exception("Changes outside neck wall region="+outside+", nonopaque="+alpha);
  return "5120x1844 opaque; changes outside neck/headstock wall region=0; changed pixels="+changed+". The whole body from y=584 downward, both posters, furnishings, lighting and floor are pixel-identical to v3.";
 }
}
'@
[LongerBassNeck]::Add($pixels,$edit)
Set-PixelData $final $pixels
$path=Join-Path $outDir 'vegas-baari-bakkari-v4-5120x1844.png'
$final.Save($path,[Drawing.Imaging.ImageFormat]::Png)
$saved=[Drawing.Bitmap]::new($path)
$report=[LongerBassNeck]::Check($before,(Pixel-Data $saved))
$preview=Resize-Canvas $saved 1280 461
$preview.Save((Join-Path $outDir 'esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
$detail=$saved.Clone([Drawing.Rectangle]::new(1120,70,1320,1000),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$detail.Save((Join-Path $outDir 'basso-ja-julisteet-lahikuva.png'),[Drawing.Imaging.ImageFormat]::Png)
$detail.Dispose();$preview.Dispose();$saved.Dispose();$final.Dispose();$source.Dispose();$raw.Dispose();$crop.Dispose()
if((Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash -ne $hash){throw 'Source image changed.'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vegas-baari-basso-pidempi-kaula-generointiohje.txt') -Destination (Join-Path $outDir 'generointiohje.txt')
@"
Vegasin baarin bäkkäri v4: basson pidempi kaula

Pelikuva: vegas-baari-bakkari-v4-5120x1844.png
Basson kaulaa pidennetty ylöspäin noin 40 prosenttia. Virityslapa, neljä virityskoneistoa ja ripustin siirretty ylemmäs. Runko ja sen sijainti säilyvät alkuperäisinä; julisteita ja muuta huonetta ei muutettu.
Vain kaulan ja sen taustaseinän rajattu alue muuttui: x=1685..1859, y=94..583. Alueen ulkopuolella kaikki pikselit ovat identtiset v3:n kanssa.
Tekotapa: sisäänrakennettu image_gen, natiivi muokkauskuva $native. Generoitu kaula ja seinäalue yhdistetty alkuperäiseen 5120 x 1844 vientikuvaan. Muu kuvaa ei generoitu uudelleen, alkuperäistä v3-tiedostoa ei muokattu. 5120 x 1844 on vientikoko.
"@ | Set-Content -LiteralPath (Join-Path $outDir 'LUE-MINUT.txt') -Encoding UTF8
@($report,"Source SHA256 unchanged=$hash","Generated edit native dimensions=$native") | Set-Content -LiteralPath (Join-Path $outDir 'tarkistus.txt') -Encoding UTF8
Write-Output $path
Write-Output $report
