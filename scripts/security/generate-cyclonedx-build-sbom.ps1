param(
    [string]$PackageInventoryPath = "artifacts/security/dotnet-package-inventory.json",
    [string]$ArtifactInventoryPath = "artifacts/security/build-artifact-inventory.json",
    [string]$OutputPath = "artifacts/security/cyclonedx-build-sbom.json"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $PackageInventoryPath)) {
    throw "Package inventory not found: $PackageInventoryPath"
}

if (-not (Test-Path -LiteralPath $ArtifactInventoryPath)) {
    throw "Build artifact inventory not found: $ArtifactInventoryPath"
}

$packageInventory = Get-Content -LiteralPath $PackageInventoryPath -Raw | ConvertFrom-Json
$artifactInventory = Get-Content -LiteralPath $ArtifactInventoryPath -Raw | ConvertFrom-Json
$componentsByRef = [ordered]@{}

function Add-Component {
    param([pscustomobject]$Component)

    $bomRef = [string]$Component."bom-ref"
    if (-not $componentsByRef.Contains($bomRef)) {
        $componentsByRef[$bomRef] = $Component
    }
}

foreach ($project in @($packageInventory.projects)) {
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

            $id = [string]$package.id
            $version = [string]$package.resolvedVersion
            Add-Component ([pscustomobject]@{
                type = "library"
                "bom-ref" = "pkg:nuget/$id@$version"
                name = $id
                version = $version
                purl = "pkg:nuget/$id@$version"
                scope = "required"
            })
        }
    }
}

foreach ($artifact in @($artifactInventory.artifacts)) {
    foreach ($file in @($artifact.files)) {
        if (-not $file.path -or -not $file.sha256) {
            continue
        }

        $artifactName = [string]$artifact.name
        $filePath = [string]$file.path
        Add-Component ([pscustomobject]@{
            type = "file"
            "bom-ref" = "file:$artifactName/$filePath"
            name = $filePath
            group = $artifactName
            hashes = @(
                [pscustomobject]@{
                    alg = "SHA-256"
                    content = [string]$file.sha256
                }
            )
            properties = @(
                [pscustomobject]@{
                    name = "ai-knowledge-transfer:artifact"
                    value = $artifactName
                },
                [pscustomobject]@{
                    name = "ai-knowledge-transfer:sizeBytes"
                    value = [string]$file.sizeBytes
                }
            )
        })
    }
}

$components = @($componentsByRef.Values)
$packageCount = @($components | Where-Object { $_.type -eq "library" }).Count
$fileCount = @($components | Where-Object { $_.type -eq "file" }).Count

$bom = [pscustomobject]@{
    bomFormat = "CycloneDX"
    specVersion = "1.6"
    serialNumber = "urn:uuid:$([guid]::NewGuid())"
    version = 1
    metadata = [pscustomobject]@{
        timestamp = [DateTimeOffset]::UtcNow.ToString("O")
        tools = [pscustomobject]@{
            components = @(
                [pscustomobject]@{
                    type = "application"
                    name = "scripts/security/generate-cyclonedx-build-sbom.ps1"
                    version = "1.0"
                }
            )
        }
        component = [pscustomobject]@{
            type = "application"
            name = "ai-knowledge-transfer"
            version = if ($env:GITHUB_SHA) { $env:GITHUB_SHA } else { "local" }
        }
        properties = @(
            [pscustomobject]@{
                name = "ai-knowledge-transfer:repository"
                value = if ($env:GITHUB_REPOSITORY) { $env:GITHUB_REPOSITORY } else { "local" }
            },
            [pscustomobject]@{
                name = "ai-knowledge-transfer:sourceWorkflow"
                value = ".github/workflows/ci.yml"
            },
            [pscustomobject]@{
                name = "ai-knowledge-transfer:packageComponentCount"
                value = [string]$packageCount
            },
            [pscustomobject]@{
                name = "ai-knowledge-transfer:fileComponentCount"
                value = [string]$fileCount
            }
        )
    }
    components = @($components)
}

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$bom | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $OutputPath -Encoding UTF8

if ($packageCount -eq 0) {
    throw "CycloneDX SBOM generation produced no package components."
}

if ($fileCount -eq 0) {
    throw "CycloneDX SBOM generation produced no file components."
}

Write-Host "Generated CycloneDX build SBOM with $packageCount package component(s) and $fileCount file component(s): $OutputPath"
