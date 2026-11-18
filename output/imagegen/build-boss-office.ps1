$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'WorksiteDetail.cs') -ReferencedAssemblies System.Drawing
Add-Type -TypeDefinition @'
using System;
public static class OfficeFloorJoin {
 public static void Apply(byte[] canvas,byte[] upper,int w,int h,int uh){
  for(int y=0;y<uh;y++)for(int x=0;x<w;x++){
   int p=(y*w+x)*4;double t=y<1100?1:Math.Max(0,(uh-1-y)/(double)(uh-1-1100));t=t*t*(3-2*t);
   double a=canvas[p+3]*(1-t),b=upper[p+3]*t,alpha=a+b;
   for(int c=0;c<3;c++)canvas[p+c]=alpha>0?(byte)Math.Round((canvas[p+c]*a+upper[p+c]*b)/alpha):(byte)0;
   canvas[p+3]=(byte)Math.Round(alpha);
  }
 }
 public static long CountFloorChanges(byte[] a,byte[] b,int w,int h,int y0){long n=0;for(int p=y0*w*4;p<w*h*4;p++)if(a[p]!=b[p])n++;return n;}
}
'@
$folder=Join-Path $PSScriptRoot 'vegas-konttori-pomon-toimisto'
$manifest=Get-Content -LiteralPath (Join-Path $folder 'osat.json') -Raw|ConvertFrom-Json
$records=Get-Content -LiteralPath (Join-Path $folder 'generoidut.json') -Raw|ConvertFrom-Json
$rawDir=Join-Path $folder 'natiivit';New-Item -ItemType Directory -Path $rawDir -Force|Out-Null
$upper=[byte[]]::new(10240*1242*4);$stats=@()
foreach($tile in $manifest){
 $record=$records|Where-Object id -eq $tile.id
 $match=[regex]::Match($record.hint,'as (C:\\[^\r\n]+?\.png) by default')
 if(!$match.Success){throw ('Missing generated path '+$tile.id)}
 $raw=Join-Path $rawDir ($tile.id+'.png');Copy-Item -LiteralPath $match.Groups[1].Value -Destination $raw
 $w=0;$h=0;$d=[WorksiteDetail]::Load($raw,[ref]$w,[ref]$h)
 $stats+=[pscustomobject]@{id=$tile.id;nativeWidth=$w;nativeHeight=$h;placedWidth=$tile.w;placedHeight=$tile.h}
 [WorksiteDetail]::Clean($d);$d=[WorksiteDetail]::Resize($d,$w,$h,[int]$tile.w,[int]$tile.h);[WorksiteDetail]::Clean($d)
 $overlap=if($tile.col -eq 0){0}else{320}
 [WorksiteDetail]::JoinX($upper,10240,1242,$d,[int]$tile.w,[int]$tile.h,[int]$tile.x,$overlap)
 Write-Output ('Joined '+$tile.id)
}
$bw=0;$bh=0;$base=Join-Path $PSScriptRoot 'vegas-konttori-kanttiini-sisatilat/kanttiini-kokonaisuus-10240x1844.png'
$canvas=[WorksiteDetail]::Load($base,[ref]$bw,[ref]$bh);$original=[byte[]]$canvas.Clone()
[OfficeFloorJoin]::Apply($canvas,$upper,10240,1844,1242)
[WorksiteDetail]::Clean($canvas)
$floorChanges=[OfficeFloorJoin]::CountFloorChanges($canvas,$original,10240,1844,1242)
$final=Join-Path $folder 'pomon-toimisto-kokonaisuus-10240x1844.png'
[WorksiteDetail]::Save($canvas,10240,1844,$final)
[WorksiteDetail]::Crop($final,(Join-Path $folder '01-pomon-toimisto-vasen-5120x1844.png'),0,0,5120,1844)
[WorksiteDetail]::Crop($final,(Join-Path $folder '02-pomon-toimisto-oikea-portaat-5120x1844.png'),5120,0,5120,1844)
[WorksiteDetail]::Preview($final,(Join-Path $folder 'esikatselu.png'),2560,461)
$src=[Drawing.Bitmap]::new($final);$sheet=[Drawing.Bitmap]::new(1800,1100,[Drawing.Imaging.PixelFormat]::Format32bppArgb);$g=[Drawing.Graphics]::FromImage($sheet);$g.Clear([Drawing.Color]::FromArgb(255,35,42,55))
$rects=@(@(1850,120),@(5500,520),@(9200,650),@(4800,600),@(6500,950),@(8100,1100))
for($i=0;$i -lt 6;$i++){$r=$rects[$i];$g.DrawImage($src,[Drawing.Rectangle]::new(($i%3)*600,[Math]::Floor($i/3)*550,600,550),[int]$r[0],[int]$r[1],600,550,[Drawing.GraphicsUnit]::Pixel)}
$g.Dispose();$sheet.Save((Join-Path $folder 'yksityiskohdat-100prosenttia.png'),[Drawing.Imaging.ImageFormat]::Png);$sheet.Dispose();$src.Dispose()
$stats|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $folder 'lahdetarkkuudet.json') -Encoding utf8
[pscustomobject]@{width=10240;height=1844;generationCount=6;reusedFloorFromY=1242;changedFloorChannels=$floorChanges}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $folder 'tarkistus.json') -Encoding utf8
Get-Item -LiteralPath (Join-Path $folder '01-pomon-toimisto-vasen-5120x1844.png'),(Join-Path $folder '02-pomon-toimisto-oikea-portaat-5120x1844.png')|Select-Object Name,Length
