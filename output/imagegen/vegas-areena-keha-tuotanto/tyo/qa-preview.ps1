param(
    [Parameter(Mandatory=$true)][string]$AnimationManifest,
    [string]$OutDir,
    [int]$PanelPreviewWidth=1000,
    [int]$Fps=0,
    [int]$CropPanel=-1,
    [int]$CropX=0,
    [int]$CropY=0,
    [int]$CropWidth=1200,
    [int]$CropHeight=700,
    [switch]$SavePngFrames
)
$ErrorActionPreference='Stop'
if($Fps -lt 0 -or $Fps -gt 60){throw 'Preview fps must be between1 and60, or0 for manifest timing.'}
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
public static class ArenaQaApng {
    sealed class Chunk { public string Type; public byte[] Data; }
    static uint[] table=BuildTable();
    static uint[] BuildTable(){uint[] t=new uint[256];for(uint n=0;n<256;n++){uint c=n;for(int k=0;k<8;k++)c=(c&1)!=0?0xedb88320U^(c>>1):c>>1;t[n]=c;}return t;}
    static uint Crc(byte[] type,byte[] data){uint c=0xffffffffU;foreach(byte b in type)c=table[(c^b)&255]^(c>>8);foreach(byte b in data)c=table[(c^b)&255]^(c>>8);return c^0xffffffffU;}
    static byte[] Be(uint n){return new byte[]{(byte)(n>>24),(byte)(n>>16),(byte)(n>>8),(byte)n};}
    static uint U32(byte[] a,int i){return ((uint)a[i]<<24)|((uint)a[i+1]<<16)|((uint)a[i+2]<<8)|a[i+3];}
    static List<Chunk> Read(byte[] png){List<Chunk> chunks=new List<Chunk>();int pos=8;while(pos+12<=png.Length){int n=(int)U32(png,pos);if(n<0||pos+n+12>png.Length)throw new Exception("Invalid PNG chunk.");string type=Encoding.ASCII.GetString(png,pos+4,4);byte[] data=new byte[n];Buffer.BlockCopy(png,pos+8,data,0,n);chunks.Add(new Chunk{Type=type,Data=data});pos+=n+12;}return chunks;}
    static void Write(Stream output,string type,byte[] data){byte[] t=Encoding.ASCII.GetBytes(type);byte[] n=Be((uint)data.Length);output.Write(n,0,4);output.Write(t,0,4);output.Write(data,0,data.Length);byte[] crc=Be(Crc(t,data));output.Write(crc,0,4);}
    static byte[] FrameControl(uint sequence,uint w,uint h,int fps){byte[] a=new byte[26];Buffer.BlockCopy(Be(sequence),0,a,0,4);Buffer.BlockCopy(Be(w),0,a,4,4);Buffer.BlockCopy(Be(h),0,a,8,4);a[20]=0;a[21]=1;a[22]=(byte)(fps>>8);a[23]=(byte)fps;a[24]=0;a[25]=0;return a;}
    public static void Save(string path,byte[][] pngs,int fps){
        if(pngs.Length<2)throw new Exception("Need animated frames.");
        List<Chunk> first=Read(pngs[0]);Chunk header=first.Find(c=>c.Type=="IHDR");uint w=U32(header.Data,0),h=U32(header.Data,4);uint sequence=0;
        using(FileStream output=new FileStream(path,FileMode.Create,FileAccess.Write)){
            output.Write(new byte[]{137,80,78,71,13,10,26,10},0,8);Write(output,"IHDR",header.Data);
            foreach(Chunk c in first){if(c.Type=="IDAT")break;if(c.Type=="IHDR"||c.Type=="acTL"||c.Type=="fcTL")continue;Write(output,c.Type,c.Data);}
            byte[] animation=new byte[8];Buffer.BlockCopy(Be((uint)pngs.Length),0,animation,0,4);Write(output,"acTL",animation);
            for(int f=0;f<pngs.Length;f++){
                List<Chunk> chunks=Read(pngs[f]);Chunk ihdr=chunks.Find(c=>c.Type=="IHDR");if(U32(ihdr.Data,0)!=w||U32(ihdr.Data,4)!=h)throw new Exception("Frame dimensions differ.");
                Write(output,"fcTL",FrameControl(sequence++,w,h,fps));
                foreach(Chunk chunk in chunks)if(chunk.Type=="IDAT"){
                    if(f==0)Write(output,"IDAT",chunk.Data);
                    else {byte[] data=new byte[chunk.Data.Length+4];Buffer.BlockCopy(Be(sequence++),0,data,0,4);Buffer.BlockCopy(chunk.Data,0,data,4,chunk.Data.Length);Write(output,"fdAT",data);}
                }
            }
            Write(output,"IEND",new byte[0]);
        }
    }
}
'@
$manifestPath=(Resolve-Path -LiteralPath $AnimationManifest).Path
$manifestDir=Split-Path -Parent $manifestPath
$manifest=Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if($Fps -eq 0){$Fps=if($manifest.frameDurationMs){[int][Math]::Round(1000.0/[double]$manifest.frameDurationMs)}else{8}}
if(-not $OutDir){$OutDir=Join-Path $manifestDir 'preview'}
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
$overviewPngDir=Join-Path $OutDir 'panoraama-png-frames'
$cropPngDir=Join-Path $OutDir 'lahikuva-png-frames'
if($SavePngFrames){New-Item -ItemType Directory -Path $overviewPngDir -Force | Out-Null;if($CropPanel -ge 0){New-Item -ItemType Directory -Path $cropPngDir -Force | Out-Null}}
function Resolve-ArenaPreviewPath([string]$Path){if([System.IO.Path]::IsPathRooted($Path)){return $Path};return Join-Path $manifestDir $Path}
$panels=@($manifest.panels)
if($panels.Count -lt 1){throw 'No panels in animation manifest.'}
$sample=[System.Drawing.Image]::FromFile((Resolve-ArenaPreviewPath ([string]$panels[0].frames[0])))
$previewHeight=[int][Math]::Round($sample.Height*$PanelPreviewWidth/[double]$sample.Width)
$sample.Dispose()
$overviewFrames=[System.Collections.Generic.List[byte[]]]::new()
$cropFrames=[System.Collections.Generic.List[byte[]]]::new()
$wrap=[System.Drawing.Imaging.ImageAttributes]::new();$wrap.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
for($frame=0;$frame -lt 6;$frame++){
    $overview=[System.Drawing.Bitmap]::new(($PanelPreviewWidth*$panels.Count),$previewHeight,[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $g=[System.Drawing.Graphics]::FromImage($overview)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    for($panel=0;$panel -lt $panels.Count;$panel++){
        $image=[System.Drawing.Bitmap]::new((Resolve-ArenaPreviewPath ([string]$panels[$panel].frames[$frame])))
        $g.DrawImage($image,[System.Drawing.Rectangle]::new(($panel*$PanelPreviewWidth),0,$PanelPreviewWidth,$previewHeight),0,0,$image.Width,$image.Height,[System.Drawing.GraphicsUnit]::Pixel,$wrap)
        if($panel -eq $CropPanel){
            if($CropX -lt 0 -or $CropY -lt 0 -or ($CropX+$CropWidth) -gt $image.Width -or ($CropY+$CropHeight) -gt $image.Height){throw 'Native preview crop is outside frame.'}
            $crop=$image.Clone([System.Drawing.Rectangle]::new($CropX,$CropY,$CropWidth,$CropHeight),[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
            $cropStream=[System.IO.MemoryStream]::new();$crop.Save($cropStream,[System.Drawing.Imaging.ImageFormat]::Png)
            if($SavePngFrames){$crop.Save((Join-Path $cropPngDir ('frame-'+($frame+1).ToString('00')+'.png')),[System.Drawing.Imaging.ImageFormat]::Png)}
            $cropFrames.Add($cropStream.ToArray());$cropStream.Dispose();$crop.Dispose()
        }
        $image.Dispose()
    }
    $g.Dispose()
    $stream=[System.IO.MemoryStream]::new();$overview.Save($stream,[System.Drawing.Imaging.ImageFormat]::Png)
    if($SavePngFrames){$overview.Save((Join-Path $overviewPngDir ('frame-'+($frame+1).ToString('00')+'.png')),[System.Drawing.Imaging.ImageFormat]::Png)}
    $overviewFrames.Add($stream.ToArray());$stream.Dispose();$overview.Dispose()
}
$wrap.Dispose()
$overviewPath=Join-Path $OutDir 'yleiso-idle-panoraama.apng'
[ArenaQaApng]::Save($overviewPath,$overviewFrames.ToArray(),$Fps)
$cropPath=$null
if($cropFrames.Count -gt 0){$cropPath=Join-Path $OutDir 'yleiso-idle-lahikuva.apng';[ArenaQaApng]::Save($cropPath,$cropFrames.ToArray(),$Fps)}
[pscustomobject]@{Overview=$overviewPath;Width=($PanelPreviewWidth*$panels.Count);Height=$previewHeight;Frames=6;Fps=$Fps;NativeScaleCloseup=$cropPath;OverviewPngFrames=$overviewPngDir;CloseupPngFrames=$cropPngDir;Note='Preview-only resampling. Final5Kframes are read, never modified.'} | ConvertTo-Json -Compress
