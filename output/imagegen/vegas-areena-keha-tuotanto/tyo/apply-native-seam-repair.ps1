param(
    [string]$InputBase=(Join-Path (Split-Path -Parent $PSScriptRoot) 'base-feather'),
    [string]$OutputBase=(Join-Path (Split-Path -Parent $PSScriptRoot) 'base'),
    [string]$Repair=(Join-Path (Split-Path -Parent $PSScriptRoot) 'natiivit/native-seam-repair.png')
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$probe=[System.Drawing.Bitmap]::new(1,1);$probeGraphics=[System.Drawing.Graphics]::FromImage($probe);$probeGraphics.Dispose();$probe.Dispose()
$drawingReferences=@([AppDomain]::CurrentDomain.GetAssemblies()|Where-Object{-not $_.IsDynamic -and $_.GetName().Name -match '^(System\.Drawing|System\.Private\.Windows|System\.Windows\.Extensions)'}|ForEach-Object{$_.Location}|Select-Object -Unique)
$standardReferences=@(Get-ChildItem -LiteralPath (Join-Path $PSHOME 'ref') -Filter '*.dll'|Where-Object{$_.Name -notmatch '^System\.Drawing'}|Select-Object -ExpandProperty FullName)
Add-Type -ReferencedAssemblies @($drawingReferences+$standardReferences) -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class ArenaNativeRepair {
    static double S(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
    public static Bitmap Apply(Bitmap original,Bitmap patch,out long changed,out long outside){
        if(patch.Width!=1536||patch.Height!=1024)throw new ArgumentException("Repair must be native1536x1024; no resizing permitted");
        Bitmap result=(Bitmap)original.Clone();
        BitmapData m=result.LockBits(new Rectangle(0,0,result.Width,result.Height),ImageLockMode.ReadWrite,PixelFormat.Format24bppRgb);
        BitmapData p=patch.LockBits(new Rectangle(0,0,patch.Width,patch.Height),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
        byte[] pixels=new byte[m.Stride*m.Height],before=new byte[m.Stride*m.Height],source=new byte[p.Stride*p.Height];
        Marshal.Copy(m.Scan0,pixels,0,pixels.Length);Array.Copy(pixels,before,pixels.Length);Marshal.Copy(p.Scan0,source,0,source.Length);
        for(int y=410;y<1024;y++)for(int x=160;x<1050;x++){
            double floor=S((y-580.0)/40.0);
            double barrierLeft=S((x-320.0)/120.0),floorLeft=S((x-160.0)/240.0);
            double alpha=((1-floor)*barrierLeft+floor*floorLeft)*S((1050.0-x)/150.0)*S((y-410.0)/24.0);
            if(alpha<=0)continue;
            int a=(860+y)*m.Stride+(4352+x)*3,b=y*p.Stride+x*3;
            for(int c=0;c<3;c++)pixels[a+c]=(byte)Math.Round(pixels[a+c]*(1-alpha)+source[b+c]*alpha);
        }
        changed=0;outside=0;
        for(int y=0;y<result.Height;y++)for(int x=0;x<result.Width;x++){
            int a=y*m.Stride+x*3;
            if(pixels[a]==before[a]&&pixels[a+1]==before[a+1]&&pixels[a+2]==before[a+2])continue;
            changed++;if(x<4512||x>=5402||y<1270||y>=1884)outside++;
        }
        Marshal.Copy(pixels,0,m.Scan0,pixels.Length);result.UnlockBits(m);patch.UnlockBits(p);return result;
    }
}
'@
New-Item -ItemType Directory -Path $OutputBase -Force|Out-Null
$master=[System.Drawing.Bitmap]::new((Join-Path $InputBase 'static-master-15000x1884.png'))
$patch=[System.Drawing.Bitmap]::new($Repair)
[long]$changed=0;[long]$outside=0
$final=[ArenaNativeRepair]::Apply($master,$patch,[ref]$changed,[ref]$outside)
if($outside -ne 0){throw 'Repair changed pixels outside the permitted lower-barrier/floor rectangle'}
$final.Save((Join-Path $OutputBase 'static-master-15000x1884.png'),[System.Drawing.Imaging.ImageFormat]::Png)
for($panel=0;$panel -lt 3;$panel++){
    $tile=$final.Clone([System.Drawing.Rectangle]::new(($panel*5000),0,5000,1884),[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $tile.Save((Join-Path $OutputBase ('static-'+($panel+1).ToString('00')+'-5000x1884.png')),[System.Drawing.Imaging.ImageFormat]::Png);$tile.Dispose()
}
$local=$final.Clone([System.Drawing.Rectangle]::new(4352,860,1536,1024),[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$local.Save((Join-Path $OutputBase 'native-repair-closeup.png'),[System.Drawing.Imaging.ImageFormat]::Png);$local.Dispose()
$preview=[System.Drawing.Bitmap]::new(3000,377,[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$pg=[System.Drawing.Graphics]::FromImage($preview);$pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$pg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$wrap=[System.Drawing.Imaging.ImageAttributes]::new();$wrap.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
$pg.DrawImage($final,[System.Drawing.Rectangle]::new(0,0,3000,377),0,0,15000,1884,[System.Drawing.GraphicsUnit]::Pixel,$wrap)
$preview.Save((Join-Path $OutputBase 'preview-static.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$pg.Dispose();$preview.Dispose();$wrap.Dispose();$final.Dispose();$patch.Dispose();$master.Dispose()
$provenance=Get-Content -LiteralPath (Join-Path $InputBase 'provenance.json') -Raw|ConvertFrom-Json
$provenance.method+='; one extra native1536x1024 generative repair composited1:1 only into lower barrier/floor, with actors, main rail, post and table preserved'
$repairSource=[pscustomobject]@{file=[System.IO.Path]::GetFullPath($Repair);sourceWidth=1536;sourceHeight=1024;cropX=0;cropY=0;targetWidth=1536;targetHeight=1024;x=4352;y=860;scaleX=1;scaleY=1;role='extra native seam repair; localized mask only'}
$provenance.nativePatches=@($provenance.nativePatches)+@($repairSource)
$provenance|Add-Member -NotePropertyName localRepairs -NotePropertyValue @([pscustomobject]@{file=[System.IO.Path]::GetFullPath($Repair);x=4512;y=1270;width=890;height=614;placementX=4352;placementY=860;sourceWidth=1536;sourceHeight=1024;scaleX=1;scaleY=1;changedPixels=$changed;changedOutsideAllowedRectangle=$outside;preserved='all people/chairs/main rail; right post and table'}) -Force
$provenance|ConvertTo-Json -Depth 15|Set-Content -LiteralPath (Join-Path $OutputBase 'provenance.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $InputBase 'source-ownership.bin') -Destination (Join-Path $OutputBase 'source-ownership.bin') -Force
Copy-Item -LiteralPath (Join-Path $InputBase 'preview-source-ownership.png') -Destination (Join-Path $OutputBase 'preview-source-ownership.png') -Force
Copy-Item -LiteralPath (Join-Path $InputBase 'preview-seams.png') -Destination (Join-Path $OutputBase 'preview-seams.png') -Force
[pscustomobject]@{OutputBase=$OutputBase;NativeSourceCount=$provenance.nativePatches.Count;RepairPixels=$changed;ChangedOutsideAllowedRectangle=$outside;SourcePixelsResampled=0}|ConvertTo-Json -Compress
