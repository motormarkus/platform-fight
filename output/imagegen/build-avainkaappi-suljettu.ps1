$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$raw='C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-a7f5c767-deea-4c88-98f3-51fb97e29226.png'
$src=[System.Drawing.Bitmap]::FromFile($raw)
Add-Type -TypeDefinition @'
using System;
public static class KeyCabinetAlpha {
 public static int[] Clean(byte[] d,int w,int h){
  int l=w,t=h,r=-1,b=-1;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int i=(y*w+x)*4;
   if(d[i+3]<=16){d[i]=d[i+1]=d[i+2]=d[i+3]=0;continue;}
   if(d[i+3]>=240)d[i+3]=255;
   l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);
  }
  return new int[]{l,t,r-l+1,b-t+1};
 }
}
'@
function Normalize-Alpha([System.Drawing.Bitmap]$im){
 $rect=[System.Drawing.Rectangle]::new(0,0,$im.Width,$im.Height)
 $bd=$im.LockBits($rect,[System.Drawing.Imaging.ImageLockMode]::ReadWrite,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $bytes=[byte[]]::new($bd.Stride*$im.Height)
 [System.Runtime.InteropServices.Marshal]::Copy($bd.Scan0,$bytes,0,$bytes.Length)
 $bounds=[KeyCabinetAlpha]::Clean($bytes,$im.Width,$im.Height)
 [System.Runtime.InteropServices.Marshal]::Copy($bytes,0,$bd.Scan0,$bytes.Length)
 $im.UnlockBits($bd)
 return ,$bounds
}
$bounds=Normalize-Alpha $src
$src.Save((Join-Path $root 'avainkaappi-suljettu-alkuperainen.png'),[System.Drawing.Imaging.ImageFormat]::Png)
function Make-Sprite([int]$w,[int]$h,[int]$padding){
 $im=[System.Drawing.Bitmap]::new($w+2*$padding,$h+2*$padding,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[System.Drawing.Graphics]::FromImage($im)
 $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
 $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
 $a=[System.Drawing.Imaging.ImageAttributes]::new();$a.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
 $g.DrawImage($src,[System.Drawing.Rectangle]::new($padding,$padding,$w,$h),$bounds[0],$bounds[1],$bounds[2],$bounds[3],[System.Drawing.GraphicsUnit]::Pixel,$a)
 $g.Dispose();$a.Dispose();$null=Normalize-Alpha $im
 return $im
}
$sprite=Make-Sprite $bounds[2] $bounds[3] 16
$sprite.Save((Join-Path $root 'avainkaappi-suljettu-sprite.png'),[System.Drawing.Imaging.ImageFormat]::Png)
# Original guard-room cabinet body is about 246 x 209 native pixels.
# Room artwork scales 2087 x 754 to 5120 x 1844.
$gameW=[int][Math]::Round(246*5120.0/2087)
$gameH=[int][Math]::Round(209*1844.0/754)
$game=Make-Sprite $gameW $gameH 12
$game.Save((Join-Path $root 'avainkaappi-suljettu-vartiohuoneen-mittakaava.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview=[System.Drawing.Bitmap]::new($game.Width,$game.Height)
$pg=[System.Drawing.Graphics]::FromImage($preview)
$pg.Clear([System.Drawing.Color]::FromArgb(75,88,95))
$pg.DrawImageUnscaled($game,0,0)
$preview.Save((Join-Path $root 'avainkaappi-suljettu-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
foreach($im in @($sprite,$game)){
 for($x=0;$x -lt $im.Width;$x++){if($im.GetPixel($x,0).A -ne 0 -or $im.GetPixel($x,$im.Height-1).A -ne 0){throw 'Top or bottom is not transparent'}}
 for($y=0;$y -lt $im.Height;$y++){if($im.GetPixel(0,$y).A -ne 0 -or $im.GetPixel($im.Width-1,$y).A -ne 0){throw 'Side is not transparent'}}
 if($im.GetPixel([int]($im.Width/2),[int]($im.Height/2)).A -ne 255){throw 'Door is not opaque'}
}
$report=@("Generated canvas: $($src.Width)x$($src.Height)","Generated object bounds: $($bounds -join ',')","High resolution sprite: $($sprite.Width)x$($sprite.Height), 16 px transparent padding.","Room-scale sprite: $($game.Width)x$($game.Height); cabinet $($gameW)x$($gameH), 12 px transparent padding.",'All outer edge pixels alpha=0; closed door centre alpha=255.','Original room artwork unchanged.')
$report
Set-Content -LiteralPath (Join-Path $root 'avainkaappi-suljettu-tarkistus.txt') -Value $report
Add-Content -LiteralPath (Join-Path $root 'avainkaappi-suljettu-prompt.txt') -Value (@('','OUTPUT','Raw source: '+$raw)+$report)
$pg.Dispose();$preview.Dispose();$game.Dispose();$sprite.Dispose();$src.Dispose()
