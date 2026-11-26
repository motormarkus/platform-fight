param(
    [string]$Heads=(Join-Path $PSScriptRoot 'crowd_head_seeds.csv'),
    [string]$Hands=(Join-Path $PSScriptRoot 'crowd_hand_seeds.csv'),
    [string]$Output=(Join-Path $PSScriptRoot 'crowd-regions-preliminary.json'),
    [string]$MasterPath,
    [switch]$NoChest
)
$ErrorActionPreference='Stop'
$people=[System.Collections.Generic.List[object]]::new()
$aliases=@{}
$records=Import-Csv -LiteralPath $Heads
foreach($row in $records) {
    if($row.Patch -notmatch '^r(\d)c(\d+)$'){throw ('Invalid patch'+$row.Patch)}
    $originX=1224*[int]$Matches[2];$originY=860*[int]$Matches[1]
    $x=$originX+[double]$row.X;$y=$originY+[double]$row.Y
    $duplicate=$null
    foreach($person in $people) {if([math]::Abs($person.headX-$x) -lt 55 -and [math]::Abs($person.headY-$y) -lt55){$duplicate=$person;break}}
    if($duplicate){$aliases[$row.Patch+'/'+$row.Person]=$duplicate;$duplicate.samples.Add($row);continue}
    $regions=@([ordered]@{kind='head';ellipse=@($x,$y,[double]$row.Rx,[double]$row.Ry);amplitudeX=2.0;amplitudeY=2.2;rotationDegrees=0.15;feather=8})
    if(-not $NoChest){
        $chestY=$y+[double]$row.Ry*1.8+15
        $regions+=([ordered]@{kind='chest';ellipse=@($x,$chestY,([double]$row.Rx*1.05),([double]$row.Ry*0.43));amplitudeX=0.7;amplitudeY=1.2;rotationDegrees=0.07;feather=9})
    }
    $person=[pscustomobject]@{id=($row.Patch+'-'+$row.Person);headX=$x;headY=$y;sourcePatch=$row.Patch;sourcePerson=$row.Person;regions=$regions;samples=[System.Collections.Generic.List[object]]::new()}
    $person.samples.Add($row)
    $people.Add($person)
    $aliases[$row.Patch+'/'+$row.Person]=$person
}
$master=$null;$nativeCache=@{}
function Get-NativeDifference([string]$patch,[double]$localX,[double]$localY) {
    if(-not $master){return 0.0}
    if($patch -notmatch '^r(\d)c(\d+)$'){return [double]::PositiveInfinity}
    $px=1224*[int]$Matches[2];$py=860*[int]$Matches[1]
    if(-not$nativeCache.ContainsKey($patch)){$nativeCache[$patch]=[System.Drawing.Bitmap]::new((Join-Path (Split-Path -Parent $PSScriptRoot) ('natiivit/'+$patch+'.png')))}
    $native=$nativeCache[$patch];$difference=0.0;$samples=0
    foreach($dy in @(-8,-4,0,4,8)){foreach($dx in @(-8,-4,0,4,8)){
        $nx=[int][math]::Round($localX+$dx);$ny=[int][math]::Round($localY+$dy);$mx=$px+$nx;$my=$py+$ny
        if($nx -lt 0 -or $ny -lt 0 -or $nx -ge $native.Width -or $ny -ge $native.Height -or $mx -lt 0 -or $my -lt 0 -or $mx -ge $master.Width -or $my -ge $master.Height){continue}
        $a=$native.GetPixel($nx,$ny);$b=$master.GetPixel($mx,$my)
        $difference += [math]::Abs([int]$a.R-$b.R)+[math]::Abs([int]$a.G-$b.G)+[math]::Abs([int]$a.B-$b.B);$samples+=3
    }}
    if($samples -eq 0){return [double]::PositiveInfinity};return $difference/$samples
}
if($MasterPath){
    Add-Type -AssemblyName System.Drawing
    $master=[System.Drawing.Bitmap]::new((Resolve-Path -LiteralPath $MasterPath).Path)
    foreach($person in $people){
        if($person.samples.Count-lt2){continue}
        $best=$person.samples[0];$bestDifference=[double]::PositiveInfinity
        foreach($sample in $person.samples){$difference=Get-NativeDifference $sample.Patch ([double]$sample.X) ([double]$sample.Y);if($difference-lt$bestDifference){$best=$sample;$bestDifference=$difference}}
        $null=$best.Patch -match '^r(\d)c(\d+)$';$newX=1224*[int]$Matches[2]+[double]$best.X;$newY=860*[int]$Matches[1]+[double]$best.Y
        $dx=$newX-$person.headX;$dy=$newY-$person.headY
        foreach($region in $person.regions){$region.ellipse[0]+=$dx;$region.ellipse[1]+=$dy}
        $person.regions[0].ellipse=@($newX,$newY,[double]$best.Rx,[double]$best.Ry)
        $person.headX=$newX;$person.headY=$newY;$person.sourcePatch=$best.Patch;$person.sourcePerson=$best.Person
    }
}
if(Test-Path -LiteralPath $Hands){foreach($hand in (Import-Csv -LiteralPath $Hands)) {
    if($hand.Patch -notmatch '^r(\d)c(\d+)$'){continue}
    $originX=1224*[int]$Matches[2];$originY=860*[int]$Matches[1]
    $person=$aliases[$hand.Patch+'/'+$hand.Person]
    if(-not $person){continue}
    if($master -and (Get-NativeDifference $hand.Patch ([double]$hand.X) ([double]$hand.Y))-gt4){continue}
    $person.regions+=([ordered]@{kind='hand';ellipse=@(($originX+[double]$hand.X),($originY+[double]$hand.Y),[double]$hand.Rx,[double]$hand.Ry);amplitudeX=1.6;amplitudeY=1.7;rotationDegrees=0.2;feather=6;phaseOffset=0.4})
}}
$masterValidated=[bool]$master
if($master){$master.Dispose()};foreach($bitmap in $nativeCache.Values){$bitmap.Dispose()}
$finalPeople=@($people | ForEach-Object {[ordered]@{id=$_.id;regions=$_.regions}})
# Conservative fixed foreground rope exclusion bands; inspect against finished master.
$excluded=@(
    @(@(5900,745),@(14500,745),@(14500,781),@(5900,781)),
    @(@(5900,945),@(14500,945),@(14500,987),@(5900,987)),
    @(@(5900,1133),@(14500,1133),@(14500,1177),@(5900,1177))
)
[ordered]@{canvasWidth=15000;canvasHeight=1884;reviewStatus=if($masterValidated){'Native source selection refined against final master; inspect mask preview before render.'}else{'Preliminary manual native-patch annotations; inspect final master before render.'};people=$finalPeople;excludePolygons=$excluded} | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $Output -Encoding utf8
Write-Output "Wrote$($people.Count) distinct spectators with head/chest/hand interior regions."
