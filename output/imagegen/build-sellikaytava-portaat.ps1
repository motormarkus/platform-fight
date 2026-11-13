$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$original=[System.Drawing.Bitmap]::FromFile((Join-Path $root 'vankilan-sellikaytava-kaksi-kerrosta-5120x1844.png'))
$edit=[System.Drawing.Bitmap]::FromFile('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-771478bd-98cd-491c-8190-e60f35e3ef60.png')
$patch=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$pg=[System.Drawing.Graphics]::FromImage($patch)
$pg.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$pg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[System.Drawing.Imaging.ImageAttributes]::new()
$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
$sx=5120.0/2089.0
$sy=1844.0/753.0
$pg.DrawImage($edit,[System.Drawing.RectangleF]::new([single](280*$sx),[single](210*$sy),[single](512*$sx),[single](512*$sy)),[System.Drawing.RectangleF]::new(0,0,$edit.Width,$edit.Height),[System.Drawing.GraphicsUnit]::Pixel)
$pg.Dispose()
$rect=[System.Drawing.Rectangle]::new(0,0,5120,1844)
$arrays=@()
foreach($im in @($original,$patch)){
 $bd=$im.LockBits($rect,[System.Drawing.Imaging.ImageLockMode]::ReadOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $bytes=[byte[]]::new($bd.Stride*$im.Height)
 [System.Runtime.InteropServices.Marshal]::Copy($bd.Scan0,$bytes,0,$bytes.Length)
 $im.UnlockBits($bd)
 $arrays+=,$bytes
}
Add-Type -TypeDefinition @'
using System;
public static class StairLocalPatch {
 static readonly double[] px={18,135,406,454,454,403,34,29,18};
 static readonly double[] py={494,494,220,167,24,24,376,447,448};
 public static double Weight(double x,double y){
  bool inside=false;double distance=1e20;
  for(int i=0,j=px.Length-1;i<px.Length;j=i++){
   if(((py[i]>y)!=(py[j]>y))&&(x<(px[j]-px[i])*(y-py[i])/(py[j]-py[i])+px[i]))inside=!inside;
   double dx=px[j]-px[i],dy=py[j]-py[i];
   double t=Math.Max(0,Math.Min(1,((x-px[i])*dx+(y-py[i])*dy)/(dx*dx+dy*dy)));
   double ex=x-px[i]-t*dx,ey=y-py[i]-t*dy;
   distance=Math.Min(distance,Math.Sqrt(ex*ex+ey*ey));
  }
  if(!inside)return 0;
  double a=Math.Min(1,distance/5.0);return a*a*(3-2*a);
 }
 public static string Apply(byte[] original,byte[] patch,int w,int h){
  int changed=0;int[] bounds={w,h,0,0};
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   double cx=(x+0.5)*2089.0/w-280,cy=(y+0.5)*753.0/h-210;
   if(cx<18||cx>454||cy<24||cy>494)continue;
   double a=Weight(cx,cy);if(a<=0)continue;
   int p=(y*w+x)*4;bool differs=false;
   for(int c=0;c<3;c++){
    byte b=(byte)Math.Max(0,Math.Min(255,Math.Round(original[p+c]*(1-a)+patch[p+c]*a)));
    if(b!=original[p+c])differs=true;original[p+c]=b;
   }
   if(differs){changed++;bounds[0]=Math.Min(bounds[0],x);bounds[1]=Math.Min(bounds[1],y);bounds[2]=Math.Max(bounds[2],x);bounds[3]=Math.Max(bounds[3],y);}
  }
  return "Changed pixels="+changed+"; local bounding box="+String.Join(",",bounds)+"; only staircase polygon modified.";
 }
 public static string Verify(byte[] original,byte[] saved,int w,int h){
  int outside=0,changed=0;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int p=(y*w+x)*4;bool diff=false;
   for(int c=0;c<4;c++)if(original[p+c]!=saved[p+c])diff=true;
   if(!diff)continue;changed++;
   double cx=(x+0.5)*2089.0/w-280,cy=(y+0.5)*753.0/h-210;
   if(Weight(cx,cy)<=0)outside++;
  }
  if(outside!=0)throw new Exception("Pixels outside allowed stair area changed: "+outside);
  return "Saved PNG validation: "+w+"x"+h+"; changed pixels="+changed+"; changed pixels outside staircase mask="+outside;
 }
}
'@
$originalBytes=[byte[]]$arrays[0].Clone()
[StairLocalPatch]::Apply($arrays[0],$arrays[1],5120,1844)
$bd=$original.LockBits($rect,[System.Drawing.Imaging.ImageLockMode]::WriteOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
[System.Runtime.InteropServices.Marshal]::Copy($arrays[0],0,$bd.Scan0,$arrays[0].Length)
$original.UnlockBits($bd)
$out=Join-Path $root 'vankilan-sellikaytava-portaat-sivulta-5120x1844.png'
$original.Save($out,[System.Drawing.Imaging.ImageFormat]::Png)
$saved=[System.Drawing.Bitmap]::FromFile($out)
$bd=$saved.LockBits($rect,[System.Drawing.Imaging.ImageLockMode]::ReadOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$savedBytes=[byte[]]::new($bd.Stride*$saved.Height)
[System.Runtime.InteropServices.Marshal]::Copy($bd.Scan0,$savedBytes,0,$savedBytes.Length)
$saved.UnlockBits($bd)
$report=[StairLocalPatch]::Verify($originalBytes,$savedBytes,5120,1844)
$report
Set-Content -LiteralPath (Join-Path $root 'sellikaytava-portaat-sivulta-tarkistus.txt') -Value $report
$preview=[System.Drawing.Bitmap]::new(2089,753)
$g=[System.Drawing.Graphics]::FromImage($preview)
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.DrawImage($original,[System.Drawing.Rectangle]::new(0,0,2089,753),0,0,5120,1844,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$preview.Save((Join-Path $root 'sellikaytava-portaat-sivulta-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose();$preview.Dispose();$attrs.Dispose();$saved.Dispose();$original.Dispose();$edit.Dispose();$patch.Dispose()
