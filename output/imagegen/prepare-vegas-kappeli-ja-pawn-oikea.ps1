$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
function Prepare-Room([string]$slug,[string]$rawPath){
 $outDir=Join-Path $PSScriptRoot ($slug+'-sisatilat');$work=Join-Path $PSScriptRoot ($slug+'-sisa-tyo')
 [void][IO.Directory]::CreateDirectory($outDir);[void][IO.Directory]::CreateDirectory($work)
 $raw=[Drawing.Bitmap]::new($rawPath);$native='{0}x{1}' -f $raw.Width,$raw.Height
 $left=Resize-Canvas $raw 5120 1844
 $left.Save((Join-Path $outDir ($slug+'-vasen-5120x1844.png')),[Drawing.Imaging.ImageFormat]::Png)
 $small=Resize-Canvas $left 1280 461
 $small.Save((Join-Path $outDir 'vasen-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
 $guide=[Drawing.Bitmap]::new(1376,461,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[Drawing.Graphics]::FromImage($guide);$g.Clear([Drawing.Color]::FromArgb(128,128,128));$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
 $g.DrawImage($small,[Drawing.Rectangle]::new(0,0,96,461),[Drawing.Rectangle]::new(1184,0,96,461),[Drawing.GraphicsUnit]::Pixel)
 $g.Dispose();$guide.Save((Join-Path $work 'oikea-jatko-ohje.png'),[Drawing.Imaging.ImageFormat]::Png)
 $native | Set-Content -LiteralPath (Join-Path $work 'vasen-natiivikoko.txt')
 $guide.Dispose();$small.Dispose();$left.Dispose();$raw.Dispose()
 Write-Output ($slug+': '+$native)
}
Prepare-Room 'vegas-kappeli' 'C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-44049ff4-513a-4310-b227-61b3303e2441.png'
Prepare-Room 'vegas-panttilainaamo' 'C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-41c4f227-ab6f-4338-94b1-e659f9f6a8b8.png'
