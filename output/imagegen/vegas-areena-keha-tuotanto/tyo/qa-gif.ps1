param(
    [Parameter(Mandatory=$true)][string]$FramesDir,
    [Parameter(Mandatory=$true)][string]$OutFile,
    [int]$Fps=8
)
$ErrorActionPreference='Stop'
$windowsPowerShell=Join-Path $env:WINDIR 'System32/WindowsPowerShell/v1.0/powershell.exe'
if(-not(Test-Path -LiteralPath $windowsPowerShell)){throw 'WindowsPowerShell5.1 is required for the bundled WPF GIF encoder.'}
$inner=Join-Path $PSScriptRoot 'qa-gif-wpf.ps1'
& $windowsPowerShell -NoProfile -STA -ExecutionPolicy Bypass -File $inner -FramesDir $FramesDir -OutFile $OutFile -Fps $Fps
if($LASTEXITCODE -ne 0){throw ('GIF encoder failed: '+$LASTEXITCODE)}
