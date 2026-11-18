$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path @((Join-Path $PSScriptRoot 'WorksiteDetail.cs'),(Join-Path $PSScriptRoot 'SiteCanteenLayers.cs')) -ReferencedAssemblies System.Drawing
$folder=Join-Path $PSScriptRoot 'vegas-tyomaakonttori-ruokala-parallaksi'
$source=Join-Path $PSScriptRoot 'vegas-tyomaakonttori-katu-01\vegas-tyomaakonttori-katu-01-5120x1844.png'
$native=Join-Path $folder 'tausta-natiivi.png'
Copy-Item -LiteralPath 'C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-4d332d90-e99b-4ea6-8176-aa4da1497d77.png' -Destination $native
$w=0;$h=0;$original=[WorksiteDetail]::Load($source,[ref]$w,[ref]$h)
$mask=[SiteCanteenLayers]::Mask($original,$w,$h)
$localMask=[SiteCanteenLayers]::LocalMask($mask,$w,2500,350,2620,800)
$cw=0;$ch=0;$crop=[WorksiteDetail]::Load((Join-Path $folder 'tausta-muokkausviite.png'),[ref]$cw,[ref]$ch)
$nw=0;$nh=0;$room=[WorksiteDetail]::Load($native,[ref]$nw,[ref]$nh)
$room=[WorksiteDetail]::Resize($room,$nw,$nh,2620,800)
# Preserve straight interior geometry; only match the broad colour values.
$tone=[SiteCanteenLayers]::ToneReference($crop,$localMask)
[WorksiteDetail]::MatchTone($tone,$room,2620,800)
# Keep the inpainted room continuous while it moves; copying separate original
# panes back into it would expose seams when the foreground mullions no longer cover them.
$foreground=[SiteCanteenLayers]::ExtractForeground($original,$mask)
$extracted=[byte[]]::new($original.Length)
for($p=0;$p -lt $mask.Length;$p++){if($mask[$p]){[Buffer]::BlockCopy($original,$p*4,$extracted,$p*4,4)}}
[WorksiteDetail]::Save($extracted,5120,1844,(Join-Path $folder 'alkuperainen-ikkunasisalto-5120x1844.png'))
$background=[SiteCanteenLayers]::PlaceBackground($room,2620,800,5120,1844,2500,350)
$padded=[SiteCanteenLayers]::PadBackground($background,5120,1844,256)
$combined=[SiteCanteenLayers]::Composite($foreground,5120,1844,$background,5120,0,0)
$fgPath=Join-Path $folder '01-katukuva-ikkunat-lapinakyvat-5120x1844.png'
$bgPath=Join-Path $folder '02-ruokala-parallaksi-5120x1844.png'
$padPath=Join-Path $folder '03-ruokala-parallaksi-liikevara-5632x1844.png'
[WorksiteDetail]::Save($foreground,5120,1844,$fgPath)
[WorksiteDetail]::Save($background,5120,1844,$bgPath)
[WorksiteDetail]::Save($padded,5632,1844,$padPath)
[WorksiteDetail]::Save($combined,5120,1844,(Join-Path $folder 'yhdistetty-tarkistus.png'))
[WorksiteDetail]::Preview($fgPath,(Join-Path $folder 'etuala-esikatselu.png'),1920,692)
[WorksiteDetail]::Preview($bgPath,(Join-Path $folder 'parallaksi-esikatselu.png'),1920,692)
[WorksiteDetail]::Save($room,2620,800,(Join-Path $folder 'ruokala-tausta-lahikuva.png'))
$checks=[SiteCanteenLayers]::Check($original,$foreground,$combined,$mask)
$moves=@()
foreach($shift in @(-96,96)){
 $moved=[SiteCanteenLayers]::Composite($foreground,5120,1844,$padded,5632,256,$shift)
 $path=Join-Path $folder ('liike-'+$shift+'px.png');[WorksiteDetail]::Save($moved,5120,1844,$path)
 $moves+=[pscustomobject]@{shiftPx=$shift;transparentHoles=[SiteCanteenLayers]::Holes($padded,5632,$mask,5120,1844,256,$shift)}
}
$qa=[pscustomobject]@{width=5120;height=1844;outsideWindowsChangedChannels=$checks[0];extractedWindowPixels=$checks[1];windowPixelsStillOpaque=$checks[2];recomposedDifferentChannels=$checks[3];note='Foreground outside canteen panes is pixel-exact. The continuous room background was reconstructed to remove all mullions and fill occluded areas; visible interior rendering may differ slightly.';parallaxMarginPx=256;motionChecks=$moves}
$qa | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $folder 'tarkistus.json') -Encoding utf8
$qa | Format-List
if($checks[0] -ne 0 -or $checks[2] -ne 0){throw 'Layer verification failed'}
