$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$outDir=Join-Path $PSScriptRoot 'vegas-baari-bakkari-v2'
[void][System.IO.Directory]::CreateDirectory($outDir)
$originalPath=Join-Path $PSScriptRoot 'vegas-baari-bakkari\vegas-baari-bakkari-5120x1844.png'
$hash=(Get-FileHash -LiteralPath $originalPath -Algorithm SHA256).Hash
$original=[System.Drawing.Bitmap]::new($originalPath)
$raw=[System.Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-c50bad17-1f4b-4ca8-a7d6-efa197fce3e5.png')
$native='{0}x{1}' -f $raw.Width,$raw.Height
$wall=Resize-Canvas $raw 5120 1844
$final=$original.Clone([System.Drawing.Rectangle]::new(0,0,5120,1844),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$before=Pixel-Data $original;$pixels=Pixel-Data $final;$fill=Pixel-Data $wall
$hmsMask=[System.Drawing.Rectangle]::new(966,12,820,1068)
$northMask=[System.Drawing.Rectangle]::new(1748,125,830,860)
Add-Type -TypeDefinition @'
using System;
using System.Drawing;
public static class SmallerBandPosters {
 static double S(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
 public static void Restore(byte[] d,byte[] b,Rectangle r,int feather){
  for(int y=r.Top;y<r.Bottom;y++)for(int x=r.Left;x<r.Right;x++){
   double a=S((x-r.Left)/(double)feather)*S((r.Right-1-x)/(double)feather)*S((y-r.Top)/(double)feather)*S((r.Bottom-1-y)/(double)feather);
   int i=(y*5120+x)*4;for(int c=0;c<4;c++)d[i+c]=(byte)Math.Round(d[i+c]*(1-a)+b[i+c]*a);
  }
 }
 public static string Check(byte[] old,byte[] d,Rectangle a,Rectangle b){int outside=0,alpha=0,changed=0;for(int y=0;y<1844;y++)for(int x=0;x<5120;x++){int i=(y*5120+x)*4;bool delta=false;for(int c=0;c<4;c++)if(d[i+c]!=old[i+c])delta=true;if(delta){changed++;if(!a.Contains(x,y)&&!b.Contains(x,y))outside++;}if(d[i+3]!=255)alpha++;}if(outside+alpha>0)throw new Exception("Changed outside poster wall="+outside+", nonopaque="+alpha);return "5120x1844 opaque. Changes outside the two former poster wall regions=0. Door, furnishings and floor preserved. Changed pixels="+changed+".";}
}
'@
[SmallerBandPosters]::Restore($pixels,$fill,$hmsMask,12)
[SmallerBandPosters]::Restore($pixels,$fill,$northMask,12)
Set-PixelData $final $pixels
# Scale the actual approved framed prints, including the supplied artwork, to 45% per dimension.
$hmsSource=[System.Drawing.Rectangle]::new(1002,34,736,1010)
$northSource=[System.Drawing.Rectangle]::new(1797,149,768,767)
function Reduced-Rect([System.Drawing.Rectangle]$r){
 $w=[int][Math]::Round($r.Width*0.45);$h=[int][Math]::Round($r.Height*0.45)
 return [System.Drawing.Rectangle]::new(($r.X+[int][Math]::Floor(($r.Width-$w)/2.0)),($r.Y+[int][Math]::Floor(($r.Height-$h)/2.0)),$w,$h)
}
$hmsDest=Reduced-Rect $hmsSource;$northDest=Reduced-Rect $northSource
$g=[System.Drawing.Graphics]::FromImage($final);$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[System.Drawing.Imaging.ImageAttributes]::new();$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
foreach($pair in @(@($hmsSource,$hmsDest),@($northSource,$northDest))){
 $s=$pair[0];$d=$pair[1]
 $g.DrawImage($original,$d,[single]$s.X,[single]$s.Y,[single]$s.Width,[single]$s.Height,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
}
$attrs.Dispose();$g.Dispose()
$path=Join-Path $outDir 'vegas-baari-bakkari-v2-5120x1844.png'
$final.Save($path,[System.Drawing.Imaging.ImageFormat]::Png)
$saved=[System.Drawing.Bitmap]::new($path)
$report=[SmallerBandPosters]::Check($before,(Pixel-Data $saved),$hmsMask,$northMask)
$preview=Resize-Canvas $saved 1280 461
$preview.Save((Join-Path $outDir 'esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose();$saved.Dispose();$final.Dispose();$original.Dispose();$raw.Dispose();$wall.Dispose()
if((Get-FileHash -LiteralPath $originalPath -Algorithm SHA256).Hash -ne $hash){throw 'Original file changed.'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vegas-baari-bakkari-julisteet-pienemmiksi-generointiohje.txt') -Destination (Join-Path $outDir 'generointiohje.txt')
@"
Vegasin baarin bäkkäri, pienemmät julisteet

Pelikuva: vegas-baari-bakkari-v2-5120x1844.png. Koko ja rajaus säilyvät 5120 x 1844 pikselissä.
Molempien julisteiden leveys ja korkeus pienennetty 45 prosenttiin aiemmasta eli 55 prosenttia pienemmiksi. HMS KEELHAUL -kehyksen koko nyt $($hmsDest.Width) x $($hmsDest.Height), NORTHFORGE-kehyksen koko $($northDest.Width) x $($northDest.Height) pikseliä. Julisteiden keskikohdat säilytetty.
Julistegrafiikat ja kehykset ovat alkuperäisestä hyväksytystä kuvasta, vain pienennettyinä. HMS KEELHAUL -logo pysyy henkilökuvan yläpuolella. Northforge-grafiikka ei muuttunut.
Vanhojen suurten julisteiden takainen seinä palautettu. Muu huone, ovi, valaisimet, kalusteet ja lattia säilyvät pikseliarvoiltaan ennallaan vanhojen julistealueiden ulkopuolella.

Tekotapa: sisäänrakennettu image_gen seinäpinnan palauttamiseen, natiivi muokkauskuva $native. Muokkaus yhdistetty alkuperäiseen 5120 x 1844 vientikuvaan rajatuilla alueilla; aidot julisteet pienennetty mekaanisesti. Alkuperäistä tiedostoa tai Unity-scenejä ei muutettu.
"@ | Set-Content -LiteralPath (Join-Path $outDir 'LUE-MINUT.txt') -Encoding UTF8
@($report,"HMS frame=$hmsDest","Northforge frame=$northDest",'Linear size=45 percent; area about 20 percent of previous',"Original SHA256 unchanged=$hash") | Set-Content -LiteralPath (Join-Path $outDir 'tarkistus.txt') -Encoding UTF8
Write-Output $path
Write-Output $report
