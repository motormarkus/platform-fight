$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
Add-Type -Path (Join-Path $PSScriptRoot 'WorksiteDetail.cs') -ReferencedAssemblies System.Drawing
$folder=Join-Path $PSScriptRoot 'vegas-tyomaakonttori-katu-01'
$nativePath=Join-Path $folder 'sommittelu-natiivi.png'
Copy-Item -LiteralPath 'C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-3a3ee1f5-18c0-4b40-ba61-31b9640f155c.png' -Destination $nativePath
$native=[Drawing.Bitmap]::new($nativePath)
$canvas=[Drawing.Bitmap]::new(5120,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($canvas);$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[Drawing.Imaging.ImageAttributes]::new();$attrs.SetWrapMode([Drawing.Drawing2D.WrapMode]::TileFlipXY)
$ys=@(0,502,565,579,726);$yd=@(0,1196,1386,1415,1844)
for($n=0;$n -lt 4;$n++){
 $g.DrawImage($native,[Drawing.Rectangle]::new(0,$yd[$n],5120,($yd[$n+1]-$yd[$n])),[single]180,[single]$ys[$n],[single]($native.Width-180),[single]($ys[$n+1]-$ys[$n]),[Drawing.GraphicsUnit]::Pixel,$attrs)
}
$attrs.Dispose();$g.Dispose();$native.Dispose()
$d=Pixel-Data $canvas;[WorksiteDetail]::Clean($d);Set-PixelData $canvas $d
$canvas.Save((Join-Path $folder 'sommittelu-5120x1844.png'),[Drawing.Imaging.ImageFormat]::Png)
$dir=Join-Path $folder 'osat';[void][IO.Directory]::CreateDirectory($dir)
$manifest=@();$xs=@(0,1707,3414,5120);$ys=@(0,922,1844)
foreach($row in 0..1){foreach($col in 0..2){
 $x=[Math]::Max(0,$xs[$col]-160);$y=[Math]::Max(0,$ys[$row]-160)
 $r=[Math]::Min(5120,$xs[$col+1]+160);$b=[Math]::Min(1844,$ys[$row+1]+160)
 $id='r'+$row+'c'+$col;$path=Join-Path $dir ($id+'-viite.png')
 $crop=$canvas.Clone([Drawing.Rectangle]::new($x,$y,($r-$x),($b-$y)),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $crop.Save($path,[Drawing.Imaging.ImageFormat]::Png);$crop.Dispose()
 $manifest+=[pscustomobject]@{id=$id;row=$row;col=$col;x=$x;y=$y;w=($r-$x);h=($b-$y);reference=$path}
}}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $folder 'osat.json') -Encoding utf8
$canvas.Dispose()
[WorksiteDetail]::Preview((Join-Path $folder 'sommittelu-5120x1844.png'),(Join-Path $folder 'sommittelu-esikatselu.png'),1536,553)
Write-Output 'Prepared 6 overlapping detail tiles.'
