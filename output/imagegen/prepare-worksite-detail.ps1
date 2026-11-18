$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$folder=Join-Path $PSScriptRoot 'vegas-tyomaa-tarkka-v2'
$tilesDir=Join-Path $folder 'osat'
New-Item -ItemType Directory -Path $folder,$tilesDir -Force | Out-Null
$source=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-tyomaa-yhtena\vegas-tyomaa-yhtenainen-15360x5632.png'))
$tiles=@()
for($row=0;$row -lt 4;$row++){
 for($col=0;$col -lt 8;$col++){
  $coreX=$col*1920;$coreY=$row*1408
  $left=[math]::Max(0,$coreX-192);$top=[math]::Max(0,$coreY-192)
  $right=[math]::Min(15360,$coreX+1920+192);$bottom=[math]::Min(5632,$coreY+1408+192)
  $id=('r{0}c{1}' -f $row,$col)
  $crop=$source.Clone([Drawing.Rectangle]::new($left,$top,($right-$left),($bottom-$top)),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $path=Join-Path $tilesDir ($id+'-viite.png')
  $crop.Save($path,[Drawing.Imaging.ImageFormat]::Png);$crop.Dispose()
  $tiles+=[pscustomobject]@{id=$id;row=$row;col=$col;x=$left;y=$top;w=($right-$left);h=($bottom-$top);coreX=$coreX;coreY=$coreY;coreW=1920;coreH=1408;reference=$path}
 }
}
$tiles | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $folder 'osat.json') -Encoding utf8
$source.Dispose()
