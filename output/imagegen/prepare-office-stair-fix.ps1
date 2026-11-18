$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'WorksiteDetail.cs') -ReferencedAssemblies System.Drawing
$folder=Join-Path $PSScriptRoot 'vegas-konttori-pomon-toimisto'
[WorksiteDetail]::Crop((Join-Path $folder 'pomon-toimisto-kokonaisuus-10240x1844.png'),(Join-Path $folder 'portaikon-korjaus-viite.png'),8870,500,1370,1180)
