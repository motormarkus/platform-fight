param(
    [Parameter(Mandatory=$true)][string]$MasterPath,
    [Parameter(Mandatory=$true)][string]$OwnerMap,
    [Parameter(Mandatory=$true)][string]$OutDir
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
$master=[System.Drawing.Bitmap]::new((Resolve-Path -LiteralPath $MasterPath).Path)
$owner=[System.IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $OwnerMap).Path)
if($master.Width -ne 15000 -or $master.Height -ne 1884 -or $owner.Length -ne $master.Width*$master.Height){throw 'Unexpected native master or ownership dimensions.'}
$bands=@(@{name='bottom-floor';y0=1790;y1=1884;columns=@(1..11)},@{name='approach-floor';y0=1500;y1=1760;columns=@(1..4)},@{name='ring-canvas';y0=1220;y1=1340;columns=@(6..10)})
$reports=@()
foreach($band in $bands){foreach($column in $band.columns){
    $left=@(0.0,0.0,0.0);$right=@(0.0,0.0,0.0);$count=0;$seamXs=@()
    for($y=[int]$band.y0;$y -lt [int]$band.y1;$y+=4){
        $seamX=-1
        for($x=1224*$column;$x -lt (1224*$column+312);$x++){if($owner[$y*15000+$x] -eq (13+$column)){$seamX=$x;break}}
        if($seamX -lt 0){continue}
        $seamXs+=$seamX
        for($k=0;$k -lt 8;$k++){
            $l=$master.GetPixel(($seamX-8+$k),$y);$r=$master.GetPixel(($seamX+$k),$y)
            $left[0]+=$l.R;$left[1]+=$l.G;$left[2]+=$l.B;$right[0]+=$r.R;$right[1]+=$r.G;$right[2]+=$r.B;$count++
        }
    }
    if($count -gt 0){$delta=@();$absMean=0.0;for($c=0;$c -lt 3;$c++){$d=($right[$c]-$left[$c])/$count;$delta+=[Math]::Round($d,3);$absMean+=[Math]::Abs($d)/3.0};$reports+=[pscustomobject]@{band=$band.name;leftPatch=('r1c'+($column-1).ToString('00'));rightPatch=('r1c'+$column.ToString('00'));meanRgbRightMinusLeft=$delta;meanChannelJump=[Math]::Round($absMean,3);minSeamX=($seamXs|Measure-Object -Minimum).Minimum;maxSeamX=($seamXs|Measure-Object -Maximum).Maximum;samplePixels=$count}}
}}
foreach($x in @(5000,10000)){
    $crop=$master.Clone([System.Drawing.Rectangle]::new(($x-700),0,1400,1884),[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $crop.Save((Join-Path $OutDir ('tile-boundary-'+$x+'-native.png')),[System.Drawing.Imaging.ImageFormat]::Png);$crop.Dispose()
    $floorCrop=$master.Clone([System.Drawing.Rectangle]::new(($x-700),1084,1400,800),[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $floorCrop.Save((Join-Path $OutDir ('tile-boundary-'+$x+'-lower.png')),[System.Drawing.Imaging.ImageFormat]::Png);$floorCrop.Dispose()
}
$sheet=[System.Drawing.Bitmap]::new(1600,1872,[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$g=[System.Drawing.Graphics]::FromImage($sheet);$g.Clear([System.Drawing.Color]::FromArgb(25,25,25));$font=[System.Drawing.Font]::new('Arial',11)
for($column=1;$column -le 11;$column++){
    $sheetX=(($column-1)%4)*400;$sheetY=[int][Math]::Floor(($column-1)/4.0)*624
    $center=1224*$column+156
    $piece=$master.Clone([System.Drawing.Rectangle]::new(($center-198),430,396,600),[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $g.DrawImageUnscaled($piece,($sheetX+2),$sheetY);$piece.Dispose()
    $g.DrawString(('quilt '+($column-1)+' / '+$column+'  x'+$center),$font,[System.Drawing.Brushes]::White,$sheetX,($sheetY+600))
}
$g.Dispose();$font.Dispose();$sheet.Save((Join-Path $OutDir 'native-quilt-head-seams.png'),[System.Drawing.Imaging.ImageFormat]::Png)
for($page=0;$page -lt 3;$page++){$part=$sheet.Clone([System.Drawing.Rectangle]::new(0,($page*624),1600,624),[System.Drawing.Imaging.PixelFormat]::Format24bppRgb);$part.Save((Join-Path $OutDir ('head-seams-'+($page+1)+'.png')),[System.Drawing.Imaging.ImageFormat]::Png);$part.Dispose()}
$sheet.Dispose();$master.Dispose()
$reports | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $OutDir 'surface-seam-statistics.json') -Encoding utf8
$reports | ConvertTo-Json -Compress
