$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath='C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-ecb96520-6de8-4a1e-92a4-2453f10cbfaf.png'
$src=[System.Drawing.Bitmap]::FromFile($sourcePath)
Add-Type -TypeDefinition @'
using System;
public static class CozyGuardRoomAlphaClean {
 public static string Normalize(byte[] data){
  int clear=0,solid=0,edge=0;
  for(int i=0;i<data.Length;i+=4){
   if(data[i+3]<=16){data[i]=data[i+1]=data[i+2]=data[i+3]=0;clear++;}
   else if(data[i+3]>=240){data[i+3]=255;solid++;}
   else edge++;
  }
  return "Transparent="+clear+"; opaque="+solid+"; antialias="+edge;
 }
}
'@
function Normalize-Alpha([System.Drawing.Bitmap]$im){
 $rect=[System.Drawing.Rectangle]::new(0,0,$im.Width,$im.Height)
 $bd=$im.LockBits($rect,[System.Drawing.Imaging.ImageLockMode]::ReadWrite,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $bytes=[byte[]]::new($bd.Stride*$im.Height)
 [System.Runtime.InteropServices.Marshal]::Copy($bd.Scan0,$bytes,0,$bytes.Length)
 $result=[CozyGuardRoomAlphaClean]::Normalize($bytes)
 [System.Runtime.InteropServices.Marshal]::Copy($bytes,0,$bd.Scan0,$bytes.Length)
 $im.UnlockBits($bd)
 $result
}

Normalize-Alpha $src
$src.Save((Join-Path $root 'vankilan-vartiohuone-viihtyisa-oikea-alkuperainen.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$src.Dispose()



