param(
    [string[]]$ArtifactPaths = @("artifacts/api", "artifacts/web"),
    [string]$OutputPath = "artifacts/security/build-artifact-inventory.json"
)

$ErrorActionPreference = "Stop"

function Get-RelativePath {
    param(
        [string]$RootPath,
        [string]$FilePath
    )

    $root = [System.IO.Path]::GetFullPath($RootPath)
    $file = [System.IO.Path]::GetFullPath($FilePath)
    $relative = [System.IO.Path]::GetRelativePath($root, $file)
    return $relative -replace "\\", "/"
}

$findings = @()
$artifacts = @()

foreach ($artifactPath in $ArtifactPaths) {
    if (-not (Test-Path -LiteralPath $artifactPath -PathType Container)) {
        $findings += [pscustomobject]@{
            ruleId = "artifact-directory-present"
            severity = "High"
            description = "Build artifact directory is missing: $artifactPath"
        }
        continue
    }

    $files = @(Get-ChildItem -LiteralPath $artifactPath -Recurse -File | Sort-Object FullName)
    if ($files.Count -eq 0) {
        $findings += [pscustomobject]@{
            ruleId = "artifact-directory-not-empty"
            severity = "High"
            description = "Build artifact directory has no files: $artifactPath"
        }
    }

    $fileEntries = @()
    foreach ($file in $files) {
        $hash = Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256
        $fileEntries += [pscustomobject]@{
            path = Get-RelativePath -RootPath $artifactPath -FilePath $file.FullName
            sizeBytes = $file.Length
            sha256 = $hash.Hash.ToLowerInvariant()
        }
    }

    $artifacts += [pscustomobject]@{
        name = Split-Path -Leaf $artifactPath
        path = $artifactPath -replace "\\", "/"
        fileCount = $fileEntries.Count
        totalBytes = ($fileEntries | Measure-Object -Property sizeBytes -Sum).Sum
        files = @($fileEntries)
    }
}

$inventory = [pscustomobject]@{
    generatedAt = [DateTimeOffset]::UtcNow.ToString("O")
    repository = if ($env:GITHUB_REPOSITORY) { $env:GITHUB_REPOSITORY } else { "local" }
    gitSha = if ($env:GITHUB_SHA) { $env:GITHUB_SHA } else { "local" }
    gitRef = if ($env:GITHUB_REF) { $env:GITHUB_REF } else { "local" }
    runId = if ($env:GITHUB_RUN_ID) { $env:GITHUB_RUN_ID } else { "local" }
    sourceWorkflow = ".github/workflows/ci.yml"
    artifactCount = $artifacts.Count
    fileCount = ($artifacts | Measure-Object -Property fileCount -Sum).Sum
    artifacts = @($artifacts)
    findingCount = $findings.Count
    findings = @($findings)
}

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$inventory | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $OutputPath -Encoding UTF8

if ($findings.Count -gt 0) {
    $findings | Format-Table ruleId, severity, description -AutoSize | Out-String | Write-Host
    throw "Build artifact inventory generation found $($findings.Count) finding(s)."
}

Write-Host "Build artifact inventory generated for $($artifacts.Count) artifact(s) with $($inventory.fileCount) file(s)."
