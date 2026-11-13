$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$doorDir=Join-Path $PSScriptRoot 'kasino-paaovet'
[void][System.IO.Directory]::CreateDirectory($doorDir)
$sourcePath=Join-Path $PSScriptRoot 'kasino-lattiajatko\03-etuala-lattiajatko-5120x1844.png'
$originalHash=(Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
$source=[System.Drawing.Bitmap]::new($sourcePath)
$raw=[System.Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-0e666d37-f0b1-40c7-80de-9e892a6f337e.png')
$native='{0}x{1}' -f $raw.Width,$raw.Height
$scaled=Resize-Canvas $raw 5120 1844
$raw.Dispose()
$registered=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($registered)
$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[System.Drawing.Imaging.ImageAttributes]::new();$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
# Match the generated gold trim to the existing floor without changing the canvas.
$g.DrawImage($scaled,[System.Drawing.Rectangle]::new(0,0,5120,1224),[single]0,[single]0,[single]5120,[single]1232,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$g.DrawImage($scaled,[System.Drawing.Rectangle]::new(0,1224,5120,620),[single]0,[single]1232,[single]5120,[single]612,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$attrs.Dispose();$g.Dispose();$scaled.Dispose()
$null=Clean-Alpha $registered
$doorData=Pixel-Data $registered
$sourceData=Pixel-Data $source
Add-Type -TypeDefinition @'
using System;
public static class CasinoEntranceJoin {
 static double Smooth(double v){v=Math.Max(0,Math.Min(1,v));return v*v*(3-2*v);}
 static byte Byte(double v){return (byte)Math.Max(0,Math.Min(255,Math.Round(v)));}
 public static byte[] Compose(byte[] door,byte[] floor,int w,int h){
  byte[] output=new byte[door.Length];
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int i=(y*w+x)*4,j=(y*w+w-1-x)*4;
   // A reflected copy of the adjacent floor makes the entire right-hand join exact.
   for(int c=0;c<4;c++)output[i+c]=floor[j+c];
   double weight=1-Smooth((x-1500)/250.0);
   if(y>1370)weight*=1-Smooth((y-1370)/474.0);
   if(weight<=0)continue;
   double a=door[i+3]/255.0*weight,b=output[i+3]/255.0;
   double alpha=a+b*(1-a);
   for(int c=0;c<3;c++)output[i+c]=alpha>0?Byte((door[i+c]*a+output[i+c]*b*(1-a))/alpha):(byte)0;
   output[i+3]=Byte(alpha*255);
  }
  return output;
 }
 public static string Check(byte[] d,byte[] s,int w,int h){
  int edge=0,clear=0,level=0,floor=0;
  for(int y=0;y<h;y++)for(int c=0;c<4;c++)if(d[(y*w+w-1)*4+c]!=s[(y*w)*4+c])edge++;
  for(int x=1750;x<w;x++){
   int first=-1;
   for(int y=0;y<h;y++){
    int i=(y*w+x)*4;
    if(y<1195)for(int c=0;c<4;c++)if(d[i+c]!=0)clear++;
    if(first<0&&d[i+3]>0)first=y;
    if(y>=1224&&d[i+3]!=255)floor++;
   }
   if(first!=1195)level++;
  }
  if(edge+clear+level+floor>0)throw new Exception("Validation: "+edge+","+clear+","+level+","+floor);
  return "Right-edge RGBA differences: 0 across all 1844 rows. Open upper area is fully transparent. Floor edge is y=1195 throughout open section; lower floor is opaque.";
 }
}
'@
$data=[CasinoEntranceJoin]::Compose($doorData,$sourceData,5120,1844)
Set-PixelData $registered $data
$finalPath=Join-Path $doorDir '03-etuala-paaovet-5120x1844.png'
$registered.Save($finalPath,[System.Drawing.Imaging.ImageFormat]::Png)
$saved=[System.Drawing.Bitmap]::new($finalPath)
$report=[CasinoEntranceJoin]::Check((Pixel-Data $saved),$sourceData,5120,1844)
$saved.Dispose()
function Door-Checker([System.Drawing.Bitmap]$im){
 $result=[System.Drawing.Bitmap]::new($im.Width,$im.Height,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[System.Drawing.Graphics]::FromImage($result)
 $brushes=@([System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(46,46,46)),[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(57,57,57)))
 for($y=0;$y -lt $im.Height;$y+=32){for($x=0;$x -lt $im.Width;$x+=32){$idx=([int][Math]::Floor($x/32)+[int][Math]::Floor($y/32))%2;$g.FillRectangle($brushes[$idx],$x,$y,32,32)}}
 $g.DrawImageUnscaled($im,0,0);$g.Dispose();$brushes[0].Dispose();$brushes[1].Dispose();return $result
}
$small=Resize-Canvas $registered 1280 461
$checked=Door-Checker $small
$checked.Save((Join-Path $doorDir 'paaovet-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$checked.Dispose();$small.Dispose()
$joined=[System.Drawing.Bitmap]::new(2560,461,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($joined);$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($registered,[System.Drawing.Rectangle]::new(0,0,1280,461));$g.DrawImage($source,[System.Drawing.Rectangle]::new(1280,0,1280,461));$g.Dispose()
$checked=Door-Checker $joined
$checked.Save((Join-Path $doorDir 'liitos-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$checked.Dispose();$joined.Dispose()
$close=[System.Drawing.Bitmap]::new(1024,724,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($close)
$g.DrawImage($registered,[System.Drawing.Rectangle]::new(0,0,512,724),[System.Drawing.Rectangle]::new(4608,1120,512,724),[System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($source,[System.Drawing.Rectangle]::new(512,0,512,724),[System.Drawing.Rectangle]::new(0,1120,512,724),[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$checked=Door-Checker $close
$checked.Save((Join-Path $doorDir 'liitos-lahikuva.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$checked.Dispose();$close.Dispose();$registered.Dispose();$source.Dispose()
if((Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash -ne $originalHash){throw 'Adjacent image changed.'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'kasino-paaovet-generointiohje.txt') -Destination (Join-Path $doorDir 'generointiohje.txt')
@"
Kasinon vasen pääty ja pääovet

Pelikuva: 03-etuala-paaovet-5120x1844.png. RGBA PNG, 5120 x 1844.
Aseta tämä lattiajatkopalan VASEMMALLE puolelle: pääovet -> lattiajatko -> baari -> baccarat/ruletti -> hissipala.
Pidä sama PPU, skaala, pivot ja korkeus. Älä rajaa läpinäkyviä marginaaleja.
Vasemmalla on sulkeva seinä ja leveät pääovet. Avoin yläosa on läpinäkyvä parallaksia varten. Lattian yläreuna avoimessa osassa on y=1195.
Esikatselujen ruudukko on vain esikatseluissa.
Tekotapa: sisäänrakennettu image_gen, alkuperäinen $native; sen jälkeen skaalaus, lattian korkeuden kohdistus sekä peilattu lattia aiemmasta lattiajatkosta. 5120 x 1844 on vientikoko, ei generoinnin natiivikoko.
Edellisiä kuvia ei muutettu.
"@ | Set-Content -LiteralPath (Join-Path $doorDir 'LUE-MINUT.txt') -Encoding UTF8
@("Generated native size: $native",'Final: 5120x1844 RGBA PNG',$report,("Reference SHA256 unchanged: $originalHash")) | Set-Content -LiteralPath (Join-Path $doorDir 'tarkistus.txt') -Encoding UTF8
Write-Output $finalPath
Write-Output $report
