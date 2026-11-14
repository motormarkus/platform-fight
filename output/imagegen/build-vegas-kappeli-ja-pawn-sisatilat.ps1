$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
Add-Type -TypeDefinition @'
using System;
public static class VegasInteriorPair {
 static double S(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
 public static void Join(byte[] d,byte[] old,bool chapel){
  const int w=5120,h=1844;
  for(int x=0;x<w;x++){
   double start=(chapel?1320:1340)+80*S((x-4400)/719.0);
   for(int y=(int)start;y<h;y++){
    double a=S((y-start)/112.0);int i=(y*w+x)*4,j=(y*w+w-1-x)*4;
    for(int c=0;c<4;c++)d[i+c]=(byte)Math.Round(d[i+c]*(1-a)+old[j+c]*a);
   }
  }
  int collar=chapel?512:80,keep=chapel?128:8;
  for(int y=0;y<h;y++)for(int x=0;x<collar;x++){
   double a=1-S((x-keep)/(double)(collar-keep-1));int i=(y*w+x)*4,j=(y*w+w-1-x)*4;
   for(int c=0;c<4;c++)d[i+c]=(byte)Math.Round(d[i+c]*(1-a)+old[j+c]*a);
  }
  for(int y=0;y<h;y++)for(int c=0;c<4;c++)d[y*w*4+c]=old[(y*w+w-1)*4+c];
 }
 public static string Check(byte[] d,byte[] old,bool chapel){
  const int w=5120,h=1844;int edge=0,rightAlpha=0,floor=0,hidden=0,leftTransparent=0;
  for(int y=0;y<h;y++)for(int c=0;c<4;c++)if(d[y*w*4+c]!=old[(y*w+w-1)*4+c])edge++;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int i=(y*w+x)*4;
   if(d[i+3]!=255)rightAlpha++;
   if(old[i+3]==0)leftTransparent++;
   if(y>=1400&&(d[i+3]!=255||old[i+3]!=255))floor++;
   if(old[i+3]==0&&(old[i]!=0||old[i+1]!=0||old[i+2]!=0))hidden++;
  }
  if(edge+rightAlpha+floor+hidden>0)throw new Exception("Edge="+edge+", rightAlpha="+rightAlpha+", floor="+floor+", hiddenRGB="+hidden);
  if(chapel&&leftTransparent<400000)throw new Exception("Missing clear chapel windows.");
  if(!chapel&&leftTransparent>0)throw new Exception("Unexpected transparency in pawnshop.");
  return "Both panels 5120x1844. Joining edge differences=0 in all 1844 rows and all RGBA channels. Right room and both floors opaque. Fully clear pixels in left="+leftTransparent+". Hidden RGB=0.";
 }
 public static void RepairPawn(byte[] d,byte[] old,byte[] repair){
  for(int y=0;y<1110;y++){
   double[] tint=new double[3];int n=0;
   for(int yy=Math.Max(0,y-12);yy<=y+12;yy+=4)for(int xx=400;xx<480;xx+=8){
    for(int c=0;c<3;c++)tint[c]+=old[(yy*5120+4608+xx)*4+c]-repair[(yy*1024+xx)*4+c];n++;
   }
   for(int c=0;c<3;c++)tint[c]/=n;
   for(int x=1;x<175;x++){
    double f=S(x/10.0)*S((174-x)/30.0)*S((1110-y)/25.0);
    int i=(y*5120+x)*4,j=(y*1024+x+512)*4;
    for(int c=0;c<3;c++){double v=Math.Max(0,Math.Min(255,repair[j+c]+tint[c]));d[i+c]=(byte)Math.Round(d[i+c]*(1-f)+v*f);}
   }
  }
 }
}
'@
function Build-Room([string]$slug,[bool]$chapel,[int[]]$from,[int[]]$to){
 $dir=Join-Path $PSScriptRoot ($slug+'-sisatilat-v2');$work=Join-Path $PSScriptRoot ($slug+'-sisa-tyo')
 $leftPath=Join-Path $dir ($slug+'-vasen-5120x1844.png');$hash=(Get-FileHash -LiteralPath $leftPath -Algorithm SHA256).Hash
 $left=[Drawing.Bitmap]::new($leftPath);$raw=[Drawing.Bitmap]::new((Join-Path $work 'oikea-raaka-5120x1844.png'))
 $right=[Drawing.Bitmap]::new(5120,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[Drawing.Graphics]::FromImage($right);$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy;$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
 $attr=[Drawing.Imaging.ImageAttributes]::new();$attr.SetWrapMode([Drawing.Drawing2D.WrapMode]::TileFlipXY)
 for($k=0;$k -lt $from.Count-1;$k++){
  $g.DrawImage($raw,[Drawing.Rectangle]::new(0,$to[$k],5120,($to[$k+1]-$to[$k])),[single]0,[single]$from[$k],[single]5120,[single]($from[$k+1]-$from[$k]),[Drawing.GraphicsUnit]::Pixel,$attr)
 }
 $attr.Dispose();$g.Dispose();$raw.Dispose()
 $d=Pixel-Data $right;$a=Pixel-Data $left
 [VegasInteriorPair]::Join($d,$a,$chapel)
 if(-not $chapel){
  $repairRaw=[Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-8ccb8652-309b-4ba4-8e83-56146af591b9.png')
  $repair=Resize-Canvas $repairRaw 1024 1844
  [VegasInteriorPair]::RepairPawn($d,$a,(Pixel-Data $repair))
  $repair.Dispose();$repairRaw.Dispose()
 }
 Set-PixelData $right $d
 $rightPath=Join-Path $dir ($slug+'-oikea-5120x1844.png')
 $right.Save($rightPath,[Drawing.Imaging.ImageFormat]::Png)
 $report=[VegasInteriorPair]::Check($d,$a,$chapel)
 if($chapel){
  foreach($offset in @(0,660)){foreach($xy in @(@(710,180),@(820,180),@(710,280),@(820,280))){
   $px=[int][Math]::Round(($xy[0]+$offset)*5120.0/2089);$py=[int][Math]::Round($xy[1]*1844.0/753)
   if($left.GetPixel($px,$py).A -ne 0){throw ('Window test failed at '+$px+','+$py)}
  }}
 }
 $ls=Resize-Canvas $left 1280 461;$rs=Resize-Canvas $right 1280 461
 $rs.Save((Join-Path $dir 'oikea-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
 $pair=[Drawing.Bitmap]::new(2560,461,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[Drawing.Graphics]::FromImage($pair);$g.Clear([Drawing.Color]::FromArgb(58,69,85));$g.DrawImageUnscaled($ls,0,0);$g.DrawImageUnscaled($rs,1280,0);$g.Dispose()
 $pair.Save((Join-Path $dir 'yhdistetty-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$pair.Dispose()
 $close=[Drawing.Bitmap]::new(1024,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[Drawing.Graphics]::FromImage($close)
 $g.DrawImage($left,[Drawing.Rectangle]::new(0,0,512,1844),[Drawing.Rectangle]::new(4608,0,512,1844),[Drawing.GraphicsUnit]::Pixel)
 $g.DrawImage($right,[Drawing.Rectangle]::new(512,0,512,1844),[Drawing.Rectangle]::new(0,0,512,1844),[Drawing.GraphicsUnit]::Pixel)
 $g.Dispose();$close.Save((Join-Path $dir 'liitos-lahikuva.png'),[Drawing.Imaging.ImageFormat]::Png);$close.Dispose()
 if($chapel){
  $check=[Drawing.Bitmap]::new(1280,461,[Drawing.Imaging.PixelFormat]::Format32bppArgb);$g=[Drawing.Graphics]::FromImage($check)
  $light=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(140,154,172));$dark=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(72,90,116))
  for($y=0;$y -lt 461;$y+=20){for($x=0;$x -lt 1280;$x+=20){$brush=if((($x/20)+($y/20))%2 -eq 0){$light}else{$dark};$g.FillRectangle($brush,$x,$y,20,20)}}
  $g.DrawImageUnscaled($ls,0,0);$g.Dispose();$light.Dispose();$dark.Dispose()
  $check.Save((Join-Path $dir 'ikkunoiden-lapinakyvyys-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$check.Dispose()
 }
 $ls.Dispose();$rs.Dispose();$left.Dispose();$right.Dispose()
 if((Get-FileHash -LiteralPath $leftPath -Algorithm SHA256).Hash -ne $hash){throw 'Left source changed during export.'}
 $nativeLeft=(Get-Content -LiteralPath (Join-Path $work 'vasen-v2-natiivikoko.txt') -Raw).Trim()
 $nativeRight=(Get-Content -LiteralPath (Join-Path $work 'oikea-natiivikoko.txt') -Raw).Trim()
 @($report,"Left native=$nativeLeft; right native=$nativeRight including guide","Left SHA256 unchanged=$hash") | Set-Content -LiteralPath (Join-Path $dir 'tarkistus.txt') -Encoding UTF8
 Write-Output $slug;Write-Output $report
}
Build-Room 'vegas-kappeli' $true @(0,20,1043,1058,1072,1089,1120,1219,1253,1300,1844) @(0,18,974,987,1001,1010,1040,1126,1152,1207,1844)
Build-Room 'vegas-panttilainaamo' $false @(0,173,813,850,874,1116,1132,1154,1183,1272,1307,1336,1844) @(0,163,756,786,809,1050,1076,1089,1107,1184,1225,1251,1844)
