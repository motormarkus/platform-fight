$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$outDir=Join-Path $PSScriptRoot 'vegas-baari-sisatilat-v2'
[void][System.IO.Directory]::CreateDirectory($outDir)
$leftPath=Join-Path $PSScriptRoot 'vegas-baari-sisatilat-vasen\vegas-baari-sisatilat-vasen-5120x1844.png'
$rightPath=Join-Path $PSScriptRoot 'vegas-baari-sisatilat-lava\vegas-baari-sisatilat-lava-5120x1844.png'
$leftHash=(Get-FileHash -LiteralPath $leftPath -Algorithm SHA256).Hash
$rightHash=(Get-FileHash -LiteralPath $rightPath -Algorithm SHA256).Hash
$left=[System.Drawing.Bitmap]::new($leftPath)
$right=[System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-baari-lava-tyo\lava-tyhja-5120x1844.png'))
$rightOriginal=[System.Drawing.Bitmap]::new($rightPath)
$raw=[System.Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-7f22ed3e-1b80-44d0-a6bf-032abfdb757e.png')
$edit=Resize-Canvas $raw 4030 1400
$dataLeft=Pixel-Data $left;$dataRight=Pixel-Data $right
$originalLeft=[byte[]]$dataLeft.Clone();$originalRight=Pixel-Data $rightOriginal;$patch=Pixel-Data $edit
Add-Type -TypeDefinition @'
using System;
public static class BarCorrections {
 // Shelf interiors in the 4030x1400 joined reference, avoiding columns and glassware bays.
 static int[,] bays=new int[,]{{278,785},{1208,1424},{1684,2175},{2865,3372}};
 static int[,] rows=new int[,]{{200,389},{397,562}};
 static double S(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
 static bool BottleArea(int x,int y){
  for(int b=0;b<4;b++)for(int r=0;r<2;r++)if(x>=bays[b,0]&&x<bays[b,1]&&y>=rows[r,0]&&y<rows[r,1])return true;
  return false;
 }
 public static void Paste(byte[] left,byte[] right,byte[] p){
  for(int b=0;b<4;b++)for(int r=0;r<2;r++){
   int x0=bays[b,0],x1=bays[b,1],y0=rows[r,0],y1=rows[r,1];
   for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++){
    double a=S((x-x0)/5.0)*S((x1-1-x)/5.0)*S((y-y0)/5.0)*S((y1-1-y)/5.0);
    byte[] d=x<1370?left:right;int dx=x<1370?3750+x:x-1370;
    int q=(y*5120+dx)*4,s=(y*4030+x)*4;
    for(int c=0;c<4;c++)d[q+c]=(byte)Math.Round(d[q+c]*(1-a)+p[s+c]*a);
   }
  }
  // Preserve the established equal edge values; a one-column correction within the edited bottle cabinet only.
  for(int y=0;y<1844;y++)for(int c=0;c<4;c++)right[(y*5120)*4+c]=left[(y*5120+5119)*4+c];
 }
 public static string Verify(byte[] left,byte[] right,byte[] oldLeft,byte[] oldRight){
  int edge=0,opaque=0,outside=0,floor=0,changedLeft=0,changedRight=0;
  for(int y=0;y<1844;y++)for(int x=0;x<5120;x++){
   int i=(y*5120+x)*4;bool dl=false,dr=false;
   for(int c=0;c<4;c++){if(left[i+c]!=oldLeft[i+c])dl=true;if(right[i+c]!=oldRight[i+c])dr=true;}
   if(dl){changedLeft++;if(!BottleArea(x-3750,y))outside++;}
   if(dr){changedRight++;bool stage=x>=2780&&x<4960&&y>=256&&y<888;if(!stage&&!BottleArea(x+1370,y))outside++;}
   if(y>=968&&(dl||dr))floor++;
   if(left[i+3]!=255||right[i+3]!=255)opaque++;
   if(x==0)for(int c=0;c<4;c++)if(right[i+c]!=left[(y*5120+5119)*4+c])edge++;
  }
  if(edge+opaque+outside+floor>0)throw new Exception("Errors: edge="+edge+", alpha="+opaque+", outside="+outside+", floor="+floor);
  return "Both images 5120x1844, opaque. Joint edge RGBA differences=0 for all 1844 rows. Changes outside bottle shelf interiors and the stage contents region=0. Walking floor changes (y968..1843)=0. Changed pixels: left="+changedLeft+", right="+changedRight+".";
 }
}
'@
[BarCorrections]::Paste($dataLeft,$dataRight,$patch)
$report=[BarCorrections]::Verify($dataLeft,$dataRight,$originalLeft,$originalRight)
Set-PixelData $left $dataLeft;Set-PixelData $right $dataRight
$finalLeft=Join-Path $outDir 'vegas-baari-sisatilat-vasen-v2-5120x1844.png'
$finalRight=Join-Path $outDir 'vegas-baari-sisatilat-lava-v2-5120x1844.png'
$left.Save($finalLeft,[System.Drawing.Imaging.ImageFormat]::Png)
$right.Save($finalRight,[System.Drawing.Imaging.ImageFormat]::Png)
$smallLeft=Resize-Canvas $left 1280 461;$smallRight=Resize-Canvas $right 1280 461
$smallLeft.Save((Join-Path $outDir 'vasen-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$smallRight.Save((Join-Path $outDir 'lava-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$pair=[System.Drawing.Bitmap]::new(2560,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($pair);$g.DrawImageUnscaled($smallLeft,0,0);$g.DrawImageUnscaled($smallRight,1280,0);$g.Dispose()
$pair.Save((Join-Path $outDir 'liitos-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$bar=[System.Drawing.Bitmap]::new(4030,1400,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($bar);$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.DrawImage($left,[System.Drawing.Rectangle]::new(0,0,1370,1400),[System.Drawing.Rectangle]::new(3750,0,1370,1400),[System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($right,[System.Drawing.Rectangle]::new(1370,0,2660,1400),[System.Drawing.Rectangle]::new(0,0,2660,1400),[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$barSmall=Resize-Canvas $bar 2015 700
$barSmall.Save((Join-Path $outDir 'pullot-lahikuva.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$edge=$right.Clone([System.Drawing.Rectangle]::new(4864,0,256,1844),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$edge.Save((Join-Path $outDir 'oikean-reunan-jatkoviite.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$edge.Dispose();$barSmall.Dispose();$bar.Dispose();$pair.Dispose();$smallLeft.Dispose();$smallRight.Dispose();$left.Dispose();$right.Dispose();$rightOriginal.Dispose();$raw.Dispose();$edit.Dispose()
if((Get-FileHash -LiteralPath $leftPath -Algorithm SHA256).Hash -ne $leftHash){throw 'Original left image changed.'}
if((Get-FileHash -LiteralPath $rightPath -Algorithm SHA256).Hash -ne $rightHash){throw 'Original stage image changed.'}
$stagePrompt=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'vegas-baari-lava-tyhjennys-generointiohje.txt') -Raw
$bottlePrompt=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'vegas-baari-pullot-generointiohje.txt') -Raw
@('Mode: built-in image_gen','', 'STAGE CONTENTS EDIT', $stagePrompt,'','LIQUOR BOTTLE EDIT ACROSS BOTH ROOM TILES',$bottlePrompt) | Set-Content -LiteralPath (Join-Path $outDir 'generointiohjeet.txt') -Encoding UTF8
@"
Vegasin baarin sisätilat, korjatut kuvat

Molemmat pelipalat ovat 5120 x 1844 PNG:
1. vegas-baari-sisatilat-vasen-v2-5120x1844.png
2. vegas-baari-sisatilat-lava-v2-5120x1844.png
Aseta palat tässä järjestyksessä vierekkäin samoilla PPU-, pivot-, skaala- ja korkeusasetuksilla.

Molempien palojen pullorivit korjattu: erimuotoisia ja erikokoisia viinapulloja, vihreää, ruskeaa, kirkasta ja sinistä lasia, eri etikettejä ilman tekstiä. Pullot eivät enää ole identtisiä keltaisina hohtavia esineitä.
Lavalta poistettu rummut, kitara, vahvistimet, mikrofoniteline ja soitintelineet. Vain kaksi PA-kaiutinta jää lavan nurkkiin. Keskiosa on tyhjä myöhemmin lisättäville bändin spriteille.
Muutokset rajattu pullokaappien sisäpintoihin ja lavan esineiden alueelle. Muu sisustus, tiski, portaat, lavan etureuna, kävelylattia ja liitosgeometria säilyvät. Kävelylattia on pikseliarvoiltaan sama kuin aiemmissa kuvissa. Alkuperäisiä PNG-tiedostoja tai Unity-scenejä ei muutettu.

Tekotapa: sisäänrakennettu image_gen, kaksi kohdennettua muokkausta. Lavan muokkauksen natiivikoko 2138 x 736, pullomuokkauksen 2128 x 739. Muokkaukset sovitettu alkuperäisiin 5120 x 1844 pelikuviin vain sallituilla alueilla. Vientikoko ei ole generoinnin natiiviresoluutio.
"@ | Set-Content -LiteralPath (Join-Path $outDir 'LUE-MINUT.txt') -Encoding UTF8
@($report,"Original left SHA256 unchanged: $leftHash","Original right SHA256 unchanged: $rightHash") | Set-Content -LiteralPath (Join-Path $outDir 'tarkistus.txt') -Encoding UTF8
Write-Output $report
Write-Output $finalLeft
Write-Output $finalRight
