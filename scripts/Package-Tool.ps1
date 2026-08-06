$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$workspace = Split-Path -Parent $root
$outputDirectory = Join-Path $workspace 'outputs'
$staging = Join-Path $root 'artifacts\package'
if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
[IO.Directory]::CreateDirectory((Join-Path $staging 'tool-inf')) | Out-Null
[IO.Directory]::CreateDirectory((Join-Path $staging 'x64')) | Out-Null
[IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
& (Join-Path $PSScriptRoot 'Build-Tool.ps1') -Configuration Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Copy-Item -LiteralPath (Join-Path $root 'artifacts\bin\Release\SkylineNModFilter.exe') -Destination $staging
Copy-Item -LiteralPath (Join-Path $root 'artifacts\bin\Release\SkylineNModFilterArgsCollector.dll') -Destination $staging
Copy-Item -LiteralPath (Join-Path $root 'artifacts\bin\Release\System.Data.SQLite.dll') -Destination $staging
Copy-Item -LiteralPath (Join-Path $root 'artifacts\bin\Release\x64\SQLite.Interop.dll') -Destination (Join-Path $staging 'x64')
Copy-Item -LiteralPath (Join-Path $root 'tool-inf\info.properties') -Destination (Join-Path $staging 'tool-inf')
Copy-Item -LiteralPath (Join-Path $root 'tool-inf\NModFilter.properties') -Destination (Join-Path $staging 'tool-inf')
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination $staging
Copy-Item -LiteralPath (Join-Path $root 'THIRD_PARTY_NOTICES.md') -Destination $staging
$package = Join-Path $outputDirectory 'SkylineNModFilter-1.5.0.zip'
if (Test-Path -LiteralPath $package) { Remove-Item -LiteralPath $package -Force }
Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $package -CompressionLevel Optimal
Write-Output $package
