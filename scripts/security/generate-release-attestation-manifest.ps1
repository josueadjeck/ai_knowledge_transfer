param(
    [string]$SecurityArtifactPath = "artifacts/security",
    [string]$OutputPath = "artifacts/security/release-attestation-manifest.json",
    [int]$RetentionDays = 90,
    [string[]]$RequiredEvidence = @(
        "build-artifact-inventory.json",
        "dotnet-package-inventory.json",
        "sbom.spdx.json",
        "cyclonedx-build-sbom.json",
        "container-policy-scan.json",
        "container-vulnerability-scan-api.json",
        "container-vulnerability-scan-web.json",
        "container-images.json",
        "container-provenance.json",
        "container-signing-policy.json",
        "vulnerable-packages.json",
        "static-source-scan.json"
    )
)

$ErrorActionPreference = "Stop"

$findings = @()
$evidence = @()

foreach ($fileName in $RequiredEvidence) {
    $path = Join-Path $SecurityArtifactPath $fileName
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $findings += [pscustomobject]@{
            ruleId = "release-evidence-present"
            severity = "High"
            description = "Required release evidence is missing: $fileName"
        }
        continue
    }

    $fileInfo = Get-Item -LiteralPath $path
    $hash = Get-FileHash -LiteralPath $path -Algorithm SHA256
    $evidence += [pscustomobject]@{
        name = $fileName
        path = ($path -replace "\\", "/")
        sizeBytes = $fileInfo.Length
        sha256 = $hash.Hash.ToLowerInvariant()
    }
}

if ($RetentionDays -lt 30) {
    $findings += [pscustomobject]@{
        ruleId = "release-evidence-retention"
        severity = "High"
        description = "Release evidence retention must be at least 30 days."
    }
}

$manifest = [pscustomobject]@{
    generatedAt = [DateTimeOffset]::UtcNow.ToString("O")
    manifestType = "ai-knowledge-transfer-release-attestation"
    status = if ($findings.Count -eq 0) { "Pass" } else { "Fail" }
    repository = if ($env:GITHUB_REPOSITORY) { $env:GITHUB_REPOSITORY } else { "local" }
    gitSha = if ($env:GITHUB_SHA) { $env:GITHUB_SHA } else { "local" }
    gitRef = if ($env:GITHUB_REF) { $env:GITHUB_REF } else { "local" }
    runId = if ($env:GITHUB_RUN_ID) { $env:GITHUB_RUN_ID } else { "local" }
    sourceWorkflow = ".github/workflows/ci.yml"
    retentionDays = $RetentionDays
    evidenceCount = $evidence.Count
    evidence = @($evidence)
    findingCount = $findings.Count
    findings = @($findings)
}

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$manifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $OutputPath -Encoding UTF8

if ($findings.Count -gt 0) {
    $findings | Format-Table ruleId, severity, description -AutoSize | Out-String | Write-Host
    throw "Release attestation manifest found $($findings.Count) finding(s)."
}

Write-Host "Release attestation manifest generated with $($evidence.Count) evidence file(s) and $RetentionDays day retention."
