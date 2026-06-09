param(
    [string]$InventoryPath = "artifacts/security/dotnet-package-inventory.json",
    [string]$OutputPath = "artifacts/security/sbom.spdx.json"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $InventoryPath)) {
    throw "Dependency inventory not found: $InventoryPath"
}

$inventory = Get-Content -LiteralPath $InventoryPath -Raw | ConvertFrom-Json
$packagesByKey = [ordered]@{}

foreach ($project in @($inventory.projects)) {
    foreach ($framework in @($project.frameworks)) {
        $allPackages = @()
        if ($framework.PSObject.Properties.Name -contains "topLevelPackages") {
            $allPackages += @($framework.topLevelPackages)
        }

        if ($framework.PSObject.Properties.Name -contains "transitivePackages") {
            $allPackages += @($framework.transitivePackages)
        }

        foreach ($package in $allPackages) {
            if (-not $package.id -or -not $package.resolvedVersion) {
                continue
            }

            $key = "$($package.id.ToLowerInvariant())@$($package.resolvedVersion)"
            if (-not $packagesByKey.Contains($key)) {
                $packagesByKey[$key] = [pscustomobject]@{
                    id = [string]$package.id
                    version = [string]$package.resolvedVersion
                }
            }
        }
    }
}

function New-SpdxId {
    param([string]$PackageId, [string]$Version)

    $safe = "$PackageId-$Version" -replace "[^A-Za-z0-9\.\-]", "-"
    return "SPDXRef-Package-$safe"
}

$packages = @()
$relationships = @()

foreach ($entry in $packagesByKey.Values) {
    $spdxId = New-SpdxId -PackageId $entry.id -Version $entry.version
    $packages += [pscustomobject]@{
        name = $entry.id
        SPDXID = $spdxId
        versionInfo = $entry.version
        downloadLocation = "NOASSERTION"
        filesAnalyzed = $false
        supplier = "NOASSERTION"
        externalRefs = @(
            [pscustomobject]@{
                referenceCategory = "PACKAGE-MANAGER"
                referenceType = "purl"
                referenceLocator = "pkg:nuget/$($entry.id)@$($entry.version)"
            }
        )
    }

    $relationships += [pscustomobject]@{
        spdxElementId = "SPDXRef-DOCUMENT"
        relationshipType = "DESCRIBES"
        relatedSpdxElement = $spdxId
    }
}

$createdAt = [DateTimeOffset]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
$namespaceTimestamp = [DateTimeOffset]::UtcNow.ToString("yyyyMMddHHmmss")
$document = [pscustomobject]@{
    spdxVersion = "SPDX-2.3"
    dataLicense = "CC0-1.0"
    SPDXID = "SPDXRef-DOCUMENT"
    name = "ai-knowledge-transfer-dotnet-dependencies"
    documentNamespace = "https://github.com/josueadjeck/ai_knowledge_transfer/security/sbom/$namespaceTimestamp"
    creationInfo = [pscustomobject]@{
        created = $createdAt
        creators = @("Tool: scripts/security/generate-spdx-sbom.ps1")
    }
    packages = @($packages)
    relationships = @($relationships)
}

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$document | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputPath -Encoding UTF8

if ($packages.Count -eq 0) {
    throw "SPDX SBOM generation produced no packages."
}

Write-Host "Generated SPDX SBOM with $($packages.Count) unique package(s): $OutputPath"
