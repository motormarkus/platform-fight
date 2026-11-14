$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$outDir=Join-Path $PSScriptRoot 'vegas-baari-bakkari-v3'
[void][IO.Directory]::CreateDirectory($outDir)
$sourcePath=Join-Path $PSScriptRoot 'vegas-baari-bakkari-v2\vegas-baari-bakkari-v2-5120x1844.png'
$hash=(Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
$source=[Drawing.Bitmap]::new($sourcePath)
$raw=[Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-6811b0c2-9414-49a8-8dd1-be3facc45fac.png')
$native='{0}x{1}' -f $raw.Width,$raw.Height
$crop=Resize-Canvas $raw 1320 900
$final=$source.Clone([Drawing.Rectangle]::new(0,0,5120,1844),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$before=Pixel-Data $source;$pixels=Pixel-Data $final;$edit=Pixel-Data $crop
Add-Type -TypeDefinition @'
using System;
public static class BackstageBass {
 static double S(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
 public static void Add(byte[] d,byte[] b){
  for(int y=40;y<860;y++)for(int x=440;x<880;x++){
   double a=S((x-440)/44.0)*S((879-x)/44.0)*S((y-40)/44.0)*S((859-y)/44.0);
   int i=((y+140)*5120+x+1120)*4,j=(y*1320+x)*4;
   for(int c=0;c<3;c++)d[i+c]=(byte)Math.Round(d[i+c]*(1-a)+b[j+c]*a);
  }
 }
 public static string Check(byte[] before,byte[] after){
  int changed=0,outside=0,alpha=0;
  for(int y=0;y<1844;y++)for(int x=0;x<5120;x++){
   int i=(y*5120+x)*4;bool delta=false;
   for(int c=0;c<4;c++)if(before[i+c]!=after[i+c])delta=true;
   if(delta){changed++;if(x<1560||x>=2000||y<180||y>=1000)outside++;}
   if(after[i+3]!=255)alpha++;
  }
  if(outside!=0||alpha!=0)throw new Exception("Pixels outside bass wall region changed="+outside+", nonopaque="+alpha);
  return "5120x1844 opaque; changes outside central bass wall rectangle=0; changed pixels="+changed+". Both original posters and frames, furniture, door, lighting and floor are pixel-identical to v2.";
 }
}
'@
[BackstageBass]::Add($pixels,$edit)
Set-PixelData $final $pixels
$path=Join-Path $outDir 'vegas-baari-bakkari-v3-5120x1844.png'
$final.Save($path,[Drawing.Imaging.ImageFormat]::Png)
$saved=[Drawing.Bitmap]::new($path)
$report=[BackstageBass]::Check($before,(Pixel-Data $saved))
$preview=Resize-Canvas $saved 1280 461
$preview.Save((Join-Path $outDir 'esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
$detail=$saved.Clone([Drawing.Rectangle]::new(1120,140,1320,900),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$detail.Save((Join-Path $outDir 'basso-ja-julisteet-lahikuva.png'),[Drawing.Imaging.ImageFormat]::Png)
$detail.Dispose();$preview.Dispose();$saved.Dispose();$final.Dispose();$source.Dispose();$raw.Dispose();$crop.Dispose()
if((Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash -ne $hash){throw 'Original source changed.'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vegas-baari-basso-generointiohje.txt') -Destination (Join-Path $outDir 'generointiohje.txt')
@"
Vegasin baarin bäkkäri v3, sähköbasso seinällä

Pelikuva: vegas-baari-bakkari-v3-5120x1844.png
Lisätty nelikielinen sähköbasso HMS Keelhaul- ja Northforge-julisteiden väliin. Julisteet ovat samat alkuperäiset grafiikat samankokoisina kuin v2:ssa.
Vain basson ja sen varjon tarvitsema seinäalue muuttui: x=1560..1999, y=180..999. Tämän alueen ulkopuoliset pikselit ovat identtiset v2:n kanssa. Alkuperäistä tiedostoa ei muokattu.
Tekotapa: sisäänrakennettu image_gen, rajatun seinäalueen muokkaus. Työkalun natiivikoko $native. Basson alue yhdistetty 5120 x 1844 alkuperäiseen vientikuvaan reunoja häivyttäen. Vienti ei tarkoita koko huoneen uudelleen generointia natiivina 5K-kuvana.
Generointiohje: generointiohje.txt
"@ | Set-Content -LiteralPath (Join-Path $outDir 'LUE-MINUT.txt') -Encoding UTF8
@($report,"Source SHA256 unchanged=$hash","Generated edit native dimensions=$native") | Set-Content -LiteralPath (Join-Path $outDir 'tarkistus.txt') -Encoding UTF8
Write-Output $path
Write-Output $report
