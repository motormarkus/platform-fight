param(
    [Parameter(Mandatory=$true)][string]$BasePath,
    [Parameter(Mandatory=$true)][string]$RegionsPath,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [int]$TileWidth=5000,
    [int]$FrameCount=6,
    [int]$FrameDurationMs=200,
    [switch]$MaskPreviewOnly,
    [switch]$SavePanoramaFrames
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
# Force the platform drawing assemblies to load before compiling the helper.
$probe=[System.Drawing.Bitmap]::new(1,1)
$probeGraphics=[System.Drawing.Graphics]::FromImage($probe)
$probeGraphics.Clear([System.Drawing.Color]::Black)
$probeGraphics.Dispose();$probe.Dispose()
$drawingReferences=@([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { -not $_.IsDynamic -and $_.GetName().Name -match '^(System\.Drawing|System\.Private\.Windows|System\.Windows\.Extensions)' } | ForEach-Object { $_.Location } | Select-Object -Unique)
$standardReferences=@(Get-ChildItem -LiteralPath (Join-Path $PSHOME 'ref') -Filter '*.dll' | Where-Object {$_.Name -notmatch '^System\.Drawing'} | Select-Object -ExpandProperty FullName)
if(-not ('CrowdIdleRenderer' -as [type])){Add-Type -ReferencedAssemblies @($drawingReferences+$standardReferences) -Path (Join-Path $PSScriptRoot 'crowd_idle.cs')}
$configuration=Get-Content -LiteralPath $RegionsPath -Raw | ConvertFrom-Json
$regions=[System.Collections.Generic.List[CrowdRegion]]::new()
$exclusions=[System.Collections.Generic.List[System.Drawing.PointF[]]]::new()
$personNumber=0
foreach($person in $configuration.people) {
    $phase=if($null -ne $person.phase){[double]$person.phase}else{($personNumber*2.399963229728653) % (2*[math]::PI)}
    $originX=0.0;$originY=0.0
    if($person.origin){$originX=[double]$person.origin[0];$originY=[double]$person.origin[1]}
    $regionNumber=0
    foreach($item in $person.regions) {
        $r=[CrowdRegion]::new();$r.Id=([string]$person.id)+'/'+$regionNumber;$r.Kind=[string]$item.kind;$r.Phase=$phase
        if($item.ellipse) {
            $e=$item.ellipse;$r.Polygon=[CrowdIdleRenderer]::Ellipse(([double]$e[0]+$originX),([double]$e[1]+$originY),[double]$e[2],[double]$e[3])
        } else {
            $points=[System.Collections.Generic.List[System.Drawing.PointF]]::new()
            foreach($p in $item.polygon){$points.Add([System.Drawing.PointF]::new(([single]$p[0]+$originX),([single]$p[1]+$originY)))}
            $r.Polygon=$points.ToArray()
        }
        if($r.Kind -eq 'chest'){$r.AmplitudeX=1.0;$r.AmplitudeY=1.3;$r.RotationDegrees=0.08;$r.Feather=12}
        if($r.Kind -eq 'hand'){$r.AmplitudeX=2.7;$r.AmplitudeY=2.0;$r.RotationDegrees=0.35;$r.Feather=7}
        if($null -ne $item.amplitudeX){$r.AmplitudeX=[double]$item.amplitudeX}
        if($null -ne $item.amplitudeY){$r.AmplitudeY=[double]$item.amplitudeY}
        if($null -ne $item.rotationDegrees){$r.RotationDegrees=[double]$item.rotationDegrees}
        if($null -ne $item.feather){$r.Feather=[double]$item.feather}
        if($null -ne $item.phaseOffset){$r.Phase += [double]$item.phaseOffset}
        $regions.Add($r);$regionNumber++
    }
    $personNumber++
}
foreach($excluded in $configuration.excludePolygons) {
    $points=[System.Collections.Generic.List[System.Drawing.PointF]]::new()
    foreach($p in $excluded){$points.Add([System.Drawing.PointF]::new([single]$p[0],[single]$p[1]))}
    $exclusions.Add($points.ToArray())
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$outputAbsolute=(Resolve-Path -LiteralPath $OutputDirectory).Path
$renderer=[CrowdIdleRenderer]::new((Resolve-Path -LiteralPath $BasePath).Path,$regions,$exclusions)
try {
    if($renderer.Width -ne 3*$TileWidth){throw "Expected three panels of width$TileWidth; actual master width=$($renderer.Width)."}
    if($configuration.canvasWidth -and $renderer.Width -ne $configuration.canvasWidth){throw 'Annotation width differs from master.'}
    if($configuration.canvasHeight -and $renderer.Height -ne $configuration.canvasHeight){throw 'Annotation height differs from master.'}
    $names=@('01-katsomo','02-keha-vasen','03-keha-oikea')
    $renderer.SaveMasks((Join-Path $outputAbsolute 'masks'),$TileWidth,$names)
    $renderer.SaveAnnotation((Join-Path $outputAbsolute 'crowd-mask-annotation.png'),4000)
    $renderer.SavePanelAnnotations($outputAbsolute,$TileWidth,2500)
    $regionMap=@();for($ri=0;$ri -lt$regions.Count;$ri++){$regionMap+=[pscustomobject]@{index=$ri+1;id=$regions[$ri].Id;kind=$regions[$ri].Kind;bounds=@($regions[$ri].Bounds.X,$regions[$ri].Bounds.Y,$regions[$ri].Bounds.Width,$regions[$ri].Bounds.Height)}}
    $regionMap | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outputAbsolute 'crowd-region-map.json') -Encoding utf8
    if($MaskPreviewOnly){Write-Output "Mask preview exported: $($renderer.ActiveCount) active pixels across$($regions.Count) regions.";return}
    $changes=@()
    for($frame=0;$frame -lt $FrameCount;$frame++) {
        $changed=$renderer.Render($frame,$FrameCount,$outputAbsolute,$TileWidth,$names,$SavePanoramaFrames.IsPresent)
        $changes += [pscustomobject]@{Frame=$frame+1;ChangedAgainstBase=$changed}
        Write-Output "Exported idle frame$($frame+1)/$FrameCount; changed pixels=$changed."
    }
    $panels=@()
    foreach($name in $names) {
        $paths=@();$overlays=@()
        for($frame=1;$frame -le $FrameCount;$frame++) {
            $paths+=Join-Path $outputAbsolute ('frames/'+$name+'/'+$name+'-frame-'+$frame.ToString('00')+'.png')
            $overlays+=Join-Path $outputAbsolute ('crowd/'+$name+'/crowd-frame-'+$frame.ToString('00')+'.png')
        }
        $panels += [pscustomobject]@{name=$name;frames=$paths;mask=(Join-Path $outputAbsolute ('masks/'+$name+'-mask.png'));maskMode='red';overlays=$overlays}
    }
    $manifest=[ordered]@{base=(Resolve-Path -LiteralPath $BasePath).Path;canvasWidth=$renderer.Width;canvasHeight=$renderer.Height;panelWidth=$TileWidth;frameCount=$FrameCount;frameDurationMs=$FrameDurationMs;loop=$true;motion='Conservative interior mesh deformation; original contours held fixed; no cleanplate required';people=$personNumber;regions=$regions.Count;activeMaskPixels=$renderer.ActiveCount;panels=$panels;changes=$changes}
    if($SavePanoramaFrames){$manifest.panoramaFrames=@(1..$FrameCount | ForEach-Object {Join-Path $outputAbsolute ('panorama-frames/panorama-frame-'+$_.ToString('00')+'.png')})}
    $manifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $outputAbsolute 'qa-manifest.json') -Encoding utf8
    $configuration | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath (Join-Path $outputAbsolute 'crowd-regions.json') -Encoding utf8
} finally {$renderer.Dispose()}
