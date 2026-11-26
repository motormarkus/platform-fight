$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$checkRoot=Join-Path $PSScriptRoot 'compositor-selfcheck'
New-Item -ItemType Directory -Path $checkRoot -Force | Out-Null
$patches=@()
foreach ($y0 in @(0,40)) {foreach ($x0 in @(0,80)) {
    $file=Join-Path $checkRoot ('fixture-'+$x0+'-'+$y0+'.png')
    $bitmap=[System.Drawing.Bitmap]::new(120,60,[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    for($y=0;$y -lt 60;$y++){for($x=0;$x -lt 120;$x++){
        $bitmap.SetPixel($x,$y,[System.Drawing.Color]::FromArgb((($x+$x0)*17)%256,(($y+$y0)*13)%256,(($x+$x0+$y+$y0)*7)%256))
    }}
    $bitmap.Save($file,[System.Drawing.Imaging.ImageFormat]::Png);$bitmap.Dispose()
    $patches+=@{file=$file;x=$x0;y=$y0;width=120;height=60}
}}
$fixtureManifest=Join-Path $checkRoot 'fixture-manifest.json'
@{width=200;height=100;frameWidth=100;patches=$patches}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $fixtureManifest -Encoding utf8
& (Join-Path $PSScriptRoot 'compose-native.ps1') -Manifest $fixtureManifest -OutDir (Join-Path $checkRoot 'good')
$actual=[System.Drawing.Bitmap]::new((Join-Path $checkRoot 'good/static-master-200x100.png'))
$different=0
for($y=0;$y -lt 100;$y++){for($x=0;$x -lt 200;$x++){
    $p=$actual.GetPixel($x,$y)
    if($p.R -ne (($x*17)%256) -or $p.G -ne (($y*13)%256) -or $p.B -ne ((($x+$y)*7)%256)){$different++}
}}
$actual.Dispose()
if($different){throw ('Native pixel reconstruction failed: '+$different)}
$invalid=Get-Content -LiteralPath $fixtureManifest -Raw|ConvertFrom-Json
$invalid.patches[0].width=121
$badManifest=Join-Path $checkRoot 'undersized-manifest.json'
$invalid|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $badManifest -Encoding utf8
$rejected=$false
try{& (Join-Path $PSScriptRoot 'compose-native.ps1') -Manifest $badManifest -OutDir (Join-Path $checkRoot 'bad')|Out-Null}catch{if($_.Exception.Message -match 'too small|upscaling is forbidden'){$rejected=$true}else{throw}}
if(-not $rejected){throw 'Undersized native source was not rejected.'}
[pscustomobject]@{ExactReconstructionDifferentPixels=$different;UpscaleRequestRejected=$rejected;NativeSourceCount=4;OutputWidth=200;OutputHeight=100}|ConvertTo-Json -Compress
