param([Parameter(Mandatory=$true)][string]$Package)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $Package))
try {
    $names = @($zip.Entries | ForEach-Object { $_.FullName.Replace('\','/') })
    foreach ($required in @('SkylineNModFilter.exe','SkylineNModFilterArgsCollector.dll','System.Data.SQLite.dll','x64/SQLite.Interop.dll','LICENSE','THIRD_PARTY_NOTICES.md','tool-inf/info.properties','tool-inf/NModFilter.properties')) {
        if ($names -notcontains $required) { throw "Package is missing $required" }
    }
    if (($names | Where-Object { $_ -eq 'tool-inf/info.properties' }).Count -ne 1) { throw 'Package must contain exactly one info.properties.' }
    if ($names | Where-Object { $_.StartsWith('/') -or $_ -match '^[A-Za-z]:' -or $_ -match '(^|/)\.\.(/|$)' }) { throw 'Package contains an unsafe path.' }
} finally { $zip.Dispose() }

$inspectionDirectory = Join-Path ([IO.Path]::GetTempPath()) ('SkylineNModFilter-package-' + [Guid]::NewGuid().ToString('N'))
try {
    [IO.Directory]::CreateDirectory($inspectionDirectory) | Out-Null
    Expand-Archive -LiteralPath $Package -DestinationPath $inspectionDirectory
    Add-Type -AssemblyName System.Windows.Forms
    $assembly = [Reflection.Assembly]::Load([IO.File]::ReadAllBytes((Join-Path $inspectionDirectory 'SkylineNModFilterArgsCollector.dll')))
    $method = $assembly.GetType('SkylineNModFilter.ArgsCollector.Collector').GetMethod('CollectArgs', [Reflection.BindingFlags]'Public,Static')
    if ($null -eq $method) { throw 'Collector is missing public static CollectArgs.' }
    $parameters = @($method.GetParameters() | ForEach-Object { $_.ParameterType.FullName })
    if ($method.ReturnType.FullName -ne 'System.String[]' -or ($parameters -join ',') -ne 'System.Windows.Forms.Control,System.String,System.String[]') {
        throw 'Collector CollectArgs signature is incompatible with Skyline.'
    }
} finally {
    if (Test-Path -LiteralPath $inspectionDirectory) { Remove-Item -LiteralPath $inspectionDirectory -Recurse -Force }
}
Write-Output 'PASS: package structure'
