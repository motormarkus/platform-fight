$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$source='C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-623446c8-f193-4da8-a52d-198fe19e5bee.png'
$native=Join-Path $root 'kasino-sisatilat-oikea-v2-alkuperainen.png'
$final=Join-Path $root 'kasino-sisatilat-oikea-v2-5120x1844.png'
if(Test-Path -LiteralPath $final){throw 'Output already exists; use a versioned sibling.'}
Copy-Item -LiteralPath $source -Destination $native
$src=[System.Drawing.Bitmap]::FromFile($source)
$attrs=[System.Drawing.Imaging.ImageAttributes]::new()
$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
function Export-Size([int]$width,[int]$height,[string]$path){
 $dst=[System.Drawing.Bitmap]::new($width,$height,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[System.Drawing.Graphics]::FromImage($dst)
 $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
 $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
 $g.DrawImage($src,[System.Drawing.Rectangle]::new(0,0,$width,$height),0,0,$src.Width,$src.Height,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
 $dst.Save($path,[System.Drawing.Imaging.ImageFormat]::Png)
 $g.Dispose();$dst.Dispose()
}
Export-Size 5120 1844 $final
Export-Size 1280 461 (Join-Path $root 'kasino-sisatilat-oikea-v2-esikatselu.png')
$check=[System.Drawing.Bitmap]::FromFile($final)
if($check.Width -ne 5120 -or $check.Height -ne 1844){throw 'Wrong final size'}
foreach($xy in @(@(0,0),@(5119,1843),@(2000,1600),@(2400,400))){if($check.GetPixel($xy[0],$xy[1]).A -ne 255){throw 'Interior should be opaque'}}
$report=@(
 'Mode: built-in image_gen edit; spacious gaming hall revision.',
 "Native generation: $($src.Width) x $($src.Height).",
 'Final export: 5120 x 1844 PNG, resampled with HighQualityBicubic; not native 5K detail.',
 'Visual check: spacious receding hall with multiple layers of slot machines and gaming tables, columns and chandeliers; broad elevator and closing wall at right, no people.',
 'Open floor begins approximately at 65% of image height, leaving approximately 35% open walking area.',
 'Left edge remains open for a later continuation; no adjoining panel has yet been generated.',
 "Final saved path: $final"
)
Set-Content -LiteralPath (Join-Path $root 'kasino-sisatilat-oikea-v2-tarkistus.txt') -Value $report
Add-Content -LiteralPath (Join-Path $root 'kasino-sisatilat-oikea-v2-generointiohje.txt') -Value (@('','EXPORT AND VALIDATION')+$report)
$report
$check.Dispose();$attrs.Dispose();$src.Dispose()

