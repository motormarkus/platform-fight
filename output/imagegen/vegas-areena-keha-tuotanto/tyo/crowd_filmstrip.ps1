param(
    [Parameter(Mandatory=$true)][string]$AnimationManifest,
    [Parameter(Mandatory=$true)][string]$OutputPath,
    [int]$Panel=0,
    [int]$CropX=1480,
    [int]$CropY=650,
    [int]$CropWidth=350,
    [int]$CropHeight=350
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$manifest=Get-Content -LiteralPath $AnimationManifest -Raw | ConvertFrom-Json
$framePaths=@($manifest.panels[$Panel].frames)
if($framePaths.Count -ne 6){throw 'Expected exactly six animation frames.'}
$strip=[System.Drawing.Bitmap]::new(($CropWidth*6),($CropHeight+30),[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$g=[System.Drawing.Graphics]::FromImage($strip)
$g.Clear([System.Drawing.Color]::FromArgb(15,15,15))
$font=[System.Drawing.Font]::new('Arial',13,[System.Drawing.FontStyle]::Bold)
for($index=0;$index -lt6;$index++) {
    $source=[System.Drawing.Bitmap]::new($framePaths[$index])
    if($CropX -lt0 -or $CropY -lt0 -or $CropX+$CropWidth -gt$source.Width -or $CropY+$CropHeight -gt$source.Height){throw 'Filmstrip crop is outside frame.'}
    $crop=$source.Clone([System.Drawing.Rectangle]::new($CropX,$CropY,$CropWidth,$CropHeight),[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $g.DrawImageUnscaled($crop,($index*$CropWidth),0)
    $g.DrawString(('Frame'+($index+1).ToString('00')),$font,[System.Drawing.Brushes]::White,($index*$CropWidth+10),($CropHeight+5))
    $crop.Dispose();$source.Dispose()
}
$strip.Save([System.IO.Path]::GetFullPath($OutputPath),[System.Drawing.Imaging.ImageFormat]::Png)
$font.Dispose();$g.Dispose();$strip.Dispose()
Write-Output 'Saved six-column close-up filmstrip at native pixel scale (no resizing).'
