$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'WorksiteDetail.cs') -ReferencedAssemblies System.Drawing
$folder=Join-Path $PSScriptRoot 'vegas-konttori-kanttiini-sisatilat'
$raw='C:/Users/marku/.codex/generated_images/01a19137-3c84-7223-9249-58aae914b472/exec-5fe8e77d-9e6b-4d4a-9020-2f238cc3ba87.png'
$w=0;$h=0;$r=[WorksiteDetail]::Load($raw,[ref]$w,[ref]$h);$r=[WorksiteDetail]::Resize($r,$w,$h,5440,1844);[WorksiteDetail]::Clean($r)
$lw=0;$lh=0;$left=[WorksiteDetail]::Load((Join-Path $folder 'vasen-sommittelu.png'),[ref]$lw,[ref]$lh)
$canvas=[byte[]]::new(10240*1844*4)
[WorksiteDetail]::JoinX($canvas,10240,1844,$left,5120,1844,0,0)
[WorksiteDetail]::JoinX($canvas,10240,1844,$r,5440,1844,4800,320)
[WorksiteDetail]::Clean($canvas)
$base=Join-Path $folder 'sommittelu-10240x1844.png'
[WorksiteDetail]::Save($canvas,10240,1844,$base)
[WorksiteDetail]::Preview($base,(Join-Path $folder 'sommittelu-esikatselu.png'),2560,461)
$dir=Join-Path $folder 'osat';New-Item -ItemType Directory -Path $dir -Force | Out-Null
$xs=@(0,1707,3414,5120,6827,8534,10240);$ys=@(0,922,1844);$manifest=@()
for($row=0;$row -lt 2;$row++){for($col=0;$col -lt 6;$col++){
 $x=[Math]::Max(0,$xs[$col]-160);$y=[Math]::Max(0,$ys[$row]-160)
 $cw=[Math]::Min(10240,$xs[$col+1]+160)-$x;$ch=[Math]::Min(1844,$ys[$row+1]+160)-$y
 $id='r'+$row+'c'+$col;$path=Join-Path $dir ($id+'-viite.png')
 [WorksiteDetail]::Crop($base,$path,$x,$y,$cw,$ch)
 $manifest+=[pscustomobject]@{id=$id;row=$row;col=$col;x=$x;y=$y;w=$cw;h=$ch;reference=$path}
}}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $folder 'osat.json') -Encoding utf8
$sheet=[Drawing.Bitmap]::new(3072,600,[Drawing.Imaging.PixelFormat]::Format32bppArgb);$g=[Drawing.Graphics]::FromImage($sheet);$g.Clear([Drawing.Color]::FromArgb(255,35,42,55))
$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
foreach($tile in $manifest){$im=[Drawing.Bitmap]::new($tile.reference);$g.DrawImage($im,[Drawing.Rectangle]::new(($tile.col*512),($tile.row*300),512,300));$im.Dispose()}
$g.Dispose();$sheet.Save((Join-Path $folder 'osien-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$sheet.Dispose()
