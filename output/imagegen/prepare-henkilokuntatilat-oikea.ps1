$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$raw=[System.Drawing.Bitmap]::FromFile('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-80e5fb58-139f-48fb-9e22-7582d04245ed.png')
$repair=[System.Drawing.Bitmap]::FromFile('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-c73c6188-54f4-410c-8298-161d8d01c625.png')
$patch=[System.Drawing.Bitmap]::new($raw.Width,$raw.Height,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($patch)
$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[System.Drawing.Imaging.ImageAttributes]::new()
$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
$g.DrawImage($repair,[System.Drawing.Rectangle]::new(100,340,180,360),0,0,$repair.Width,$repair.Height,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$g.Dispose();$attrs.Dispose()
$rect=[System.Drawing.Rectangle]::new(0,0,$raw.Width,$raw.Height)
$arrays=@()
foreach($im in @($raw,$patch)){
 $bd=$im.LockBits($rect,[System.Drawing.Imaging.ImageLockMode]::ReadOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $bytes=[byte[]]::new($bd.Stride*$im.Height)
 [System.Runtime.InteropServices.Marshal]::Copy($bd.Scan0,$bytes,0,$bytes.Length)
 $im.UnlockBits($bd);$arrays+=,$bytes
}
Add-Type -TypeDefinition @'
using System;
public static class StaffPillarRepair {
 public static void Apply(byte[] image,byte[] patch,int w){
  for(int y=357;y<682;y++)for(int x=130;x<222;x++){
   double d=Math.Min(Math.Min(x-130,222-x),Math.Min(y-357,682-y));
   double a=Math.Min(1,d/6.0);a=a*a*(3-2*a);
   int p=(y*w+x)*4;
   for(int c=0;c<3;c++)image[p+c]=(byte)Math.Round(image[p+c]*(1-a)+patch[p+c]*a);
  }
  for(int p=0;p<image.Length;p+=4){
   if(image[p+3]<=16)image[p]=image[p+1]=image[p+2]=image[p+3]=0;
   else if(image[p+3]>=240)image[p+3]=255;
  }
 }
}
'@
[StaffPillarRepair]::Apply($arrays[0],$arrays[1],$raw.Width)
$bd=$raw.LockBits($rect,[System.Drawing.Imaging.ImageLockMode]::WriteOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
[System.Runtime.InteropServices.Marshal]::Copy($arrays[0],0,$bd.Scan0,$arrays[0].Length)
$raw.UnlockBits($bd)
$raw.Save((Join-Path $root 'henkilokuntatilat-oikea-kohdistettava.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$rows=@()
for($y=1;$y -lt $raw.Height;$y++){
 $a=$raw.GetPixel(152,$y-1);$b=$raw.GetPixel(152,$y)
 $d=[math]::Abs([int]$a.R-$b.R)+[math]::Abs([int]$a.G-$b.G)+[math]::Abs([int]$a.B-$b.B)
 if($d -gt 40){$rows+=[pscustomobject]@{Y=$y;RGB="$($b.R),$($b.G),$($b.B)";Difference=$d}}
}
$rows | ConvertTo-Json -Compress
$raw.Dispose();$repair.Dispose();$patch.Dispose()
