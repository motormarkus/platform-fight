$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'WorksiteDetail.cs') -ReferencedAssemblies System.Drawing
$folder=Join-Path $PSScriptRoot 'vegas-konttori-pomon-toimisto';$dir=Join-Path $folder 'viitteet'
New-Item -ItemType Directory -Path $folder,$dir -Force | Out-Null
$base=Join-Path $PSScriptRoot 'vegas-konttori-kanttiini-sisatilat/kanttiini-kokonaisuus-10240x1844.png'
$xs=@(0,1707,3414,5120,6827,8534,10240);$manifest=@()
for($col=0;$col -lt 6;$col++){
 $x=[Math]::Max(0,$xs[$col]-160);$w=[Math]::Min(10240,$xs[$col+1]+160)-$x;$path=Join-Path $dir ('osa'+$col+'.png')
 [WorksiteDetail]::Crop($base,$path,$x,0,$w,1242)
 $manifest+=[pscustomobject]@{id=('osa'+$col);col=$col;x=$x;w=$w;h=1242;reference=$path}
}
$manifest|ConvertTo-Json -Depth 4|Set-Content -LiteralPath (Join-Path $folder 'osat.json') -Encoding utf8
$sheet=[Drawing.Bitmap]::new(2400,600,[Drawing.Imaging.PixelFormat]::Format32bppArgb);$g=[Drawing.Graphics]::FromImage($sheet);$g.Clear([Drawing.Color]::FromArgb(255,35,42,55));$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
foreach($tile in $manifest){$im=[Drawing.Bitmap]::new($tile.reference);$g.DrawImage($im,[Drawing.Rectangle]::new(($tile.col*400),0,400,600));$im.Dispose()}
$g.Dispose();$sheet.Save((Join-Path $folder 'viitteet.png'),[Drawing.Imaging.ImageFormat]::Png);$sheet.Dispose()
