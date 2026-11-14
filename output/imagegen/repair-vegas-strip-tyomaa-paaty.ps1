$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
Add-Type -TypeDefinition @'
using System;
public static class WorksiteEndRepair {
 static double S(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
 public static void Apply(byte[] d,byte[] repair){
  for(int y=0;y<900;y++)for(int x=1;x<384;x++){
   double t=S(x/10.0)*(1-S((x-240)/144.0))*(1-S((y-824)/76.0));
   int i=(y*5120+x)*4,j=(y*1024+x+512)*4;
   double a=d[i+3]*(1-t)+repair[j+3]*t;
   for(int c=0;c<3;c++)d[i+c]=a>0?(byte)Math.Round((d[i+c]*d[i+3]*(1-t)+repair[j+c]*repair[j+3]*t)/a):(byte)0;
   d[i+3]=(byte)Math.Round(a);
  }
  for(int y=0;y<1196;y++)for(int x=2500;x<5120;x++){int i=(y*5120+x)*4;d[i]=d[i+1]=d[i+2]=d[i+3]=0;}
 }
}
'@
$dir=Join-Path $PSScriptRoot 'vegas-strip-tyomaa-paaty'
$work=Join-Path $PSScriptRoot 'vegas-strip-tyomaa-paaty-tyo'
$path=Join-Path $dir 'vegas-strip-tyomaa-paaty-5120x1844.png'
$basePath=Join-Path $work 'kohdistettu-ennen-pilarikorjausta.png'
if(!(Test-Path -LiteralPath $basePath)){Copy-Item -LiteralPath $path -Destination $basePath}
$im=[Drawing.Bitmap]::new($basePath)
$raw=[Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-9672bf48-3235-4881-9fd8-333a80817e6d.png')
$null=Clean-Alpha $raw;$fix=Resize-Canvas $raw 1024 960;$null=Clean-Alpha $fix
$d=Pixel-Data $im;[WorksiteEndRepair]::Apply($d,(Pixel-Data $fix));$null=[CasinoLeftPixels]::Normalize($d);Set-PixelData $im $d
$im.Save($path,[Drawing.Imaging.ImageFormat]::Png)
$preview=Resize-Canvas $im 1280 461;$preview.Save((Join-Path $dir 'etuala-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$preview.Dispose()
$upperWork=Join-Path $PSScriptRoot 'vegas-strip-tyomaa-ylajatko-tyo';[void][IO.Directory]::CreateDirectory($upperWork)
$small=Resize-Canvas $im 1280 461
$guide=[Drawing.Bitmap]::new(1280,557,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($guide);$g.Clear([Drawing.Color]::FromArgb(128,128,128));$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.DrawImage($small,[Drawing.Rectangle]::new(0,461,1280,96),[Drawing.Rectangle]::new(0,0,1280,96),[Drawing.GraphicsUnit]::Pixel);$g.Dispose()
$guide.Save((Join-Path $upperWork 'ylajatko-ohje.png'),[Drawing.Imaging.ImageFormat]::Png)
$guide.Dispose();$small.Dispose();$im.Dispose();$raw.Dispose();$fix.Dispose()
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vegas-strip-tyomaa-paaty-pilarikorjaus.txt') -Destination (Join-Path $dir 'liitoskorjauksen-generointiohje.txt')
Add-Content -LiteralPath (Join-Path $dir 'LUE-MINUT.txt') -Value 'Liitosalueen katkennut pilari poistettu paikallisella image_gen-muokkauksella. Korjaus rajattu uuden kuvan vasempaan ylaosaan; edellista kuvaa ei muutettu.'
