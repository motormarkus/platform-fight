param(
    [string]$Manifest=(Join-Path (Split-Path -Parent $PSScriptRoot) 'native-manifest.json'),
    [string]$Seeds=(Join-Path $PSScriptRoot 'crowd_head_seeds.csv'),
    [string]$Output=(Join-Path $PSScriptRoot 'native-manifest-protected.json'),
    [switch]$TopOnly
)
$ErrorActionPreference='Stop'
$manifestFile=(Resolve-Path -LiteralPath $Manifest).Path
$base=Split-Path -Parent $manifestFile
$config=Get-Content -LiteralPath $manifestFile -Raw|ConvertFrom-Json
$regions=@()
foreach($seed in (Import-Csv -LiteralPath $Seeds)) {
    if($seed.Patch -notmatch '^r([0-9]+)c([0-9]+)') {throw ('Invalid seed patch: '+$seed.Patch)}
    $row=[int]$Matches[1];$col=[int]$Matches[2]
    $topMargin=if($row -gt 0){50}else{20}
    $regions+=@{x=(1224*$col+[int]$seed.X-[int]$seed.Rx-14);y=(860*$row+[int]$seed.Y-[int]$seed.Ry-$topMargin);width=(2*[int]$seed.Rx+28);height=(2*[int]$seed.Ry+$topMargin+20)}
}
$patches=@()
foreach($patch in $config.patches) {
    if($TopOnly -and [int]$patch.y -ne 0){continue}
    if(-not [System.IO.Path]::IsPathRooted($patch.file)){$patch.file=Join-Path $base $patch.file}
    $guards=@()
    foreach($region in $regions) {
        if($region.x -ge ($patch.x+$patch.width) -or ($region.x+$region.width) -le $patch.x -or $region.y -ge ($patch.y+$patch.height) -or ($region.y+$region.height) -le $patch.y){continue}
        $guards+=@{x=($region.x-$patch.x);y=($region.y-$patch.y);width=$region.width;height=$region.height}
    }
    $patch|Add-Member -NotePropertyName avoidRectangles -NotePropertyValue @($guards) -Force
    $patches+=@($patch)
}
$config.patches=$patches
if($TopOnly){$config.height=1024}
$config|ConvertTo-Json -Depth 15|Set-Content -LiteralPath $Output -Encoding utf8
[pscustomobject]@{Output=$Output;Patches=$patches.Count;HeadRectangles=$regions.Count}|ConvertTo-Json -Compress
