$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'WorksiteDetail.cs') -ReferencedAssemblies System.Drawing
Add-Type -TypeDefinition @'
using System;
public static class CakeBounds {
 public static int[] Bounds(byte[] d,int w,int h){int x0=w,y0=h,x1=0,y1=0;for(int y=0;y<h;y++)for(int x=0;x<w;x++){if(d[(y*w+x)*4+3]>0){x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);}}return new[]{x0,y0,x1-x0+1,y1-y0+1};}
 public static long[] Alpha(byte[] d){long clear=0,solid=0;for(int p=3;p<d.Length;p+=4){if(d[p]==0)clear++;if(d[p]==255)solid++;}return new[]{clear,solid};}
}
'@
$folder=Join-Path $PSScriptRoot 'vegas-haakakku-sotku/kevyet-versiot'
$raw='C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-4c8fc8a7-9699-4421-98a9-9a94a58469e8.png'
Copy-Item -LiteralPath $raw -Destination (Join-Path $folder '01-kermalammikko-natiivi.png')
$w=0;$h=0;$d=[WorksiteDetail]::Load($raw,[ref]$w,[ref]$h);[WorksiteDetail]::Clean($d)
$b=[CakeBounds]::Bounds($d,$w,$h);$im=[WorksiteDetail]::Image($d,$w,$h)
$crop=$im.Clone([Drawing.Rectangle]::new($b[0],$b[1],$b[2],$b[3]),[Drawing.Imaging.PixelFormat]::Format32bppArgb);$im.Dispose()
$scale=[Math]::Min(992.0/$b[2],224.0/$b[3]);$dw=[int][Math]::Round($b[2]*$scale);$dh=[int][Math]::Round($b[3]*$scale)
$resized=[WorksiteDetail]::Resize([WorksiteDetail]::Read($crop),$crop.Width,$crop.Height,$dw,$dh);$crop.Dispose()
$canvas=[byte[]]::new(1024*256*4);$x=[int][Math]::Floor((1024-$dw)/2);$y=[int][Math]::Floor((256-$dh)/2)
for($row=0;$row -lt $dh;$row++){[Buffer]::BlockCopy($resized,$row*$dw*4,$canvas,(($y+$row)*1024+$x)*4,$dw*4)}
$final=Join-Path $folder '01-kermalammikko-1024x256.png';[WorksiteDetail]::Save($canvas,1024,256,$final)
$preview=[Drawing.Bitmap]::new(1024,256,[Drawing.Imaging.PixelFormat]::Format32bppArgb);$g=[Drawing.Graphics]::FromImage($preview);$g.Clear([Drawing.Color]::FromArgb(255,75,75,75));$brush=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(255,95,95,95))
for($yy=0;$yy -lt 256;$yy+=16){for($xx=0;$xx -lt 1024;$xx+=16){if((($xx/16+$yy/16)%2) -eq 0){$g.FillRectangle($brush,$xx,$yy,16,16)}}}
$sprite=[WorksiteDetail]::Image($canvas,1024,256);$g.DrawImageUnscaled($sprite,0,0);$sprite.Dispose();$brush.Dispose();$g.Dispose();$preview.Save((Join-Path $folder '01-kermalammikko-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$preview.Dispose()
$a=[CakeBounds]::Alpha($canvas)
[pscustomobject]@{width=1024;height=256;spriteWidth=$dw;spriteHeight=$dh;transparentPixels=$a[0];opaquePixels=$a[1];bytes=(Get-Item -LiteralPath $final).Length}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $folder '01-kermalammikko-tarkistus.json') -Encoding utf8
Get-Content -LiteralPath (Join-Path $folder '01-kermalammikko-tarkistus.json')

