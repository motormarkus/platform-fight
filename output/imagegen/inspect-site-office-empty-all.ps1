$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'WorksiteDetail.cs') -ReferencedAssemblies System.Drawing
$folder=Join-Path $PSScriptRoot 'vegas-tyomaakonttori-kaikki-ikkunat-tyhjat'
$w=0;$h=0;$d=[WorksiteDetail]::Load((Join-Path $folder 'tyhjennys-natiivi.png'),[ref]$w,[ref]$h)
$d=[WorksiteDetail]::Resize($d,$w,$h,5120,1844);[WorksiteDetail]::Clean($d)
[WorksiteDetail]::Save($d,5120,1844,(Join-Path $folder 'tyhjennys-kohdistettu.png'))
[WorksiteDetail]::Crop((Join-Path $folder 'tyhjennys-kohdistettu.png'),(Join-Path $folder 'kahvat-generoidut.png'),1840,790,250,220)
foreach($y in @(50,110,112,700,850,900,950)){
 $runs=@();$start=-1
 for($x=1500;$x -lt 5120;$x++){
  $clear=$d[($y*5120+$x)*4+3] -lt 32
  if($clear -and $start -lt 0){$start=$x}
  if((!$clear -or $x -eq 5119) -and $start -ge 0){if($x-$start -ge 8){$runs+=('{0}-{1}' -f $start,($x-1))};$start=-1}
 }
 '{0}: {1}' -f $y,($runs -join ', ')
}
