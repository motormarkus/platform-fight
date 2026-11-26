param([Parameter(Mandatory=$true)][string]$AnimationManifest)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$probe=[System.Drawing.Bitmap]::new(1,1);$g=[System.Drawing.Graphics]::FromImage($probe);$g.Dispose();$probe.Dispose()
$drawingReferences=@([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {-not $_.IsDynamic -and $_.GetName().Name -match '^(System\.Drawing|System\.Private\.Windows|System\.Windows\.Extensions)'} | ForEach-Object {$_.Location} | Select-Object -Unique)
$standardReferences=@(Get-ChildItem -LiteralPath (Join-Path $PSHOME 'ref') -Filter '*.dll' | Where-Object {$_.Name -notmatch '^System\.Drawing'} | Select-Object -ExpandProperty FullName)
if(-not ('CrowdOverlayAudit' -as [type])) {
Add-Type -ReferencedAssemblies @($drawingReferences+$standardReferences) -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public sealed class CrowdOverlayCheck {
 public long CompositeDifferentPixels,HiddenRgbPixels,AlphaOutsideMask,AlphaMismatch,StaticDifferentPixels;
 public int MaximumCompositeChannelError;
}
public sealed class CrowdOverlayAudit {
 byte[] source;int width,height,stride;
 static byte[] Read(string path,PixelFormat format,out int w,out int h,out int stride) {
  using(Bitmap b=new Bitmap(path)) {
   w=b.Width;h=b.Height;
   BitmapData d=b.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,format);stride=d.Stride;
   byte[] values=new byte[stride*h];Marshal.Copy(d.Scan0,values,0,values.Length);b.UnlockBits(d);return values;
  }
 }
 public CrowdOverlayAudit(string path) {source=Read(path,PixelFormat.Format24bppRgb,out width,out height,out stride);}
 public CrowdOverlayCheck Check(string fullPath,string overlayPath,string maskPath,int panel,int panelWidth) {
  int fw,fh,fs,ow,oh,os,mw,mh,ms;
  byte[] full=Read(fullPath,PixelFormat.Format24bppRgb,out fw,out fh,out fs);
  byte[] overlay=Read(overlayPath,PixelFormat.Format32bppArgb,out ow,out oh,out os);
  byte[] mask=Read(maskPath,PixelFormat.Format24bppRgb,out mw,out mh,out ms);
  if(fw!=panelWidth||ow!=fw||mw!=fw||fh!=height||oh!=fh||mh!=fh)throw new Exception("Overlay dimensions do not match frame.");
  CrowdOverlayCheck result=new CrowdOverlayCheck();
  for(int y=0;y<height;y++)for(int x=0;x<fw;x++) {
   int i=y*fs+x*3,j=y*os+x*4,k=y*stride+(panel*panelWidth+x)*3,m=y*ms+x*3;
   int alpha=overlay[j+3];byte allowed=mask[m];
   if(alpha!=allowed)result.AlphaMismatch++;
   if(allowed==0&&alpha!=0)result.AlphaOutsideMask++;
   if(alpha==0&&(overlay[j]!=0||overlay[j+1]!=0||overlay[j+2]!=0))result.HiddenRgbPixels++;
   bool different=false,staticDifferent=false;
   double a=alpha/255.0;
   for(int c=0;c<3;c++) {
    int expected=(int)Math.Round(source[k+c]*(1-a)+overlay[j+c]*a);
    int error=Math.Abs(expected-full[i+c]);
    if(error>0)different=true;
    result.MaximumCompositeChannelError=Math.Max(result.MaximumCompositeChannelError,error);
    if(alpha==0&&full[i+c]!=source[k+c])staticDifferent=true;
   }
   if(different)result.CompositeDifferentPixels++;
   if(staticDifferent)result.StaticDifferentPixels++;
  }
  return result;
 }
}
'@
}
$manifest=Get-Content -LiteralPath $AnimationManifest -Raw | ConvertFrom-Json
$audit=[CrowdOverlayAudit]::new([string]$manifest.base)
$results=@();$panelIndex=0
foreach($panel in $manifest.panels) {
    for($frame=0;$frame -lt $panel.frames.Count;$frame++) {
        $check=$audit.Check([string]$panel.frames[$frame],[string]$panel.overlays[$frame],[string]$panel.mask,$panelIndex,[int]$manifest.panelWidth)
        $results+=[pscustomobject]@{Panel=$panel.name;Frame=$frame+1;CompositeDifferentPixels=$check.CompositeDifferentPixels;MaximumCompositeChannelError=$check.MaximumCompositeChannelError;HiddenRgbPixels=$check.HiddenRgbPixels;AlphaOutsideMask=$check.AlphaOutsideMask;AlphaMismatch=$check.AlphaMismatch;StaticDifferentPixels=$check.StaticDifferentPixels}
    }
    $panelIndex++
}
$failures=@($results | Where-Object {$_.CompositeDifferentPixels -ne 0 -or $_.HiddenRgbPixels -ne 0 -or $_.AlphaOutsideMask -ne 0 -or $_.AlphaMismatch -ne 0 -or $_.StaticDifferentPixels -ne 0})
$report=[ordered]@{Passed=($failures.Count -eq 0);FramesChecked=$results.Count;Definition='Straight-alpha overlay composited over immutable native RGB master using round(base*(1-alpha)+overlayRGB*alpha).';Results=$results}
$reportPath=Join-Path (Split-Path -Parent $AnimationManifest) 'overlay-koostamisen-tarkistus.json'
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $reportPath -Encoding utf8
[pscustomobject]@{Passed=$report.Passed;FramesChecked=$results.Count;Failures=$failures.Count;Report=$reportPath} | ConvertTo-Json -Compress
if($failures.Count -gt0){throw 'Crowd overlay audit failed.'}
