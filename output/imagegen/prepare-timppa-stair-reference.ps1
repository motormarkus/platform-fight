$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'WorksiteDetail.cs') -ReferencedAssemblies System.Drawing
$src=Join-Path (Split-Path (Split-Path $PSScriptRoot)) 'Assets/Sprites/Taustat/viidakko_tyomaan_konttori.png'
$folder=Join-Path $PSScriptRoot 'vegas-konttori-pomon-toimisto'
$im=[Drawing.Bitmap]::new($src);$w=$im.Width;$h=$im.Height;$im.Dispose()
[WorksiteDetail]::Crop($src,(Join-Path $folder 'timpan-portaikko-malli.png'),[int]($w*0.372),[int]($h*0.46),[int]($w*0.365),[int]($h*0.33))
Write-Output ('Reference '+$w+'x'+$h)
