param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root ("artifacts\bin\" + $Configuration)
[IO.Directory]::CreateDirectory($output) | Out-Null
$sqliteManaged = Join-Path $root '..\work\SkylineRuntime\System.Data.SQLite.dll'
$sqliteNative = Join-Path $root '..\work\SkylineRuntime\SQLite.Interop.dll'
if (!(Test-Path -LiteralPath $sqliteManaged)) { throw "SQLite managed runtime is missing: $sqliteManaged" }
if (!(Test-Path -LiteralPath $sqliteNative)) { throw "SQLite native runtime is missing: $sqliteNative" }
$sources = Get-ChildItem -LiteralPath (Join-Path $root 'src\SkylineNModFilter') -Filter '*.cs' | ForEach-Object { $_.FullName }
$sharedSources = Get-ChildItem -LiteralPath (Join-Path $root 'src\Shared') -Filter '*.cs' | ForEach-Object { $_.FullName }
$args = @('/nologo','/target:winexe','/main:SkylineNModFilter.Program',('/out:' + (Join-Path $output 'SkylineNModFilter.exe')),'/reference:System.Xml.Linq.dll','/reference:System.Windows.Forms.dll','/reference:Microsoft.VisualBasic.dll',('/reference:' + $sqliteManaged)) + $sources + $sharedSources
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' $args
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$collectorSources = Get-ChildItem -LiteralPath (Join-Path $root 'src\SkylineNModFilter.ArgsCollector') -Filter '*.cs' | ForEach-Object { $_.FullName }
$collectorArgs = @('/nologo','/target:library',('/out:' + (Join-Path $output 'SkylineNModFilterArgsCollector.dll')),'/reference:System.Windows.Forms.dll','/reference:System.Core.dll','/reference:Microsoft.VisualBasic.dll') + $collectorSources + $sharedSources
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' $collectorArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$nativeOutput = Join-Path $output 'x64'
[IO.Directory]::CreateDirectory($nativeOutput) | Out-Null
Copy-Item -LiteralPath $sqliteManaged -Destination $output -Force
Copy-Item -LiteralPath $sqliteNative -Destination $nativeOutput -Force
Write-Output (Join-Path $output 'SkylineNModFilter.exe')
