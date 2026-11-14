$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$work=Join-Path $PSScriptRoot 'vegas-baari-lava-tyo'
$stage=[System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-baari-sisatilat-lava\vegas-baari-sisatilat-lava-5120x1844.png'))
$raw=[System.Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-a42b3440-4acb-4982-a448-b8e4245dedd8.png')
$edit=Resize-Canvas $raw 2440 840
$raw.Dispose()
$data=Pixel-Data $stage;$patch=Pixel-Data $edit
Add-Type -TypeDefinition @'
using System;
public static class EmptyBarStage {
 static double S(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
 public static void Paste(byte[] d,byte[] p){
  for(int y=128;y<760;y++)for(int x=100;x<2280;x++){
   double a=S((x-100)/32.0)*S((2280-x)/32.0)*S((y-128)/40.0)*S((760-y)/24.0);
   int target=((y+128)*5120+x+2680)*4,source=(y*2440+x)*4;
   for(int c=0;c<4;c++)d[target+c]=(byte)Math.Round(d[target+c]*(1-a)+p[source+c]*a);
  }
 }
}
'@
[EmptyBarStage]::Paste($data,$patch)
Set-PixelData $stage $data
$stage.Save((Join-Path $work 'lava-tyhja-5120x1844.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$left=[System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-baari-sisatilat-vasen\vegas-baari-sisatilat-vasen-5120x1844.png'))
# Readable wide crop of the full back bar, spanning the existing join.
$bar=[System.Drawing.Bitmap]::new(4030,1400,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($bar);$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.DrawImage($left,[System.Drawing.Rectangle]::new(0,0,1370,1400),[System.Drawing.Rectangle]::new(3750,0,1370,1400),[System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($stage,[System.Drawing.Rectangle]::new(1370,0,2660,1400),[System.Drawing.Rectangle]::new(0,0,2660,1400),[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$bar.Save((Join-Path $work 'pullot-muokkausviite.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$small=Resize-Canvas $stage 1280 461;$small.Save((Join-Path $work 'lava-tyhja-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$small.Dispose();$bar.Dispose();$stage.Dispose();$left.Dispose();$edit.Dispose()
