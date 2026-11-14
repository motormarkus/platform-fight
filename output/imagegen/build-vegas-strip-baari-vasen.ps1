$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$outDir=Join-Path $PSScriptRoot 'vegas-strip-baari-vasen'
[void][System.IO.Directory]::CreateDirectory($outDir)
$oldPath=Join-Path $PSScriptRoot 'vegas-strip-kasino-ulkoa-v2\vegas-strip-kasino-ulkoa-v2-5120x1844.png'
$oldHash=(Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash
$old=[System.Drawing.Bitmap]::new($oldPath)
$raw=[System.Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-ccd1a569-cfc6-47fd-9def-5ca3e4f84f6f.png')
$native='{0}x{1}' -f $raw.Width,$raw.Height
$null=Clean-Alpha $raw
$cropped=Resize-Canvas $raw 5120 1844 ($raw.Width*1280/1376.0)
$raw.Dispose()
$final=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($final);$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[System.Drawing.Imaging.ImageAttributes]::new();$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
# Register facade details and the pavement/curb using measured row profiles at the join.
$srcRows=@(0,130,152,239,334,431,447,500,546,1080,1097,1135,1252,1294,1422,1435,1455,1844)
$dstRows=@(0,104,135,219,310,401,415,466,514,1025,1040,1076,1187,1224,1377,1390,1410,1844)
for($i=0;$i -lt $srcRows.Count-1;$i++){
 $g.DrawImage($cropped,[System.Drawing.Rectangle]::new(0,$dstRows[$i],5120,($dstRows[$i+1]-$dstRows[$i])),[single]0,[single]$srcRows[$i],[single]5120,[single]($srcRows[$i+1]-$srcRows[$i]),[System.Drawing.GraphicsUnit]::Pixel,$attrs)
}
$attrs.Dispose();$g.Dispose();$cropped.Dispose()
$null=Clean-Alpha $final
$data=Pixel-Data $final;$oldData=Pixel-Data $old
Add-Type -TypeDefinition @'
using System;
public static class VegasBarJoin {
 static byte Clamp(double x){return (byte)Math.Max(0,Math.Min(255,Math.Round(x)));}
 public static void FloorTone(byte[] d,byte[] old,int w,int h){
  const int top=1236;
  double[,] diff=new double[h,3];
  for(int y=top;y<h;y++)for(int c=0;c<3;c++){
   double a=0,b=0;int n=0;
   for(int yy=Math.Max(top,y-32);yy<=Math.Min(h-1,y+32);yy+=4)for(int x=0;x<96;x+=8){a+=old[(yy*w+x)*4+c];b+=d[(yy*w+w-1-x)*4+c];n++;}
   diff[y,c]=(a-b)/n;
  }
  for(int y=top;y<h;y++)for(int k=0;k<768;k++){
   double t=k/767.0,a=1-t*t*(3-2*t);int p=(y*w+w-1-k)*4;
   for(int c=0;c<3;c++)d[p+c]=Clamp(d[p+c]+diff[y,c]*a);
  }
 }
 public static void Quilt(byte[] d,byte[] old,int w,int h){
  const int band=384,exact=128,n=224;
  double[] prev=new double[n],curr=new double[n];short[] parent=new short[n*h];
  for(int y=0;y<h;y++){
   for(int k=0;k<n;k++){
    int dist=exact+16+k,a=(y*w+w-1-dist)*4,b=(y*w+dist)*4;double cost=0;
    for(int c=0;c<3;c++){
     double q=d[a+c]-old[b+c];cost+=q*q;
     cost+=(Math.Abs(d[a+4+c]-d[a-4+c])+Math.Abs(old[b+4+c]-old[b-4+c]))*12;
    }
    double center=(k-n*.55)/n;cost+=center*center*180;
    int pk=k;double best=0;
    if(y>0){best=double.PositiveInfinity;for(int q=Math.Max(0,k-2);q<=Math.Min(n-1,k+2);q++){double v=prev[q]+Math.Abs(q-k)*60;if(v<best){best=v;pk=q;}}}
    curr[k]=cost+best;parent[y*n+k]=(short)pk;
   }
   var temp=prev;prev=curr;curr=temp;
  }
  int last=0;for(int k=1;k<n;k++)if(prev[k]<prev[last])last=k;
  int[] cuts=new int[h];for(int y=h-1;y>=0;y--){cuts[y]=exact+16+last;last=parent[y*n+last];}
  for(int y=0;y<h;y++)for(int dist=0;dist<band;dist++){
   double t=Math.Max(0,Math.Min(1,(dist-cuts[y]+4)/8.0)),a=1-t*t*(3-2*t);
   // Continuous neon and metal bands blend smoothly; avoid an irregular quilt edge through their glow.
   double rowWeight=Math.Min(Math.Max(0,Math.Min(1,(y-165)/30.0)),Math.Max(0,Math.Min(1,(460-y)/30.0)));
   if(rowWeight>0){double u=Math.Max(0,Math.Min(1,(dist-exact)/(double)(band-exact-1)));double smooth=1-u*u*(3-2*u);a=a*(1-rowWeight)+smooth*rowWeight;}
   if(a<=0)continue;int p=(y*w+w-1-dist)*4,q=(y*w+dist)*4;
   for(int c=0;c<4;c++)d[p+c]=Clamp(d[p+c]*(1-a)+old[q+c]*a);
  }
 }
 public static string Verify(byte[] d,byte[] old,int w,int h){
  int edge=0,hidden=0,floor=0,gap=0,roof=0;
  for(int y=0;y<h;y++)for(int c=0;c<4;c++)if(d[(y*w+w-1)*4+c]!=old[y*w*4+c])edge++;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int i=(y*w+x)*4;
   if(d[i+3]==0&&(d[i]!=0||d[i+1]!=0||d[i+2]!=0))hidden++;
   if(y>=1240&&d[i+3]!=255)floor++;
   if(x>=2850&&x<3550&&y<1170&&d[i+3]!=0)gap++;
   if(x<2400&&y<100&&d[i+3]!=255)roof++;
  }
  if(edge+hidden+floor+gap+roof>0)throw new Exception("Validation: edge="+edge+", hidden="+hidden+", floor="+floor+", gap="+gap+", croppedBar="+roof);
  return "Right-to-left RGBA boundary errors=0 in all 1844 rows. Lower foreground fully opaque. Middle opening truly transparent. Bar facade fills upper-left crop; no roof silhouette. Hidden RGB errors=0.";
 }
}
'@
[VegasBarJoin]::FloorTone($data,$oldData,5120,1844)
[VegasBarJoin]::Quilt($data,$oldData,5120,1844)
$null=[CasinoLeftPixels]::Normalize($data)
Set-PixelData $final $data
$finalPath=Join-Path $outDir 'vegas-strip-baari-vasen-5120x1844.png'
$final.Save($finalPath,[System.Drawing.Imaging.ImageFormat]::Png)
$saved=[System.Drawing.Bitmap]::new($finalPath)
$report=[VegasBarJoin]::Verify((Pixel-Data $saved),$oldData,5120,1844)
$newSmall=Resize-Canvas $saved 1280 461;$oldSmall=Resize-Canvas $old 1280 461
$newSmall.Save((Join-Path $outDir 'baari-etuala-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$joined=[System.Drawing.Bitmap]::new(2560,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($joined);$g.Clear([System.Drawing.Color]::FromArgb(45,45,45));$g.DrawImageUnscaled($newSmall,0,0);$g.DrawImageUnscaled($oldSmall,1280,0);$g.Dispose()
$joined.Save((Join-Path $outDir 'liitos-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$joined.Dispose()
$close=[System.Drawing.Bitmap]::new(1200,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($close);$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.DrawImage($saved,[System.Drawing.Rectangle]::new(0,0,600,1844),[System.Drawing.Rectangle]::new(4520,0,600,1844),[System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($old,[System.Drawing.Rectangle]::new(600,0,600,1844),[System.Drawing.Rectangle]::new(0,0,600,1844),[System.Drawing.GraphicsUnit]::Pixel);$g.Dispose()
$close.Save((Join-Path $outDir 'liitos-lahikuva.png'),[System.Drawing.Imaging.ImageFormat]::Png);$close.Dispose()
$scene=[System.Drawing.Bitmap]::new(2560,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($scene)
foreach($name in @('01-yotaivas-5120x1844.png','02-hotellit-ja-rakennukset-5120x1844.png','03-kasvillisuus-5120x1844.png')){
 $layer=[System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot ('las-vegas-parallaksi-v2\'+$name)));$small=Resize-Canvas $layer 1280 461
 $g.DrawImageUnscaled($small,1280,0);$small.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX);$g.DrawImageUnscaled($small,0,0);$small.Dispose();$layer.Dispose()
}
$g.DrawImageUnscaled($newSmall,0,0);$g.DrawImageUnscaled($oldSmall,1280,0);$g.Dispose()
$scene.Save((Join-Path $outDir 'strip-parallakseilla-liitos-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$single=$scene.Clone([System.Drawing.Rectangle]::new(0,0,1280,461),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$single.Save((Join-Path $outDir 'baari-parallakseilla-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$single.Dispose();$scene.Dispose();$newSmall.Dispose();$oldSmall.Dispose();$saved.Dispose();$final.Dispose();$old.Dispose()
if((Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash -ne $oldHash){throw 'Adjacent casino source changed.'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vegas-strip-baari-vasen-generointiohje.txt') -Destination (Join-Path $outDir 'generointiohje.txt')
@"
Vegasin Strip: baaripala kasinon VASEMMALLE puolelle

Pelikuva: vegas-strip-baari-vasen-5120x1844.png, RGBA PNG.
Aseta tämän oikea reuna suoraan kuvan vegas-strip-kasino-ulkoa-v2-5120x1844.png vasempaan reunaan samoilla PPU-, pivot-, skaala- ja korkeusasetuksilla.
Vasemmalta oikealle: suuri baarijulkisivu (katto rajautuu pois), läpinäkyvä aukko Vegas-parallakseille, kasinon pääty ja julkisivun jatko vanhaan kasinopalaan.
Jalkakäytävä on kapea, reunakynnys matala ja asfalttia on reilusti. Kokonaiskävelyalue noin 35%, asfaltin alku noin y1410. Kasinon neonraidat, julkisivun vaakalistat ja kadun korkeudet on kohdistettu viereiseen kuvaan.
Taustaa ei ole leivottu etualaan. Parallaksiesikatselu käyttää aiempia Vegas-tasoja: uudessa vasemmassa palassa ne näkyvät peilattuina, vanhassa oikeassa palassa alkuperäisinä. Tämä on sommitteluesimerkki.
Vanhaa kasinopalaa ei muutettu. Unityn sceneä tai asetuksia ei muutettu.

Tekotapa: sisäänrakennettu image_gen, vasemmalle jatkaminen ja erillinen baarin suurennus. Generoitu lähtökuva $native sisältäen oikeanpuoleisen apuliuskan. Apuliuska poistettu, uusi pala skaalattu 5120 x 1844:ään. Julkisivun ja kadun korkeuskohdistus, lähes läpinäkyvän reunavärin siivous sekä kapea sauman sovitus tehty viennissä. 5120 x 1844 on vientikoko, ei generoinnin natiiviresoluutio.
"@ | Set-Content -LiteralPath (Join-Path $outDir 'LUE-MINUT.txt') -Encoding UTF8
@("Native=$native including guide strip",'Final=5120x1844 RGBA PNG',$report,"Adjacent source SHA256 unchanged: $oldHash") | Set-Content -LiteralPath (Join-Path $outDir 'tarkistus.txt') -Encoding UTF8
Write-Output $finalPath
Write-Output $report
