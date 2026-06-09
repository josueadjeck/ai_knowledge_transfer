param(
    [string[]]$Dockerfiles = @("Dockerfile.api", "Dockerfile.web"),
    [string]$OutputPath = "artifacts/security/container-policy-scan.json"
)

$ErrorActionPreference = "Stop"

$findings = @()
$results = @()

foreach ($dockerfile in $Dockerfiles) {
    if (-not (Test-Path -LiteralPath $dockerfile)) {
        $findings += [pscustomobject]@{
            file = $dockerfile
            ruleId = "dockerfile-exists"
            severity = "High"
            description = "Expected Dockerfile is missing."
        }
        continue
    }

    $content = Get-Content -LiteralPath $dockerfile -Raw
    $lines = Get-Content -LiteralPath $dockerfile
    $fileFindings = @()

    function Add-Finding {
        param(
            [string]$RuleId,
            [string]$Severity,
            [string]$Description
        )

        $fileFindings += [pscustomobject]@{
            file = $dockerfile
            ruleId = $RuleId
            severity = $Severity
            description = $Description
        }
    }

    $fromLines = @($lines | Where-Object { $_ -match "^\s*FROM\s+" })
    if ($fromLines.Count -eq 0) {
        Add-Finding "base-image-defined" "High" "Dockerfile must declare at least one base image."
    }

    foreach ($fromLine in $fromLines) {
        if ($fromLine -match ":latest(\s|$)") {
            Add-Finding "no-latest-base-image" "High" "Base images must not use the latest tag."
        }

        if ($fromLine -notmatch "mcr\.microsoft\.com/dotnet/(sdk|aspnet):10\.0") {
            Add-Finding "approved-dotnet-base-image" "Medium" "Base images should use the approved .NET 10 SDK or ASP.NET runtime image."
        }
    }

    if ($content -notmatch "(?m)^\s*USER\s+\S+") {
        Add-Finding "non-root-user" "High" "Runtime image must declare a non-root USER."
    }

    if ($content -match "(?m)^\s*USER\s+(root|0)\s*$") {
        Add-Finding "no-root-user" "High" "Runtime image must not run as root."
    }

    if ($content -notmatch "(?m)^\s*ENV\s+ASPNETCORE_ENVIRONMENT=Production\s*$") {
        Add-Finding "production-environment" "Medium" "Container must set ASPNETCORE_ENVIRONMENT=Production."
    }

    if ($content -notmatch "(?m)^\s*EXPOSE\s+8080\s*$") {
        Add-Finding "expected-port" "Medium" "Container must expose port 8080."
    }

    if ($content -match "(?m)^\s*ADD\s+") {
        Add-Finding "avoid-add" "Medium" "Use COPY instead of ADD for predictable build context handling."
    }

    if ($content -match "(curl|wget).*\|\s*(sh|bash)") {
        Add-Finding "no-curl-pipe-shell" "High" "Do not pipe downloaded scripts directly into a shell."
    }

    $findings += $fileFindings
    $results += [pscustomobject]@{
        file = $dockerfile
        findingCount = $fileFindings.Count
    }
}

$scan = [pscustomobject]@{
    generatedAt = [DateTimeOffset]::UtcNow.ToString("O")
    scannedFiles = @($Dockerfiles)
    findingCount = $findings.Count
    findings = @($findings)
    results = @($results)
}

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$scan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputPath -Encoding UTF8

if ($findings.Count -gt 0) {
    $findings | Format-Table file, ruleId, severity, description -AutoSize | Out-String | Write-Host
    throw "Container policy scan found $($findings.Count) finding(s)."
}

Write-Host "Container policy scan passed for $($Dockerfiles.Count) Dockerfile(s)."
