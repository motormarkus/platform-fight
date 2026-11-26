param(
    [Parameter(Mandatory=$true)][string]$Before,
    [Parameter(Mandatory=$true)][string]$After,
    [Parameter(Mandatory=$true)][string]$Provenance,
    [Parameter(Mandatory=$true)][string]$Report
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$drawingProbe=[System.Drawing.Bitmap]::new(1,1)
$drawingProbeGraphics=[System.Drawing.Graphics]::FromImage($drawingProbe)
$drawingProbeGraphics.Dispose();$drawingProbe.Dispose()
$drawingReferences=@([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { -not $_.IsDynamic -and $_.GetName().Name -match '^(System\.Drawing|System\.Private\.Windows|System\.Windows\.Extensions)' } | ForEach-Object {$_.Location} | Select-Object -Unique)
Add-Type -ReferencedAssemblies $drawingReferences -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class ArenaQaRoiDifference {
    public static long[] Compare(string before,string after,Rectangle[] allowed){
        using(Bitmap a=new Bitmap(before))using(Bitmap b=new Bitmap(after)){
            if(a.Width!=b.Width||a.Height!=b.Height)throw new Exception("Dimensions differ.");
            BitmapData da=a.LockBits(new Rectangle(0,0,a.Width,a.Height),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
            BitmapData db=b.LockBits(new Rectangle(0,0,b.Width,b.Height),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
            byte[] aa=new byte[da.Stride*a.Height],bb=new byte[db.Stride*b.Height];Marshal.Copy(da.Scan0,aa,0,aa.Length);Marshal.Copy(db.Scan0,bb,0,bb.Length);
            long changed=0,outside=0;
            for(int y=0;y<a.Height;y++)for(int x=0;x<a.Width;x++){
                int i=y*da.Stride+x*3,j=y*db.Stride+x*3;
                if(aa[i]==bb[j]&&aa[i+1]==bb[j+1]&&aa[i+2]==bb[j+2])continue;
                changed++;bool ok=false;foreach(Rectangle r in allowed)if(r.Contains(x,y)){ok=true;break;}if(!ok)outside++;
            }
            a.UnlockBits(da);b.UnlockBits(db);return new long[]{changed,outside};
        }
    }
}
'@
$proof=Get-Content -LiteralPath $Provenance -Raw | ConvertFrom-Json
$rectangles=[System.Collections.Generic.List[System.Drawing.Rectangle]]::new()
foreach($roi in $proof.colorLeveling){$rectangles.Add([System.Drawing.Rectangle]::new([int]$roi.x,[int]$roi.y,[int]$roi.width,[int]$roi.height))}
foreach($roi in $proof.localRepairs){$rectangles.Add([System.Drawing.Rectangle]::new([int]$roi.x,[int]$roi.y,[int]$roi.width,[int]$roi.height))}
$counts=[ArenaQaRoiDifference]::Compare((Resolve-Path -LiteralPath $Before).Path,(Resolve-Path -LiteralPath $After).Path,$rectangles.ToArray())
$result=[pscustomobject]@{changedPixels=$counts[0];changedOutsideReportedSurfaceRois=$counts[1];roiCount=$rectangles.Count;passed=($counts[1] -eq 0)}
$result | ConvertTo-Json | Set-Content -LiteralPath $Report -Encoding utf8
$result | ConvertTo-Json -Compress
if(-not $result.passed){throw 'Non-surface pixels changed during color leveling.'}
