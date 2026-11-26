param(
    [Parameter(Mandatory=$true)][string]$Manifest,
    [string]$OutDir,
    [switch]$AllowUncovered,
    [switch]$LevelEmptySurfaces,
    [switch]$FeatherEmptySurfaces
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
# Load forwarded GDI assemblies before compiling against this runtime's drawing implementation.
$drawingProbe=[System.Drawing.Bitmap]::new(1,1)
$drawingGraphics=[System.Drawing.Graphics]::FromImage($drawingProbe)
$drawingGraphics.Dispose();$drawingProbe.Dispose()
$drawingReferences=@([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { -not $_.IsDynamic -and $_.GetName().Name -match '^(System\.Drawing|System\.Private\.Windows|System\.Windows\.Extensions)' } | ForEach-Object {$_.Location} | Select-Object -Unique)
$standardReferences=@(Get-ChildItem -LiteralPath (Join-Path $PSHOME 'ref') -Filter '*.dll' | Where-Object {$_.Name -notmatch '^System\.Drawing'} | Select-Object -ExpandProperty FullName)
if (-not ('ArenaNativeQuilter' -as [type])) {
    Add-Type -ReferencedAssemblies @($drawingReferences+$standardReferences) -TypeDefinition (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ArenaNativeQuilter.cs') -Raw)
}
$manifestFile=(Resolve-Path -LiteralPath $Manifest).Path
$manifestDirectory=Split-Path -Parent $manifestFile
$config=Get-Content -LiteralPath $manifestFile -Raw | ConvertFrom-Json
if (-not $OutDir) {$OutDir=Join-Path $manifestDirectory 'native-base'}
$outputPath=[System.IO.Path]::GetFullPath($OutDir)
$patches=[System.Collections.Generic.List[ArenaNativePatch]]::new()
foreach ($item in $config.patches) {
    $patch=[ArenaNativePatch]::new()
    $patch.File=if ([System.IO.Path]::IsPathRooted($item.file)) {$item.file} else {Join-Path $manifestDirectory $item.file}
    $patch.X=[int]$item.x;$patch.Y=[int]$item.y
    if ($null -ne $item.width) {$patch.Width=[int]$item.width}
    if ($null -ne $item.height) {$patch.Height=[int]$item.height}
    if ($null -ne $item.cropX) {$patch.CropX=[int]$item.cropX}
    if ($null -ne $item.cropY) {$patch.CropY=[int]$item.cropY}
    if ($item.avoidRectangles) {
        $rectangles=[System.Collections.Generic.List[System.Drawing.Rectangle]]::new()
        foreach ($r in $item.avoidRectangles) {$rectangles.Add([System.Drawing.Rectangle]::new([int]$r.x,[int]$r.y,[int]$r.width,[int]$r.height))}
        $patch.AvoidRectangles=$rectangles.ToArray()
    }
    $patches.Add($patch)
}
$width=if ($config.width) {[int]$config.width} else {15000}
$height=if ($config.height) {[int]$config.height} else {1884}
$frameWidth=if ($config.frameWidth) {[int]$config.frameWidth} elseif ($config.tileWidth) {[int]$config.tileWidth} else {5000}
$report=[ArenaNativeQuilter]::Compose($patches.ToArray(),$width,$height,$frameWidth,$outputPath,[bool]$AllowUncovered,[bool]$LevelEmptySurfaces,[bool]$FeatherEmptySurfaces)
$report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $outputPath 'provenance.json') -Encoding utf8
[pscustomobject]@{Output=$outputPath;Width=$report.width;Height=$report.height;Panels=$report.panelCount;NativePatches=$report.nativePatches.Count;UncoveredPixels=$report.uncoveredPixels;SourcePixelsResampled=$report.sourcePixelsResampled} | ConvertTo-Json -Compress
