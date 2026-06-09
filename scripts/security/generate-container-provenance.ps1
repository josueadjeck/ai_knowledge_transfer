param(
    [string]$ImageInventoryPath = "artifacts/security/container-images.json",
    [string]$OutputPath = "artifacts/security/container-provenance.json",
    [string[]]$ExpectedTags = @("ai-knowledge-transfer-api:ci", "ai-knowledge-transfer-web:ci")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ImageInventoryPath)) {
    throw "Container image inventory not found: $ImageInventoryPath"
}

$images = @(Get-Content -LiteralPath $ImageInventoryPath -Raw | ConvertFrom-Json)
$findings = @()

if ($images.Count -eq 0) {
    $findings += [pscustomobject]@{
        ruleId = "container-inventory-not-empty"
        severity = "High"
        description = "Container image inventory must contain at least one image."
    }
}

foreach ($tag in $ExpectedTags) {
    $matchingImage = $images | Where-Object { @($_.RepoTags) -contains $tag } | Select-Object -First 1
    if ($null -eq $matchingImage) {
        $findings += [pscustomobject]@{
            ruleId = "expected-image-tag-present"
            severity = "High"
            description = "Expected image tag '$tag' is missing from container inventory."
        }
    }
}

$subjects = @()
foreach ($image in $images) {
    $repoTags = @($image.RepoTags)
    $imageId = [string]$image.Id
    if ($imageId -notmatch "^sha256:[a-fA-F0-9]{64}$") {
        $findings += [pscustomobject]@{
            ruleId = "image-id-sha256"
            severity = "High"
            description = "Image '$($repoTags -join ", ")' does not have a sha256 image id."
        }
    }

    $subjects += [pscustomobject]@{
        name = if ($repoTags.Count -gt 0) { $repoTags[0] } else { $imageId }
        tags = $repoTags
        imageId = $imageId
        digest = @{
            sha256 = $imageId -replace "^sha256:", ""
        }
        created = $image.Created
        dockerVersion = $image.DockerVersion
        architecture = $image.Architecture
        os = $image.Os
        rootFsLayerCount = @($image.RootFS.Layers).Count
        user = $image.Config.User
        labels = $image.Config.Labels
    }
}

$provenance = [pscustomobject]@{
    generatedAt = [DateTimeOffset]::UtcNow.ToString("O")
    statementType = "https://slsa.dev/provenance/v1"
    predicateType = "https://slsa.dev/provenance/v1"
    buildType = "github-actions-container-build"
    repository = if ($env:GITHUB_REPOSITORY) { $env:GITHUB_REPOSITORY } else { "local" }
    gitSha = if ($env:GITHUB_SHA) { $env:GITHUB_SHA } else { "local" }
    gitRef = if ($env:GITHUB_REF) { $env:GITHUB_REF } else { "local" }
    runId = if ($env:GITHUB_RUN_ID) { $env:GITHUB_RUN_ID } else { "local" }
    sourceWorkflow = ".github/workflows/ci.yml"
    expectedTags = @($ExpectedTags)
    subjects = @($subjects)
    findingCount = $findings.Count
    findings = @($findings)
}

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$provenance | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $OutputPath -Encoding UTF8

if ($findings.Count -gt 0) {
    $findings | Format-Table ruleId, severity, description -AutoSize | Out-String | Write-Host
    throw "Container provenance generation found $($findings.Count) finding(s)."
}

Write-Host "Container provenance generated for $($subjects.Count) image(s)."
