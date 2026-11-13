$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
Add-Type -TypeDefinition @'
using System;
public static class RoofElevatorPatch {
 public static void Normalize(byte[] d){
  for(int i=0;i<d.Length;i+=4){
   if(d[i+3]<=16)d[i]=d[i+1]=d[i+2]=d[i+3]=0;
   else if(d[i+3]>=240)d[i+3]=255;
  }
 }
 static double Rect(double x,double y,double l,double t,double r,double b){
  double d=Math.Min(Math.Min(x-l,r-x),Math.Min(y-t,b-y));
  if(d<=0)return 0;double v=Math.Min(1,d/6.0);return v*v*(3-2*v);
 }
 public static double Weight(double x,double y){
  return Math.Max(Rect(x,y,20,144,415,459),Rect(x,y,108,48,323,160));
 }
 public static int Apply(byte[] image,byte[] patch,int w,int h){
  int changed=0;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   double cx=(x+0.5)*2089.0/w-130,cy=(y+0.5)*753.0/h-110;
   if(cx<20||cx>415||cy<48||cy>459)continue;
   double a=Weight(cx,cy);if(a<=0)continue;
   int p=(y*w+x)*4;bool diff=false;
   for(int c=0;c<4;c++){
    byte v=(byte)Math.Max(0,Math.Min(255,Math.Round(image[p+c]*(1-a)+patch[p+c]*a)));
    if(v!=image[p+c])diff=true;image[p+c]=v;
   }
   if(image[p+3]<=16)image[p]=image[p+1]=image[p+2]=image[p+3]=0;
   else if(image[p+3]>=240)image[p+3]=255;
   if(diff)changed++;
  }
  return changed;
 }
 public static string Verify(byte[] original,byte[] saved,int w,int h){
  int outside=0,changed=0;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int p=(y*w+x)*4;bool diff=false;
   for(int c=0;c<4;c++)if(original[p+c]!=saved[p+c])diff=true;
   if(!diff)continue;changed++;
   double cx=(x+0.5)*2089.0/w-130,cy=(y+0.5)*753.0/h-110;
   if(Weight(cx,cy)<=0)outside++;
  }
  if(outside!=0)throw new Exception("Changes outside elevator/machine area: "+outside);
  return "Saved PNG "+w+"x"+h+"; changed pixels="+changed+"; changes outside elevator/machine mask="+outside;
 }
}
'@
function Read-Pixels([System.Drawing.Bitmap]$im){
 $r=[System.Drawing.Rectangle]::new(0,0,$im.Width,$im.Height)
 $bd=$im.LockBits($r,[System.Drawing.Imaging.ImageLockMode]::ReadOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $d=[byte[]]::new($bd.Stride*$im.Height)
 [System.Runtime.InteropServices.Marshal]::Copy($bd.Scan0,$d,0,$d.Length)
 $im.UnlockBits($bd)
 return ,$d
}
function Write-Pixels([System.Drawing.Bitmap]$im,[byte[]]$d){
 $r=[System.Drawing.Rectangle]::new(0,0,$im.Width,$im.Height)
 $bd=$im.LockBits($r,[System.Drawing.Imaging.ImageLockMode]::WriteOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 [System.Runtime.InteropServices.Marshal]::Copy($d,0,$bd.Scan0,$d.Length)
 $im.UnlockBits($bd)
}
$original=[System.Drawing.Bitmap]::FromFile((Join-Path $root 'vankilan-katto-vasen-kauempaa-5120x1844.png'))
$edit=[System.Drawing.Bitmap]::FromFile('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-6f60ab8b-068e-4a5d-a01e-8d1bd00982e9.png')
$editBytes=Read-Pixels $edit
[RoofElevatorPatch]::Normalize($editBytes)
Write-Pixels $edit $editBytes
$edit.Save((Join-Path $root 'katto-hissi-levennetty-paikka.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$patch=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($patch)
$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$sx=5120.0/2089;$sy=1844.0/753
$g.DrawImage($edit,[System.Drawing.RectangleF]::new([single](130*$sx),[single](110*$sy),[single](480*$sx),[single](480*$sy)),[System.Drawing.RectangleF]::new(0,0,$edit.Width,$edit.Height),[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose()
$originalBytes=Read-Pixels $original
$resultBytes=[byte[]]$originalBytes.Clone()
$patchBytes=Read-Pixels $patch
[RoofElevatorPatch]::Apply($resultBytes,$patchBytes,5120,1844)
Write-Pixels $original $resultBytes
$out=Join-Path $root 'vankilan-katto-vasen-levea-hissi-5120x1844.png'
$original.Save($out,[System.Drawing.Imaging.ImageFormat]::Png)
$saved=[System.Drawing.Bitmap]::FromFile($out)
$savedBytes=Read-Pixels $saved
$report=[RoofElevatorPatch]::Verify($originalBytes,$savedBytes,5120,1844)
$report
Set-Content -LiteralPath (Join-Path $root 'vankilan-katto-levea-hissi-tarkistus.txt') -Value $report
$attrs=[System.Drawing.Imaging.ImageAttributes]::new()
$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
$reference=[System.Drawing.Bitmap]::new(2089,753,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$rg=[System.Drawing.Graphics]::FromImage($reference)
$rg.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$rg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$rg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$rg.DrawImage($saved,[System.Drawing.Rectangle]::new(0,0,2089,753),0,0,5120,1844,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$reference.Save((Join-Path $root 'vankilan-katto-vasen-levea-hissi-tyopohja.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview=[System.Drawing.Bitmap]::new(1280,461)
$pg=[System.Drawing.Graphics]::FromImage($preview)
$pg.Clear([System.Drawing.Color]::FromArgb(45,90,110))
$pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$pg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$pg.DrawImage($saved,[System.Drawing.Rectangle]::new(0,0,1280,461),0,0,5120,1844,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$preview.Save((Join-Path $root 'vankilan-katto-vasen-levea-hissi-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$attrs.Dispose();$pg.Dispose();$preview.Dispose();$rg.Dispose();$reference.Dispose();$saved.Dispose();$patch.Dispose();$edit.Dispose();$original.Dispose()
