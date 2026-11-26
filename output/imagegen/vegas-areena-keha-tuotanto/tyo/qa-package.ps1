param(
    [Parameter(Mandatory=$true)][string]$AnimationManifest,
    [Parameter(Mandatory=$true)][string]$QaReport,
    [Parameter(Mandatory=$true)][string]$NativeProvenance,
    [Parameter(Mandatory=$true)][string]$OutDir,
    [string]$NativeBaseDirectory,
    [switch]$IncludeOverlays,
    [switch]$Zip
)
$ErrorActionPreference='Stop'
$manifestPath=(Resolve-Path -LiteralPath $AnimationManifest).Path
$manifestDir=Split-Path -Parent $manifestPath
$manifest=Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$qa=Get-Content -LiteralPath $QaReport -Raw | ConvertFrom-Json
if(-not $qa.passed){throw 'Package requires a passing final QA audit.'}
if($qa.expectedPanelWidth -ne 5000 -or $qa.expectedPanelHeight -ne 1884){throw 'Package requires final5000x1884 QA dimensions.'}
if(Test-Path -LiteralPath $OutDir){throw 'Package destination already exists; choose a new version.'}
New-Item -ItemType Directory -Path $OutDir | Out-Null
$outAbsolute=(Resolve-Path -LiteralPath $OutDir).Path
function Resolve-PackageSource([string]$Path){if([System.IO.Path]::IsPathRooted($Path)){return $Path};return Join-Path $manifestDir $Path}
$portablePanels=@()
foreach($panel in $manifest.panels){
    $name=[string]$panel.name
    $panelDir=Join-Path $outAbsolute $name
    New-Item -ItemType Directory -Path $panelDir | Out-Null
    $framePaths=@()
    foreach($file in $panel.frames){$source=Resolve-PackageSource ([string]$file);$fileName=[System.IO.Path]::GetFileName($source);Copy-Item -LiteralPath $source -Destination (Join-Path $panelDir $fileName);$framePaths+=($name+'/'+$fileName)}
    $maskName='yleisomaski.png';Copy-Item -LiteralPath (Resolve-PackageSource ([string]$panel.mask)) -Destination (Join-Path $panelDir $maskName)
    $overlays=@()
    if($IncludeOverlays -and $panel.overlays){$overlayDir=Join-Path $panelDir 'yleisokerros';New-Item -ItemType Directory -Path $overlayDir | Out-Null;foreach($file in $panel.overlays){$source=Resolve-PackageSource ([string]$file);$fileName=[System.IO.Path]::GetFileName($source);Copy-Item -LiteralPath $source -Destination (Join-Path $overlayDir $fileName);$overlays+=($name+'/yleisokerros/'+$fileName)}}
    $portablePanels += [pscustomobject]@{name=$name;frames=$framePaths;mask=($name+'/'+$maskName);maskMode=$panel.maskMode;overlays=$overlays}
}
$portable=[ordered]@{canvasWidth=$manifest.canvasWidth;canvasHeight=$manifest.canvasHeight;panelWidth=$manifest.panelWidth;frameCount=6;frameDurationMs=$manifest.frameDurationMs;loop=$true;panels=$portablePanels}
$portable | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $outAbsolute 'qa-manifest.json') -Encoding utf8
Copy-Item -LiteralPath $NativeProvenance -Destination (Join-Path $outAbsolute 'provenance.json')
New-Item -ItemType Directory -Path (Join-Path $outAbsolute 'qa') | Out-Null
Copy-Item -LiteralPath $QaReport -Destination (Join-Path $outAbsolute 'qa/audit.json')
if($NativeBaseDirectory){
    $baseOut=Join-Path $outAbsolute 'pohjat'
    New-Item -ItemType Directory -Path $baseOut | Out-Null
    foreach($baseFile in (Get-ChildItem -LiteralPath $NativeBaseDirectory -Filter 'static-??-5000x1884.png' -File)){Copy-Item -LiteralPath $baseFile.FullName -Destination (Join-Path $baseOut $baseFile.Name)}
}
$readme=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'qa-lue-minut-pohja.txt') -Raw
$readme=$readme.Replace('kunkin kuvan frame-kansiossa','kunkin kuvan omassa kansiossa')
$readme | Set-Content -LiteralPath (Join-Path $outAbsolute 'LUE-MINUT.txt') -Encoding utf8
$zipPath=$null
if($Zip){
    $zipPath=$outAbsolute+'.zip'
    if(Test-Path -LiteralPath $zipPath){throw 'Zip already exists.'}
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($outAbsolute,$zipPath,[System.IO.Compression.CompressionLevel]::Fastest,$false)
}
[pscustomobject]@{Folder=$outAbsolute;Panels=$portablePanels.Count;FullRgbFrames=($portablePanels.Count*6);OverlaysIncluded=$IncludeOverlays.IsPresent;Zip=$zipPath} | ConvertTo-Json -Compress
