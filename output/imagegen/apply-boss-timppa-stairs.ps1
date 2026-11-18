$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'WorksiteDetail.cs') -ReferencedAssemblies System.Drawing
Add-Type -TypeDefinition @'
using System;
public static class BossWallStairs {
 static double Smooth(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
 public static void Apply(byte[] d,byte[] patch,int w,int pw,int ph){
  for(int y=0;y<ph;y++)for(int x=0;x<pw;x++){
   double t=Smooth((x-325)/45.0)*Smooth((y-145)/45.0)*Smooth((1150-y)/100.0);
   if(t<=0)continue;
   int p=(y*w+8870+x)*4,q=(y*pw+x)*4;
   double a=d[p+3]*(1-t),b=patch[q+3]*t,alpha=a+b;
   for(int c=0;c<3;c++)d[p+c]=alpha>0?(byte)Math.Round((d[p+c]*a+patch[q+c]*b)/alpha):(byte)0;
   d[p+3]=(byte)Math.Round(alpha);
  }
 }
 public static int FixFloorAlpha(byte[] d,byte[] floor,int w,int h){int fixedPixels=0;for(int y=1014;y<h;y++)for(int x=5120;x<w;x++){
   int p=(y*w+x)*4;if(d[p+3]==255)continue;double a=d[p+3]/255.0;
   for(int c=0;c<3;c++)d[p+c]=(byte)Math.Round(d[p+c]*a+floor[p+c]*(1-a));d[p+3]=255;fixedPixels++;
  }return fixedPixels;
 }
 public static long LeftChanges(byte[] a,byte[] b,int w,int h){long n=0;for(int y=0;y<h;y++)for(int x=0;x<5120;x++)for(int c=0;c<4;c++){int p=(y*w+x)*4+c;if(a[p]!=b[p])n++;}return n;}
}
'@
$folder=Join-Path $PSScriptRoot 'vegas-konttori-pomon-toimisto'
$w=0;$h=0;$d=[WorksiteDetail]::Load((Join-Path $folder 'pomon-toimisto-kokonaisuus-10240x1844.png'),[ref]$w,[ref]$h);$before=[byte[]]$d.Clone()
$pw=0;$ph=0;$raw='C:/Users/marku/.codex/generated_images/01a19137-3c84-7223-9249-58aae914b472/exec-3b0e374e-2f16-4759-a1a2-485e52632d55.png'
Copy-Item -LiteralPath $raw -Destination (Join-Path $folder 'timpan-mallinen-portaikko-natiivi.png')
$patch=[WorksiteDetail]::Load($raw,[ref]$pw,[ref]$ph);$patch=[WorksiteDetail]::Resize($patch,$pw,$ph,1370,1260);[WorksiteDetail]::Clean($patch)
[BossWallStairs]::Apply($d,$patch,10240,1370,1260)
$fw=0;$fh=0;$floor=[WorksiteDetail]::Load((Join-Path $PSScriptRoot 'vegas-konttori-kanttiini-sisatilat/kanttiini-kokonaisuus-10240x1844.png'),[ref]$fw,[ref]$fh)
$fixed=[BossWallStairs]::FixFloorAlpha($d,$floor,10240,1844)
$final=Join-Path $folder 'pomon-toimisto-kokonaisuus-v4-10240x1844.png'
[WorksiteDetail]::Save($d,10240,1844,$final)
[WorksiteDetail]::Crop($final,(Join-Path $folder '02-pomon-toimisto-oikea-portaat-v4-5120x1844.png'),5120,0,5120,1844)
[WorksiteDetail]::Preview($final,(Join-Path $folder 'esikatselu-v4.png'),2560,461)
[WorksiteDetail]::Crop($final,(Join-Path $folder 'timpan-mallinen-portaikko-tarkistus.png'),8870,0,1370,1260)
[pscustomobject]@{leftTileChangedChannels=[BossWallStairs]::LeftChanges($d,$before,10240,1844);floorAlphaFixedPixels=$fixed;stairNativeWidth=$pw;stairNativeHeight=$ph;imageWidth=5120;imageHeight=1844}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $folder 'portaikko-v4-tarkistus.json') -Encoding utf8
Get-Content -LiteralPath (Join-Path $folder 'portaikko-v4-tarkistus.json')


