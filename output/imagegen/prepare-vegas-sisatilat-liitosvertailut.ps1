$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
function Prepare([string]$slug,[string]$rawPath){
 $work=Join-Path $PSScriptRoot ($slug+'-sisa-tyo');$dir=Join-Path $PSScriptRoot ($slug+'-sisatilat-v2')
 $raw=[Drawing.Bitmap]::new($rawPath)
 $x=$raw.Width*96.0/1376.0;$cw=$raw.Width-$x
 $right=[Drawing.Bitmap]::new(5120,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[Drawing.Graphics]::FromImage($right);$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy;$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
 $attr=[Drawing.Imaging.ImageAttributes]::new();$attr.SetWrapMode([Drawing.Drawing2D.WrapMode]::TileFlipXY)
 $g.DrawImage($raw,[Drawing.Rectangle]::new(0,0,5120,1844),[single]$x,[single]0,[single]$cw,[single]$raw.Height,[Drawing.GraphicsUnit]::Pixel,$attr)
 $g.Dispose();$attr.Dispose()
 $right.Save((Join-Path $work 'oikea-raaka-5120x1844.png'),[Drawing.Imaging.ImageFormat]::Png)
 ('{0}x{1}' -f $raw.Width,$raw.Height) | Set-Content -LiteralPath (Join-Path $work 'oikea-natiivikoko.txt')
 $left=[Drawing.Bitmap]::new((Join-Path $dir ($slug+'-vasen-5120x1844.png')))
 $pair=[Drawing.Bitmap]::new(1024,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[Drawing.Graphics]::FromImage($pair)
 $g.DrawImage($left,[Drawing.Rectangle]::new(0,0,512,1844),[Drawing.Rectangle]::new(4608,0,512,1844),[Drawing.GraphicsUnit]::Pixel)
 $g.DrawImage($right,[Drawing.Rectangle]::new(512,0,512,1844),[Drawing.Rectangle]::new(0,0,512,1844),[Drawing.GraphicsUnit]::Pixel)
 $g.Dispose();$pair.Save((Join-Path $work 'raaka-liitos.png'),[Drawing.Imaging.ImageFormat]::Png)
 $pair.Dispose();$left.Dispose();$right.Dispose();$raw.Dispose()
}
Prepare 'vegas-kappeli' 'C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-26437edb-adaf-456f-b296-aafd9a49590d.png'
Prepare 'vegas-panttilainaamo' 'C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-a4f1407b-c6cb-4e42-b957-6fff41137f3b.png'
