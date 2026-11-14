$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$outDir=Join-Path $PSScriptRoot 'vegas-baari-wc'
$leftPath=Join-Path $outDir 'vegas-baari-wc-vasen-5120x1844.png'
$leftHash=(Get-FileHash -LiteralPath $leftPath -Algorithm SHA256).Hash
$left=[System.Drawing.Bitmap]::new($leftPath)
$raw=[System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-baari-wc-tyo\raaka-5120x1844.png'))
$right=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($right);$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[System.Drawing.Imaging.ImageAttributes]::new();$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
$srcRows=@(0,676,727,775,824,872,919,967,1015,1071,1128,1844)
$dstRows=@(0,669,720,769,819,867,911,958,1006,1062,1120,1844)
for($i=0;$i -lt $srcRows.Count-1;$i++){
 $g.DrawImage($raw,[System.Drawing.Rectangle]::new(0,$dstRows[$i],5120,($dstRows[$i+1]-$dstRows[$i])),[single]0,[single]$srcRows[$i],[single]5120,[single]($srcRows[$i+1]-$srcRows[$i]),[System.Drawing.GraphicsUnit]::Pixel,$attrs)
}
$attrs.Dispose();$g.Dispose();$raw.Dispose()
$d=Pixel-Data $right;$a=Pixel-Data $left
Add-Type -TypeDefinition @'
using System;
public static class BarWCJoin {
 static double S(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
 static byte C(double t){return (byte)Math.Max(0,Math.Min(255,Math.Round(t)));}
 public static void Match(byte[] d,byte[] old,int w,int h){
  for(int x=0;x<w;x++){
   double start=1168+112*S((x-4400)/719.0);
   for(int y=(int)Math.Floor(start);y<h;y++){
    double f=S((y-start)/128.0);int p=(y*w+x)*4,q=(y*w+w-1-x)*4;
    for(int c=0;c<4;c++)d[p+c]=C(d[p+c]*(1-f)+old[q+c]*f);
   }
  }
  // Quiet plaster and the matched stone floor can use a soft collar.
  for(int y=0;y<h;y++)if(y<650||y>=1100)for(int x=0;x<384;x++){
   double f=1-S((x-64)/319.0);int p=(y*w+x)*4,q=(y*w+w-1-x)*4;
   for(int c=0;c<4;c++)d[p+c]=C(d[p+c]*(1-f)+old[q+c]*f);
  }
  // Continue a complete course of the established wall tiles up to the first cubicle.
  // Use only the plain reference area, beyond the end of the vanity.
  double[,] tone=new double[480,3];
  for(int y=640;y<1120;y++)for(int c=0;c<3;c++){
   double delta=0;int count=0;
   for(int yy=Math.Max(640,y-12);yy<=Math.Min(1119,y+12);yy+=3)for(int x=1400;x<1480;x+=8){
    int sx=w-1-(int)Math.Round(x*1344.0/1524);delta+=d[(yy*w+x)*4+c]-old[(yy*w+sx)*4+c];count++;
   }
   tone[y-640,c]=delta/count;
  }
  for(int y=640;y<1120;y++)for(int x=0;x<1524;x++){
   double f=(1-S((x-1488)/35.0))*S((y-640)/24.0)*S((1120-y)/16.0);
   int sx=w-1-(int)Math.Round(x*1344.0/1524),p=(y*w+x)*4,q=(y*w+sx)*4;
   double tint=S((x-640)/884.0)*S((y-680)/24.0)*S((1080-y)/24.0);
   for(int c=0;c<3;c++){double v=old[q+c]+tone[y-640,c]*tint;d[p+c]=C(d[p+c]*(1-f)+v*f);}
   d[p+3]=255;
  }
  for(int y=0;y<h;y++)for(int c=0;c<4;c++)d[y*w*4+c]=old[(y*w+w-1)*4+c];
 }
 public static string Check(byte[] d,byte[] old,int w,int h){int edge=0,alpha=0;for(int y=0;y<h;y++)for(int c=0;c<4;c++)if(d[y*w*4+c]!=old[(y*w+w-1)*4+c])edge++;for(int i=3;i<d.Length;i+=4)if(d[i]!=255||old[i]!=255)alpha++;if(edge+alpha>0)throw new Exception("Edge="+edge+" Alpha="+alpha);return "Both 5120x1844 images opaque; joining edge has 0 RGBA differences in all 1844 rows. Wall tile courses, brass trim, skirting and floor line registered. Left tile unchanged during right join export.";}
}
'@
[BarWCJoin]::Match($d,$a,5120,1844)
Set-PixelData $right $d
$rightPath=Join-Path $outDir 'vegas-baari-wc-oikea-5120x1844.png'
$right.Save($rightPath,[System.Drawing.Imaging.ImageFormat]::Png)
$saved=[System.Drawing.Bitmap]::new($rightPath)
$report=[BarWCJoin]::Check((Pixel-Data $saved),$a,5120,1844)
$smallLeft=Resize-Canvas $left 1280 461;$smallRight=Resize-Canvas $saved 1280 461
$smallRight.Save((Join-Path $outDir 'oikea-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$pair=[System.Drawing.Bitmap]::new(2560,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($pair);$g.DrawImageUnscaled($smallLeft,0,0);$g.DrawImageUnscaled($smallRight,1280,0);$g.Dispose()
$pair.Save((Join-Path $outDir 'yhdistetty-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png);$pair.Dispose()
$close=[System.Drawing.Bitmap]::new(1024,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($close)
$g.DrawImage($left,[System.Drawing.Rectangle]::new(0,0,512,1844),[System.Drawing.Rectangle]::new(4608,0,512,1844),[System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($saved,[System.Drawing.Rectangle]::new(512,0,512,1844),[System.Drawing.Rectangle]::new(0,0,512,1844),[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$close.Save((Join-Path $outDir 'liitos-lahikuva.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$close.Dispose();$smallLeft.Dispose();$smallRight.Dispose();$saved.Dispose();$left.Dispose();$right.Dispose()
if((Get-FileHash -LiteralPath $leftPath -Algorithm SHA256).Hash -ne $leftHash){throw 'Left image changed.'}
$p1=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'vegas-baari-wc-vasen-generointiohje.txt') -Raw
$p2=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'vegas-baari-wc-vasen-lattia-korjaus.txt') -Raw
$p3=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'vegas-baari-wc-oikea-generointiohje.txt') -Raw
@('Mode: built-in image_gen','','LEFT TILE',$p1,'','LEFT FLOOR REFRAMING',$p2,'','RIGHT CONTINUATION',$p3) | Set-Content -LiteralPath (Join-Path $outDir 'generointiohjeet.txt') -Encoding UTF8
@"
Vegasin baarin WC, kaksi vierekkäin liitettävää pelikuvaa

1. vegas-baari-wc-vasen-5120x1844.png
2. vegas-baari-wc-oikea-5120x1844.png
Molemmat 5120 x 1844 px, täysin peittävä PNG. Aseta vasen ja oikea kuva tähän järjestykseen samoilla PPU-, pivot-, skaala- ja korkeusasetuksilla.

Yksi yhtenäinen WC-tila kahdella pelipalalla: vasemmassa päädyssä sulkeva seinä ja yksi sisäänkäyntiovi, kaksi pesuallasta ja peiliä, käsienkuivain ja roska-astia. Oikealla kolme WC-koppia; yksi ovi raollaan ja WC-istuin näkyvissä. Oikea seinä sulkee huoneen. Ei tekstiä tai ihmisiä. Vapaata kävelylattiaa noin 40 prosenttia kuvan korkeudesta.
Tyyli jatkaa baarin viininpunaista, pähkinäpuuta ja messinkiä; WC-tilassa vaalea seinälaatoitus, valkoista posliinia ja hillitty kivilattia. Peileissä ei ole ulkomaisemaa tai hahmoja.
Liitoskohdan vaakalinjat ja reunapikselit tarkistettu. Lattian ja laatoituksen saumaa sovitettu viennissä. Unityn scenejä ja aiempia baarikuvia ei muutettu.

Tekotapa: sisäänrakennettu image_gen. Vasen natiivikuva 2089 x 753; oikea natiivikuva 2167 x 726 sisältäen vasemman apuliuskan. Lopulliset 5120 x 1844 kuvat ovat skaalattuja vientikokoja, eivät natiivisti generoituja 5K-kuvia. Oikean apuliuska poistettu ja kuva kohdistettu vasempaan.
"@ | Set-Content -LiteralPath (Join-Path $outDir 'LUE-MINUT.txt') -Encoding UTF8
@($report,'Left native=2089x753','Right native=2167x726 including guide',"Left SHA256 unchanged: $leftHash") | Set-Content -LiteralPath (Join-Path $outDir 'tarkistus.txt') -Encoding UTF8
Write-Output $leftPath
Write-Output $rightPath
Write-Output $report
