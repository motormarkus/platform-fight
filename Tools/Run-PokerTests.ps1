param([string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.18f1\Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $projectRoot
try {
    $monoPath = Join-Path $UnityEditor 'Data\MonoBleedingEdge\bin\mono.exe'
    $compilerPath = Join-Path $UnityEditor 'Data\MonoBleedingEdge\lib\mono\4.5\csc.exe'
    $nunitPath = Join-Path $UnityEditor 'Data\Resources\PackageManager\BuiltInPackages\com.unity.ext.nunit\net40\unity-custom\nunit.framework.dll'
    foreach ($file in @($monoPath, $compilerPath, $nunitPath)) { if (!(Test-Path -LiteralPath $file)) { throw "Missing: $file" } }
    $outputDir = Join-Path $projectRoot 'Temp\PokerTests'
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
    $testExe = Join-Path $outputDir 'PokerTests.exe'
    $compileArgs = @('/nologo', '/target:exe', "/out:$testExe", "/reference:$nunitPath")
    $compileArgs += Get-ChildItem -LiteralPath 'Assets\Scripts\Poker\Core' -Filter '*.cs' | ForEach-Object { $_.FullName }
    $compileArgs += @('Assets\Tests\Poker\PokerCoreTests.cs', 'Tools\PokerTests\Runner.cs')
    & $monoPath $compilerPath @compileArgs
    if ($LASTEXITCODE -ne 0) { throw 'Poker test compilation failed.' }
    Copy-Item -LiteralPath $nunitPath -Destination (Join-Path $outputDir 'nunit.framework.dll') -Force
    & $monoPath $testExe
    if ($LASTEXITCODE -ne 0) { throw 'Poker tests failed.' }
} finally { Pop-Location }
