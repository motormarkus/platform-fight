$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path @((Join-Path $PSScriptRoot 'WorksiteDetail.cs'),(Join-Path $PSScriptRoot 'OfficeExtension.cs')) -ReferencedAssemblies System.Drawing
$folder=Join-Path $PSScriptRoot 'vegas-tyomaakonttori-jatko-ja-katto'
$raw=Join-Path $folder 'sommittelu-natiivi.png'
Copy-Item -LiteralPath 'C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-93fa7a62-5ed1-4e6c-af62-e2833ab4b2d0.png' -Destination $raw
$rw=0;$rh=0;$d=[WorksiteDetail]::Load($raw,[ref]$rw,[ref]$rh);$d=[WorksiteDetail]::Resize($d,$rw,$rh,10240,3688);[WorksiteDetail]::Clean($d)
$source=Join-Path $PSScriptRoot 'vegas-tyomaakonttori-kaikki-ikkunat-tyhjat\vegas-tyomaakonttori-lasit-tyhjat-5120x1844.png'
$ow=0;$oh=0;$old=[WorksiteDetail]::Load($source,[ref]$ow,[ref]$oh)
'New upper openings: '+([OfficeExtension]::Runs($d,10240,1844,5120) -join ',')
'Old upper openings: '+([OfficeExtension]::Runs($old,5120,0,5120) -join ',')
[OfficeExtension]::AlignUpper($d,10240,3688,$old,5120)
[OfficeExtension]::RestoreAndCollar($d,10240,3688,$old,5120,1844)
$base=Join-Path $folder 'sommittelu-10240x3688.png';[WorksiteDetail]::Save($d,10240,3688,$base)
[WorksiteDetail]::Preview($base,(Join-Path $folder 'sommittelu-esikatselu.png'),2048,738)
$dir=Join-Path $folder 'osat';[void][IO.Directory]::CreateDirectory($dir)
$manifest=@();$xs=@(0,1707,3414,5120,6827,8534,10240);$ys=@(0,922,1844,2766,3688)
$im=[Drawing.Bitmap]::new($base)
foreach($row in 0..3){foreach($col in 0..5){
 if($row -ge 2 -and $col -lt 3){continue}
 $x=[Math]::Max(0,$xs[$col]-160);$y=[Math]::Max(0,$ys[$row]-160);$r=[Math]::Min(10240,$xs[$col+1]+160);$b=[Math]::Min(3688,$ys[$row+1]+160)
 $id='r'+$row+'c'+$col;$path=Join-Path $dir ($id+'-viite.png')
 $crop=$im.Clone([Drawing.Rectangle]::new($x,$y,($r-$x),($b-$y)),[Drawing.Imaging.PixelFormat]::Format32bppArgb);$crop.Save($path,[Drawing.Imaging.ImageFormat]::Png);$crop.Dispose()
 $manifest+=[pscustomobject]@{id=$id;row=$row;col=$col;x=$x;y=$y;w=($r-$x);h=($b-$y);reference=$path}
}}
$im.Dispose();$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $folder 'osat.json') -Encoding utf8
$contact=[Drawing.Bitmap]::new(3072,1104,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$cg=[Drawing.Graphics]::FromImage($contact);$cg.Clear([Drawing.Color]::FromArgb(34,40,58));$font=[Drawing.Font]::new('Arial',14)
for($n=0;$n -lt $manifest.Count;$n++){$tile=[Drawing.Bitmap]::new($manifest[$n].reference);$cx=($n%6)*512;$cy=[Math]::Floor($n/6)*368;$cg.DrawImage($tile,[Drawing.Rectangle]::new($cx,$cy+24,512,344));$cg.DrawString($manifest[$n].id,$font,[Drawing.Brushes]::White,$cx+5,$cy);$tile.Dispose()}
$font.Dispose();$cg.Dispose();$contact.Save((Join-Path $folder 'osien-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$contact.Dispose()
