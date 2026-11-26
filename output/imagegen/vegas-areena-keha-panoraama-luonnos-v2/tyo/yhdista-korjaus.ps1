$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$taskOutput = Split-Path -Parent $PSScriptRoot
$oldOutput = 'G:/unity pelit/mopopeli/platform fight/output/imagegen/vegas-areena-keha-panoraama-luonnos'
$generatedPath = 'C:/Users/marku/.codex/generated_images/01a207f3-74e0-7c63-b5d8-b78973c50087/exec-111fb85b-7510-4fa7-9f4d-5bc34f0d983e.png'
Copy-Item -LiteralPath $generatedPath -Destination (Join-Path $PSScriptRoot 'liitos-generoitu.png')
$source = [System.Drawing.Bitmap]::new((Join-Path $oldOutput 'panoraama-luonnos.png'))
$generated = [System.Drawing.Image]::FromFile($generatedPath)
$sized = [System.Drawing.Bitmap]::new(1536,770,[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$sizeGraphics = [System.Drawing.Graphics]::FromImage($sized)
$sizeGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$sizeGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$wrap = [System.Drawing.Imaging.ImageAttributes]::new()
$wrap.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
$sizeGraphics.DrawImage($generated,[System.Drawing.Rectangle]::new(0,0,1536,770),0,0,$generated.Width,$generated.Height,[System.Drawing.GraphicsUnit]::Pixel,$wrap)
$sizeGraphics.Dispose()
$generated.Dispose()

# Register the regenerated front rail and skirting to the original native rows.
$registered = [System.Drawing.Bitmap]::new(1536,770,[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$registrationGraphics = [System.Drawing.Graphics]::FromImage($registered)
$registrationGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$registrationGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$sourceRows = @(0,503,586,770)
$targetRows = @(0,502,586,770)
for ($band=0; $band -lt 3; $band++) {
    $registrationGraphics.DrawImage($sized,[System.Drawing.Rectangle]::new(0,$targetRows[$band],1536,($targetRows[$band+1]-$targetRows[$band])),0,$sourceRows[$band],1536,($sourceRows[$band+1]-$sourceRows[$band]),[System.Drawing.GraphicsUnit]::Pixel,$wrap)
}
$registrationGraphics.Dispose()
$sized.Dispose()
$registered.Save((Join-Path $PSScriptRoot 'liitos-kohdistettu.png'),[System.Drawing.Imaging.ImageFormat]::Png)

$drawingReferences = @([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { -not $_.IsDynamic -and $_.GetName().Name -match '^(System\.Drawing|System\.Private\.Windows|System\.Windows\.Extensions)' } | ForEach-Object { $_.Location } | Select-Object -Unique)
Add-Type -ReferencedAssemblies $drawingReferences -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class ArenaSeamPatch {
    static double Smooth(double t) { t=Math.Max(0,Math.Min(1,t)); return t*t*(3-2*t); }
    public static Bitmap Compose(Bitmap source, Bitmap patch, int offset, out long changed, out long changedOutside) {
        Bitmap result = (Bitmap)source.Clone();
        Rectangle whole=new Rectangle(0,0,result.Width,result.Height);
        Rectangle rect=new Rectangle(0,0,patch.Width,patch.Height);
        BitmapData data=result.LockBits(whole,ImageLockMode.ReadWrite,PixelFormat.Format24bppRgb);
        BitmapData pd=patch.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
        byte[] pixels=new byte[data.Stride*data.Height];
        byte[] before=new byte[pixels.Length];
        byte[] repl=new byte[pd.Stride*pd.Height];
        Marshal.Copy(data.Scan0,pixels,0,pixels.Length); Array.Copy(pixels,before,pixels.Length);
        Marshal.Copy(pd.Scan0,repl,0,repl.Length);
        for(int y=0;y<patch.Height;y++) {
            double floor=Smooth((y-580.0)/80.0);
            for(int x=0;x<1024;x++) {
                double right=Smooth((1024.0-x)/128.0);
                double wall=Smooth((x-256.0)/128.0)*right;
                // Make cuts in the gaps between whole spectators; never cross-fade faces.
                if(y>=170 && y<506) {
                    double cut = y<310 ? 410 : (y<385 ? 300 : 410);
                    wall=Smooth((x-cut)/2.0)*Smooth((960.0-x)/2.0);
                } else if(y>=506 && y<=600) {
                    wall=Smooth((x-256.0)/2.0)*Smooth((960.0-x)/2.0);
                }
                double ground=Smooth(x/384.0)*right;
                double a=wall*(1-floor)+ground*floor;
                if(a<=0)continue;
                int i=y*data.Stride+(offset+x)*3;
                int j=y*pd.Stride+x*3;
                for(int c=0;c<3;c++) pixels[i+c]=(byte)Math.Round(pixels[i+c]*(1-a)+repl[j+c]*a);
            }
        }
        changed=0; changedOutside=0;
        for(int y=0;y<result.Height;y++)for(int x=0;x<result.Width;x++) {
            int i=y*data.Stride+x*3;
            if(pixels[i]!=before[i]||pixels[i+1]!=before[i+1]||pixels[i+2]!=before[i+2]) {
                changed++;
                if(x<offset||x>=offset+1024)changedOutside++;
            }
        }
        Marshal.Copy(pixels,0,data.Scan0,pixels.Length);
        result.UnlockBits(data); patch.UnlockBits(pd);
        return result;
    }
}
'@
[long]$changedPixels=0
[long]$outsidePixels=0
$result = [ArenaSeamPatch]::Compose($source,$registered,1275,[ref]$changedPixels,[ref]$outsidePixels)
if ($outsidePixels -ne 0) { throw 'Pixels outside repair area changed.' }
$result.Save((Join-Path $taskOutput 'panoraama-luonnos-v2.png'),[System.Drawing.Imaging.ImageFormat]::Png)

$partsOutput=Join-Path $taskOutput 'luonnoksen-osat'
New-Item -ItemType Directory -Path $partsOutput -Force | Out-Null
$names=@('01-katsomo-ja-kaytava.png','02-kehan-vasen-puoli.png','03-kehan-oikea-puoli.png')
for ($panel=0; $panel -lt 3; $panel++) {
    $piece=$result.Clone([System.Drawing.Rectangle]::new(($panel*2043),0,2043,770),[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $piece.Save((Join-Path $partsOutput $names[$panel]),[System.Drawing.Imaging.ImageFormat]::Png)
    $piece.Dispose()
}
$detail=$result.Clone([System.Drawing.Rectangle]::new(1275,0,1536,770),[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$detail.Save((Join-Path $taskOutput 'liitos-korjattu-lahikuva.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$detail.Dispose()
$preview=[System.Drawing.Bitmap]::new(3000,377,[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$pg=[System.Drawing.Graphics]::FromImage($preview)
$pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$pg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$pg.DrawImage($result,[System.Drawing.Rectangle]::new(0,0,3000,377),0,0,$result.Width,$result.Height,[System.Drawing.GraphicsUnit]::Pixel,$wrap)
$preview.Save((Join-Path $taskOutput 'panoraama-esikatselu-v2.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$pg.Dispose();$preview.Dispose();$result.Dispose();$registered.Dispose();$source.Dispose();$wrap.Dispose()
[pscustomobject]@{Width=6129;Height=770;OriginalJoinX=2043;RepairStartsX=1275;RepairEndsBeforeX=2299;ChangedPixels=$changedPixels;ChangedOutsideRepair=$outsidePixels;GeneratedBy='built-in image_gen';Operation='Local repair, alignment and masked compositing';AnimationFrames=0} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskOutput 'tarkistus.json') -Encoding utf8
Get-Content -LiteralPath (Join-Path $taskOutput 'tarkistus.json')
