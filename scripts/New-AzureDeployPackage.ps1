param(
    [string]$ProjectPath = "src/SignalIntelligenceWorkspace/SignalIntelligenceWorkspace.csproj",
    [string]$Configuration = "Release",
    [string]$OutputRoot = "tmp/azure-publish/manual",
    [string]$PackageName = "signal-intelligence-workspace.zip"
)

$ErrorActionPreference = "Stop"

$publishDir = Join-Path $OutputRoot "publish"
$zipPath = Join-Path $OutputRoot $PackageName

if (Test-Path $OutputRoot) {
    Remove-Item -LiteralPath $OutputRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $publishDir | Out-Null

dotnet publish $ProjectPath -c $Configuration -o $publishDir
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    $root = (Resolve-Path $publishDir).Path.TrimEnd("\")
    Get-ChildItem -Path $publishDir -Recurse -File | ForEach-Object {
        $relative = $_.FullName.Substring($root.Length + 1).Replace("\", "/")
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zip,
            $_.FullName,
            $relative,
            [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
}
finally {
    $zip.Dispose()
}

$requiredEntries = @(
    "wwwroot/app.css",
    "wwwroot/SignalIntelligenceWorkspace.styles.css",
    "wwwroot/public-feedback.js",
    "wwwroot/public-home-flow-map.js",
    "wwwroot/_content/Telerik.UI.for.Blazor/css/kendo-theme-fluent/all.css",
    "wwwroot/_framework/blazor.web.js"
)

$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entries = @{}
    foreach ($entry in $zip.Entries) {
        if ($entry.FullName.Contains("\")) {
            throw "Invalid Windows-style zip entry path: $($entry.FullName)"
        }

        $entries[$entry.FullName] = $entry
    }

    foreach ($requiredEntry in $requiredEntries) {
        if (-not $entries.ContainsKey($requiredEntry)) {
            throw "Missing required publish asset: $requiredEntry"
        }

        if ($entries[$requiredEntry].Length -le 0) {
            throw "Required publish asset is empty: $requiredEntry"
        }
    }
}
finally {
    $zip.Dispose()
}

Get-Item $zipPath | Select-Object FullName, Length
