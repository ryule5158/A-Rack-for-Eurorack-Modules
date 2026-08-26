param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$rackCompiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$rackSolidWorks = 'E:\SW2025\SOLIDWORKS'
$rackOutputDirectory = Join-Path $PSScriptRoot 'build'
$rackOutputExecutable = Join-Path $rackOutputDirectory 'BuildRack4Modules.exe'
$rackSldWorksInterop = Join-Path $rackSolidWorks 'SolidWorks.Interop.sldworks.dll'
$rackConstantsInterop = Join-Path $rackSolidWorks 'SolidWorks.Interop.swconst.dll'
$rackCoreSource = Join-Path $PSScriptRoot 'SwCadCore.cs'
$rackGeometrySource = Join-Path $PSScriptRoot 'BuildRack4Modules.cs'

foreach ($rackRequired in @($rackCompiler, $rackSldWorksInterop, $rackConstantsInterop, $rackCoreSource, $rackGeometrySource)) {
    if (-not (Test-Path -LiteralPath $rackRequired)) {
        throw "Missing required SolidWorks 2025 build input: $rackRequired"
    }
}

New-Item -ItemType Directory -Path $rackOutputDirectory -Force | Out-Null

& $rackCompiler `
    /nologo `
    /platform:x64 `
    /target:exe `
    /out:$rackOutputExecutable `
    /reference:$rackSldWorksInterop `
    /reference:$rackConstantsInterop `
    /reference:System.Web.Extensions.dll `
    $rackCoreSource `
    $rackGeometrySource

if ($LASTEXITCODE -ne 0) {
    throw 'The SolidWorks 2025 native CAD generator did not compile.'
}

Copy-Item -LiteralPath $rackSldWorksInterop -Destination $rackOutputDirectory -Force
Copy-Item -LiteralPath $rackConstantsInterop -Destination $rackOutputDirectory -Force

& $rackOutputExecutable $ProjectRoot
if ($LASTEXITCODE -ne 0) {
    throw 'The SolidWorks 2025 native CAD generator did not complete.'
}
