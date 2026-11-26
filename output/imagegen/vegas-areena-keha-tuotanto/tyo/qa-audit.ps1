param(
    [Parameter(Mandatory=$true)][string]$AnimationManifest,
    [string]$NativeProvenance,
    [string]$OutDir,
    [int]$ExpectedWidth=5000,
    [int]$ExpectedHeight=1884,
    [double]$MaxLoopJumpRatio=2.5
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
public sealed class ArenaQaImage {
    public int Width,Height;
    public byte[] Pixels;
    public ArenaQaImage(ArenaQaImage source,int x,int y,int width,int height){
        if(x<0||y<0||x+width>source.Width||y+height>source.Height)throw new Exception("Reference crop outside master.");
        Width=width;Height=height;Pixels=new byte[width*height*4];
        for(int row=0;row<height;row++)Buffer.BlockCopy(source.Pixels,((y+row)*source.Width+x)*4,Pixels,row*width*4,width*4);
    }
    public ArenaQaImage(string path) {
        using(Bitmap source=new Bitmap(path))
        using(Bitmap image=new Bitmap(source.Width,source.Height,PixelFormat.Format32bppArgb)) {
            Width=image.Width;Height=image.Height;
            using(Graphics g=Graphics.FromImage(image)) { g.DrawImageUnscaled(source,0,0); }
            BitmapData data=image.LockBits(new Rectangle(0,0,Width,Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            Pixels=new byte[Width*Height*4];
            if(data.Stride==Width*4) Marshal.Copy(data.Scan0,Pixels,0,Pixels.Length);
            else for(int y=0;y<Height;y++) Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),Pixels,y*Width*4,Width*4);
            image.UnlockBits(data);
        }
    }
    public bool[] Mask(string mode) {
        bool[] values=new bool[Width*Height];
        bool useAlpha=mode=="alpha";
        if(mode=="auto")for(int i=3;i<Pixels.Length;i+=4) if(Pixels[i]<255){useAlpha=true;break;}
        int c=useAlpha?3:2;
        for(int i=0;i<values.Length;i++)values[i]=Pixels[i*4+c]!=0;
        return values;
    }
}
public sealed class ArenaQaDifference {
    public long ChangedPixels,ChangedOutsideMask,AllowedPixels;
    public double MeanAbsoluteRgbInsideMask,MaximumChannelChange;
}
public static class ArenaQa {
    public static ArenaQaDifference Diff(ArenaQaImage a,ArenaQaImage b,bool[] mask) {
        if(a.Width!=b.Width||a.Height!=b.Height||mask.Length!=a.Width*a.Height)throw new Exception("Image dimensions differ.");
        ArenaQaDifference result=new ArenaQaDifference();double sum=0;
        for(int p=0;p<mask.Length;p++) {
            int i=p*4;bool changed=false;
            if(mask[p])result.AllowedPixels++;
            for(int c=0;c<4;c++) {
                int d=Math.Abs((int)a.Pixels[i+c]-b.Pixels[i+c]);
                if(d!=0)changed=true;
                if(c<3 && mask[p])sum+=d;
                if(d>result.MaximumChannelChange)result.MaximumChannelChange=d;
            }
            if(changed){result.ChangedPixels++;if(!mask[p])result.ChangedOutsideMask++;}
        }
        result.MeanAbsoluteRgbInsideMask=result.AllowedPixels==0?0:sum/(result.AllowedPixels*3.0);
        return result;
    }
    public static long CheckSlice(ArenaQaImage master,ArenaQaImage part,int x,int y) {
        if(x<0||y<0||x+part.Width>master.Width||y+part.Height>master.Height)throw new Exception("Part is outside master.");
        long changed=0;
        for(int py=0;py<part.Height;py++)for(int px=0;px<part.Width;px++){
            int a=((py+y)*master.Width+px+x)*4,b=(py*part.Width+px)*4;
            if(master.Pixels[a]!=part.Pixels[b]||master.Pixels[a+1]!=part.Pixels[b+1]||master.Pixels[a+2]!=part.Pixels[b+2]||master.Pixels[a+3]!=part.Pixels[b+3])changed++;
        }
        return changed;
    }
    public static double StripJump(ArenaQaImage image,int x,int y0,int y1,int strip) {
        if(x<strip||x+strip>=image.Width)return 0;
        double[] left=new double[3],right=new double[3];long count=0;
        for(int y=y0;y<y1;y++)for(int k=0;k<strip;k++){
            int l=(y*image.Width+x-strip+k)*4,r=(y*image.Width+x+k)*4;
            for(int c=0;c<3;c++){left[c]+=image.Pixels[l+c];right[c]+=image.Pixels[r+c];}count++;
        }
        double result=0;for(int c=0;c<3;c++)result+=Math.Abs(left[c]-right[c])/count/3.0;return result;
    }
}
'@
$animationPath=(Resolve-Path -LiteralPath $AnimationManifest).Path
$animationDirectory=Split-Path -Parent $animationPath
$manifest=Get-Content -LiteralPath $animationPath -Raw | ConvertFrom-Json
if(-not $OutDir){$OutDir=Join-Path $animationDirectory 'qa'}
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
function Resolve-ArenaQaPath([string]$Path,[string]$RelativeTo){
    if([System.IO.Path]::IsPathRooted($Path)){return $Path}
    return Join-Path $RelativeTo $Path
}
$failures=[System.Collections.Generic.List[string]]::new()
$panelReports=@()
$allFramePaths=@()
$baseMaster=$null
if($manifest.base){
    $baseMasterPath=Resolve-ArenaQaPath ([string]$manifest.base) $animationDirectory
    if(Test-Path -LiteralPath $baseMasterPath){$baseMaster=[ArenaQaImage]::new($baseMasterPath)}else{$failures.Add('Static base master is missing: '+$baseMasterPath)}
}
$panelNumber=0
foreach($panel in $manifest.panels){
    $panelName=if($panel.name){[string]$panel.name}else{'unnamed'}
    $frames=@($panel.frames)
    if($frames.Count -ne 6){$failures.Add($panelName+': expected exactly six frames.')}
    $maskPath=Resolve-ArenaQaPath ([string]$panel.mask) $animationDirectory
    $maskImage=[ArenaQaImage]::new($maskPath)
    $maskMode=if($panel.maskMode){[string]$panel.maskMode}else{'auto'}
    $mask=$maskImage.Mask($maskMode)
    if($maskImage.Width -ne $ExpectedWidth -or $maskImage.Height -ne $ExpectedHeight){$failures.Add($panelName+': mask size mismatch.')}
    $resolvedFrames=@($frames | ForEach-Object {Resolve-ArenaQaPath ([string]$_) $animationDirectory})
    $allFramePaths += ,$resolvedFrames
    $reference=[ArenaQaImage]::new($resolvedFrames[0])
    $staticReference=if($baseMaster){[ArenaQaImage]::new($baseMaster,($panelNumber*$ExpectedWidth),0,$ExpectedWidth,$ExpectedHeight)}else{$reference}
    if($reference.Width -ne $ExpectedWidth -or $reference.Height -ne $ExpectedHeight){$failures.Add($panelName+': frame size mismatch.')}
    $frameReports=@();$pairReports=@();$motionSum=0.0;$uniqueChanges=[System.Collections.Generic.HashSet[string]]::new()
    foreach($frameIndex in 0..($resolvedFrames.Count-1)){
        $frame=[ArenaQaImage]::new($resolvedFrames[$frameIndex])
        if($frame.Width -ne $ExpectedWidth -or $frame.Height -ne $ExpectedHeight){$failures.Add($panelName+': unexpected frame size at '+$frameIndex)}
        $delta=[ArenaQa]::Diff($reference,$frame,$mask)
        $staticDelta=[ArenaQa]::Diff($staticReference,$frame,$mask)
        if($staticDelta.ChangedOutsideMask -ne 0){$failures.Add($panelName+': static pixels changed at frame '+($frameIndex+1))}
        $hash=(Get-FileHash -LiteralPath $resolvedFrames[$frameIndex] -Algorithm SHA256).Hash
        [void]$uniqueChanges.Add($hash)
        $frameReports += [pscustomobject]@{frame=($frameIndex+1);file=$resolvedFrames[$frameIndex];width=$frame.Width;height=$frame.Height;changedFromFirst=$delta.ChangedPixels;changedFromStaticBase=$staticDelta.ChangedPixels;changedOutsideMask=$staticDelta.ChangedOutsideMask;allowedPixels=$delta.AllowedPixels;sha256=$hash}
        $nextIndex=($frameIndex+1)%$resolvedFrames.Count
        $next=[ArenaQaImage]::new($resolvedFrames[$nextIndex])
        $pair=[ArenaQa]::Diff($frame,$next,$mask)
        $motionSum+=$pair.MeanAbsoluteRgbInsideMask
        $pairReports += [pscustomobject]@{from=($frameIndex+1);to=($nextIndex+1);changedPixels=$pair.ChangedPixels;meanAbsoluteRgbInsideMask=$pair.MeanAbsoluteRgbInsideMask;changedOutsideMask=$pair.ChangedOutsideMask}
    }
    $meanMotion=$motionSum/$resolvedFrames.Count
    $loopMotion=$pairReports[-1].meanAbsoluteRgbInsideMask
    $loopRatio=if($meanMotion -gt 0){$loopMotion/$meanMotion}else{0}
    if($uniqueChanges.Count -lt 3){$failures.Add($panelName+': fewer than three unique poses.')}
    if($meanMotion -le 0){$failures.Add($panelName+': no visible pixel motion.')}
    if($loopRatio -gt $MaxLoopJumpRatio){$failures.Add($panelName+': last-to-first transition exceeds normal motion.')}
    $panelReports += [pscustomobject]@{name=$panelName;mask=$maskPath;maskMode=$maskMode;uniqueFrames=$uniqueChanges.Count;meanPairDifference=$meanMotion;lastToFirstDifference=$loopMotion;lastToFirstRatio=$loopRatio;frames=$frameReports;cyclicPairs=$pairReports}
    $panelNumber++
}
$nativeReports=@();$provenance=$null
if($NativeProvenance){
    $nativePath=(Resolve-Path -LiteralPath $NativeProvenance).Path
    $nativeDirectory=Split-Path -Parent $nativePath
    $provenance=Get-Content -LiteralPath $nativePath -Raw | ConvertFrom-Json
    foreach($patch in $provenance.nativePatches){
        $path=Resolve-ArenaQaPath ([string]$patch.file) $nativeDirectory
        $nativeImage=[System.Drawing.Image]::FromFile($path)
        $width=$nativeImage.Width;$height=$nativeImage.Height;$nativeImage.Dispose()
        $fits=($width -ge ([int]$patch.cropX+[int]$patch.targetWidth)) -and ($height -ge ([int]$patch.cropY+[int]$patch.targetHeight))
        $unitScale=([double]$patch.scaleX -eq 1.0 -and [double]$patch.scaleY -eq 1.0)
        if(-not $fits -or -not $unitScale){$failures.Add('Native patch was enlarged or has insufficient source pixels: '+$path)}
        $nativeReports += [pscustomobject]@{file=$path;actualWidth=$width;actualHeight=$height;targetWidth=$patch.targetWidth;targetHeight=$patch.targetHeight;unitScale=$unitScale;sourcePixelsSufficient=$fits}
    }
    if($null -ne $provenance.sourcePixelsResampled -and [long]$provenance.sourcePixelsResampled -ne 0){$failures.Add('Native compositor reports resampled source pixels.')}
    if($null -ne $provenance.uncoveredPixels -and [long]$provenance.uncoveredPixels -ne 0){$failures.Add('Native compositor reports uncovered pixels.')}
}
$splitReports=@()
if($manifest.panoramaFrames){
    $masterFrames=@($manifest.panoramaFrames)
    foreach($frameIndex in 0..($masterFrames.Count-1)){
        $masterPath=Resolve-ArenaQaPath ([string]$masterFrames[$frameIndex]) $animationDirectory
        $master=[ArenaQaImage]::new($masterPath)
        foreach($panelIndex in 0..($allFramePaths.Count-1)){
            $part=[ArenaQaImage]::new($allFramePaths[$panelIndex][$frameIndex])
            $different=[ArenaQa]::CheckSlice($master,$part,($panelIndex*$ExpectedWidth),0)
            if($different -ne 0){$failures.Add('Panel does not match the same pixels in master frame '+($frameIndex+1))}
            $splitReports += [pscustomobject]@{frame=($frameIndex+1);panel=($panelIndex+1);differentFromMaster=$different}
        }
    }
}
$result=[pscustomobject]@{passed=($failures.Count -eq 0);expectedPanelWidth=$ExpectedWidth;expectedPanelHeight=$ExpectedHeight;framesPerPanel=6;failures=@($failures);panels=$panelReports;nativePatches=$nativeReports;splitMasterChecks=$splitReports;note='Visual seam and facial identity review is required in addition to these exact pixel checks.'}
$result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $OutDir 'audit.json') -Encoding utf8
[pscustomobject]@{Passed=$result.passed;PanelCount=$panelReports.Count;NativePatchCount=$nativeReports.Count;FailureCount=$failures.Count;Report=(Join-Path $OutDir 'audit.json')} | ConvertTo-Json -Compress
if($failures.Count -ne 0){throw ($failures -join [Environment]::NewLine)}
