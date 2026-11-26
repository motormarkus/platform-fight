$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=$PSScriptRoot
$inputs=Join-Path $root 'inputs'
New-Item -ItemType Directory -Force -Path $inputs | Out-Null
$base=Join-Path (Split-Path $root -Parent) 'vegas-areena-keha-tuotanto/base'
$world=[System.Drawing.Bitmap]::new(15360,1884,[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$g=[System.Drawing.Graphics]::FromImage($world)
$g.Clear([System.Drawing.Color]::FromArgb(24,20,19))
for($i=0;$i -lt 3;$i++) {
  $im=[System.Drawing.Bitmap]::new((Join-Path $base ('static-{0:00}-5000x1884.png' -f ($i+1))))
  $g.DrawImageUnscaled($im,180+$i*5000,0)
  if($i -eq 0) {
    $edge=$im.Clone([System.Drawing.Rectangle]::new(0,0,180,1884),$im.PixelFormat)
    $edge.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX)
    $g.DrawImageUnscaled($edge,0,0);$edge.Dispose()
  }
  $im.Dispose()
}
$g.Dispose()
$world.Save((Join-Path $inputs 'world-original-15360x1884.png'))
foreach($s in @(@{Name='seam-01';X=4352},@{Name='seam-02';X=9472})) {
  $crop=$world.Clone([System.Drawing.Rectangle]::new($s.X,256,1536,1024),$world.PixelFormat)
  $crop.Save((Join-Path $inputs ($s.Name+'.png')));$crop.Dispose()
}
# Native-pixel context guides: the blank halves are the only regions to paint.
$roof=[System.Drawing.Bitmap]::new(1536,1024,[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$rg=[System.Drawing.Graphics]::FromImage($roof);$rg.Clear([System.Drawing.Color]::FromArgb(45,42,40))
$ctx=$world.Clone([System.Drawing.Rectangle]::new(3600,0,1536,526),$world.PixelFormat)
$rg.DrawImageUnscaled($ctx,0,498);$ctx.Dispose();$rg.Dispose()
$roof.Save((Join-Path $inputs 'roof-context.png'));$roof.Dispose()
$floor=[System.Drawing.Bitmap]::new(1536,1024,[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$fg=[System.Drawing.Graphics]::FromImage($floor);$fg.Clear([System.Drawing.Color]::FromArgb(50,33,26))
$ctx=$world.Clone([System.Drawing.Rectangle]::new(2000,1358,1536,526),$world.PixelFormat)
$fg.DrawImageUnscaled($ctx,0,0);$ctx.Dispose();$fg.Dispose()
$floor.Save((Join-Path $inputs 'floor-context.png'));$floor.Dispose()
$world.Dispose()
Get-ChildItem -LiteralPath $inputs | Select-Object Name,Length | ConvertTo-Json -Compress
