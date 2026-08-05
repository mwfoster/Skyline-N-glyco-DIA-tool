param(
    [Parameter(Mandatory = $true)]
    [string]$SkylineToolDll,

    [Parameter(Mandatory = $true)]
    [string]$SkylineCommand,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

$toolPath = (Resolve-Path -LiteralPath $SkylineToolDll).Path
$commandPath = (Resolve-Path -LiteralPath $SkylineCommand).Path
$resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
$outputDirectory = [IO.Path]::GetDirectoryName($resolvedOutput)
if (-not [string]::IsNullOrEmpty($outputDirectory)) {
    [IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
}

$assembly = [Reflection.Assembly]::LoadFrom($toolPath)
$clientType = $assembly.GetType('SkylineTool.SkylineToolClient', $true)
$methodLines = $clientType.GetMethods(
    [Reflection.BindingFlags]'Public,Instance,DeclaredOnly'
) | Sort-Object Name | ForEach-Object { $_.ToString() }

$helpLines = @(& $commandPath --help 2>&1 | ForEach-Object { $_.ToString() })
if ($LASTEXITCODE -ne 0) {
    throw "Skyline command help failed with exit code $LASTEXITCODE."
}

$requiredMethods = @('GetReport', 'DeleteElements')
$requiredSwitches = @(
    '--in=',
    '--out=',
    '--refine-min-peptides=',
    '--pep-max-variable-mods='
)

$missing = [Collections.Generic.List[string]]::new()
foreach ($method in $requiredMethods) {
    if (-not ($methodLines | Select-String -SimpleMatch $method -Quiet)) {
        $missing.Add("Tool Service method: $method")
    }
}
foreach ($argument in $requiredSwitches) {
    if (-not ($helpLines | Select-String -SimpleMatch $argument -Quiet)) {
        $missing.Add("SkylineCmd switch: $argument")
    }
}

$version = [Diagnostics.FileVersionInfo]::GetVersionInfo($commandPath).ProductVersion
$inventory = @(
    "Skyline command version: $version"
    "SkylineTool.dll: $toolPath"
    "Skyline command: $commandPath"
    ''
    'SkylineToolClient public methods:'
) + $methodLines + @(
    ''
    'Required SkylineCmd switches:'
) + $requiredSwitches

[IO.File]::WriteAllLines($resolvedOutput, $inventory, [Text.UTF8Encoding]::new($false))

if ($missing.Count -gt 0) {
    throw "Missing required Skyline capabilities: $($missing -join '; ')"
}

Write-Output "Skyline compatibility inventory written to $resolvedOutput"
