$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'WorksiteDetail.cs') -ReferencedAssemblies System.Drawing
$folder=Join-Path $PSScriptRoot 'vegas-konttori-kanttiini-sisatilat'
$src=[Drawing.Bitmap]::new('C:/Users/marku/.codex/generated_images/01a19137-3c84-7223-9249-58aae914b472/exec-adbf0835-cc44-4ff8-89c5-9b6486f85e9c.png')
$left=[Drawing.Bitmap]::new(5120,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($left);$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy;$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$sy=[int][Math]::Round($src.Height*0.535)
$g.DrawImage($src,[Drawing.Rectangle]::new(0,0,5120,1014),0,0,$src.Width,$sy,[Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($src,[Drawing.Rectangle]::new(0,1014,5120,830),0,$sy,$src.Width,($src.Height-$sy),[Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$src.Dispose();$d=[WorksiteDetail]::Read($left);[WorksiteDetail]::Clean($d)
[WorksiteDetail]::Save($d,5120,1844,(Join-Path $folder 'vasen-sommittelu.png'))
$guide=[Drawing.Bitmap]::new(2176,738,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($guide);$g.Clear([Drawing.Color]::FromArgb(255,128,128,128));$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy;$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($left,[Drawing.Rectangle]::new(0,0,128,738),4800,0,320,1844,[Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$left.Dispose();$guide.Save((Join-Path $folder 'oikea-jatko-ohje.png'),[Drawing.Imaging.ImageFormat]::Png);$guide.Dispose()
