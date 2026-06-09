param(
    [string]$ProvenancePath = "artifacts/security/container-provenance.json",
    [string]$OutputPath = "artifacts/security/container-signing-policy.json",
    [string[]]$ExpectedTags = @("ai-knowledge-transfer-api:ci", "ai-knowledge-transfer-web:ci"),
    [string]$Registry = "ghcr.io/josueadjeck/ai_knowledge_transfer",
    [string]$SignerIdentity = "github-actions-oidc",
    [string]$VerificationPolicy = "registry-signature-required"
)

$ErrorActionPreference = "Stop"

$findings = @()

if (-not (Test-Path -LiteralPath $ProvenancePath)) {
    $findings += [pscustomobject]@{
        ruleId = "container-provenance-present"
        severity = "High"
        description = "Container provenance is required before image signing policy can be evaluated: $ProvenancePath"
    }
    $provenance = $null
}
else {
    $provenance = Get-Content -LiteralPath $ProvenancePath -Raw | ConvertFrom-Json
}

if ([string]::IsNullOrWhiteSpace($Registry) -or $Registry -notmatch "^[a-zA-Z0-9][a-zA-Z0-9\.\-]*(?::[0-9]+)?/.+") {
    $findings += [pscustomobject]@{
        ruleId = "registry-backed-signing-registry-configured"
        severity = "High"
        description = "A concrete target registry path is required for registry-backed image signing."
    }
}

if ([string]::IsNullOrWhiteSpace($SignerIdentity)) {
    $findings += [pscustomobject]@{
        ruleId = "registry-backed-signing-signer-configured"
        severity = "High"
        description = "A signer identity such as GitHub OIDC or a customer-approved signer is required."
    }
}

if ([string]::IsNullOrWhiteSpace($VerificationPolicy)) {
    $findings += [pscustomobject]@{
        ruleId = "registry-backed-signing-verification-policy-configured"
        severity = "High"
        description = "A verification policy is required before signed images can be promoted."
    }
}

$subjects = @()
if ($null -ne $provenance) {
    $subjects = @($provenance.subjects)
    if ($subjects.Count -eq 0) {
        $findings += [pscustomobject]@{
            ruleId = "container-provenance-subjects-present"
            severity = "High"
            description = "Container provenance must contain image subjects."
        }
    }

    foreach ($tag in $ExpectedTags) {
        $matchingSubject = $subjects | Where-Object { @($_.tags) -contains $tag } | Select-Object -First 1
        if ($null -eq $matchingSubject) {
            $findings += [pscustomobject]@{
                ruleId = "signing-policy-expected-tag-present"
                severity = "High"
                description = "Expected image tag '$tag' is missing from provenance subjects."
            }
            continue
        }

        $imageId = [string]$matchingSubject.imageId
        if ($imageId -notmatch "^sha256:[a-fA-F0-9]{64}$") {
            $findings += [pscustomobject]@{
                ruleId = "signing-policy-subject-digest-present"
                severity = "High"
                description = "Expected image tag '$tag' does not have a sha256 image id in provenance."
            }
        }
    }
}

$policy = [pscustomobject]@{
    generatedAt = [DateTimeOffset]::UtcNow.ToString("O")
    policyType = "registry-backed-container-signing"
    status = if ($findings.Count -eq 0) { "Pass" } else { "Fail" }
    registry = $Registry
    signerIdentity = $SignerIdentity
    verificationPolicy = $VerificationPolicy
    repository = if ($env:GITHUB_REPOSITORY) { $env:GITHUB_REPOSITORY } else { "local" }
    gitSha = if ($env:GITHUB_SHA) { $env:GITHUB_SHA } else { "local" }
    gitRef = if ($env:GITHUB_REF) { $env:GITHUB_REF } else { "local" }
    runId = if ($env:GITHUB_RUN_ID) { $env:GITHUB_RUN_ID } else { "local" }
    sourceWorkflow = ".github/workflows/ci.yml"
    expectedTags = @($ExpectedTags)
    subjectCount = $subjects.Count
    findingCount = $findings.Count
    findings = @($findings)
}

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$policy | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $OutputPath -Encoding UTF8

if ($findings.Count -gt 0) {
    $findings | Format-Table ruleId, severity, description -AutoSize | Out-String | Write-Host
    throw "Container signing policy scan found $($findings.Count) finding(s)."
}

Write-Host "Container signing policy passed for $($subjects.Count) provenance subject(s)."
