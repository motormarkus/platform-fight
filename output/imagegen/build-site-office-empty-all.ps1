$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path @((Join-Path $PSScriptRoot 'WorksiteDetail.cs'),(Join-Path $PSScriptRoot 'SiteOfficeGlass.cs')) -ReferencedAssemblies System.Drawing
$folder=Join-Path $PSScriptRoot 'vegas-tyomaakonttori-kaikki-ikkunat-tyhjat'
$source=Join-Path $PSScriptRoot 'vegas-tyomaakonttori-ruokala-parallaksi\01-katukuva-ikkunat-lapinakyvat-5120x1844.png'
$w=0;$h=0;$original=[WorksiteDetail]::Load($source,[ref]$w,[ref]$h)
$gw=0;$gh=0;$guide=[WorksiteDetail]::Load((Join-Path $folder 'tyhjennys-kohdistettu.png'),[ref]$gw,[ref]$gh)
$handles=[byte[]]::new(0);$mask=[SiteOfficeGlass]::Mask($original,$guide,$w,$h,[ref]$handles)
$result=[SiteOfficeGlass]::Apply($original,$mask)
$path=Join-Path $folder 'vegas-tyomaakonttori-lasit-tyhjat-5120x1844.png'
[WorksiteDetail]::Save($result,$w,$h,$path)
[WorksiteDetail]::Preview($path,(Join-Path $folder 'esikatselu.png'),1920,692)
[WorksiteDetail]::Crop($path,(Join-Path $folder 'kahvat-tarkistus.png'),1840,790,250,220)
[WorksiteDetail]::Crop($path,(Join-Path $folder 'ovi-tarkistus.png'),1530,460,800,620)
[WorksiteDetail]::Crop($path,(Join-Path $folder 'ylakerta-tarkistus.png'),1500,0,3620,175)
$check=[SiteOfficeGlass]::Check($original,$result,$mask,$handles)
$qa=[pscustomobject]@{width=$w;height=$h;bytes=(Get-Item -LiteralPath $path).Length;changedChannelsOutsideNewGlassMask=$check[0];newlyClearedPixels=$check[1];unclearedMaskPixels=$check[2];changedHandleChannels=$check[3];source=$source;output=$path}
$qa | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $folder 'tarkistus.json') -Encoding utf8
$qa | Format-List
if($check[0] -ne 0 -or $check[2] -ne 0 -or $check[3] -ne 0){throw 'Pixel preservation check failed'}
