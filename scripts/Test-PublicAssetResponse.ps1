param(
    [string]$BaseUrl = "https://johannafan.com"
)

$ErrorActionPreference = "Stop"

$assets = @(
    "/app.css",
    "/SignalIntelligenceWorkspace.styles.css",
    "/_content/Telerik.UI.for.Blazor/css/kendo-theme-fluent/all.css",
    "/public-feedback.js",
    "/public-home-flow-map.js",
    "/_framework/blazor.web.js"
)

$results = @()
foreach ($asset in $assets) {
    $url = $BaseUrl.TrimEnd("/") + $asset
    $tmp = Join-Path $env:TEMP ([IO.Path]::GetRandomFileName())
    try {
        $curl = curl.exe -L --compressed --max-time 30 -s -o $tmp -w "%{http_code} %{size_download} %{content_type}" $url
        $parts = $curl -split " ", 3
        $httpStatus = [int]$parts[0]
        $downloadSize = [int]$parts[1]
        $fileLength = (Get-Item $tmp).Length
        $contentType = if ($parts.Count -gt 2) { $parts[2] } else { "" }

        $results += [pscustomobject]@{
            Asset = $asset
            Http = $httpStatus
            DownloadSize = $downloadSize
            FileLength = $fileLength
            ContentType = $contentType
        }

        if ($httpStatus -ne 200 -or $downloadSize -le 0 -or $fileLength -le 0) {
            throw "Asset check failed for $asset with HTTP $httpStatus and $downloadSize downloaded bytes."
        }
    }
    finally {
        Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue
    }
}

$results
