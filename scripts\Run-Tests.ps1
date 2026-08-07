param(
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputDirectory = Join-Path $projectRoot ("artifacts\tests\" + $Configuration)
[IO.Directory]::CreateDirectory($outputDirectory) | Out-Null

$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$testSources = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'tests\SkylineNModFilter.Tests') -Filter '*.cs' | ForEach-Object { $_.FullName }
$productionDirectory = Join-Path $projectRoot 'src\SkylineNModFilter'
$productionSources = @()
if (Test-Path -LiteralPath $productionDirectory) {
    $productionSources = Get-ChildItem -LiteralPath $productionDirectory -Filter '*.cs' | ForEach-Object { $_.FullName }
}
$sharedDirectory = Join-Path $projectRoot 'src\Shared'
$sharedSources = @()
if (Test-Path -LiteralPath $sharedDirectory) {
    $sharedSources = Get-ChildItem -LiteralPath $sharedDirectory -Filter '*.cs' | ForEach-Object { $_.FullName }
}

$arguments = @(
    '/nologo',
    '/target:exe',
    '/main:SkylineNModFilter.Tests.Program',
    ('/out:' + (Join-Path $outputDirectory 'SkylineNModFilter.Tests.exe')),
    '/reference:System.Xml.Linq.dll'
    '/reference:System.Windows.Forms.dll'
    '/reference:Microsoft.VisualBasic.dll'
    ('/reference:' + (Join-Path $projectRoot '..\work\SkylineRuntime\System.Data.SQLite.dll'))
) + $productionSources + $sharedSources + $testSources

& $compiler $arguments
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Copy-Item -LiteralPath (Join-Path $projectRoot '..\work\SkylineRuntime\System.Data.SQLite.dll') -Destination $outputDirectory -Force
$nativeDirectory = Join-Path $outputDirectory 'x64'
[IO.Directory]::CreateDirectory($nativeDirectory) | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot '..\work\SkylineRuntime\SQLite.Interop.dll') -Destination $nativeDirectory -Force

& (Join-Path $outputDirectory 'SkylineNModFilter.Tests.exe')
exit $LASTEXITCODE
