param(
    [string]$OutputPath = "artifacts/security/static-source-scan.json"
)

$ErrorActionPreference = "Stop"

$rules = @(
    @{
        Id = "cors-allow-any-origin"
        Pattern = "AllowAnyOrigin\s*\("
        Severity = "High"
        Description = "CORS must not allow every origin in release code."
    },
    @{
        Id = "jwt-validate-issuer-disabled"
        Pattern = "ValidateIssuer\s*=\s*false"
        Severity = "High"
        Description = "JWT issuer validation must not be disabled."
    },
    @{
        Id = "jwt-validate-audience-disabled"
        Pattern = "ValidateAudience\s*=\s*false"
        Severity = "High"
        Description = "JWT audience validation must not be disabled."
    },
    @{
        Id = "oidc-https-metadata-disabled"
        Pattern = "RequireHttpsMetadata\s*=\s*false"
        Severity = "High"
        Description = "OIDC metadata must require HTTPS."
    },
    @{
        Id = "developer-exception-page"
        Pattern = "UseDeveloperExceptionPage\s*\("
        Severity = "Medium"
        Description = "Developer exception pages must not be enabled in release code."
    },
    @{
        Id = "trusted-server-certificate"
        Pattern = "TrustServerCertificate\s*=\s*True"
        Severity = "High"
        Description = "Database connections must not trust arbitrary server certificates."
    }
)

$roots = @("src", ".github")
$include = @("*.cs", "*.csproj", "*.json", "*.yml", "*.yaml", "*.props", "*.targets")
$excludeDirectories = @("bin", "obj")
$findings = @()

foreach ($root in $roots) {
    if (-not (Test-Path $root)) {
        continue
    }

    $files = Get-ChildItem -Path $root -Recurse -File -Include $include |
        Where-Object {
            $pathParts = $_.FullName -split "[\\/]"
            -not ($pathParts | Where-Object { $excludeDirectories -contains $_ })
        }

    foreach ($file in $files) {
        $lines = Get-Content -LiteralPath $file.FullName
        for ($index = 0; $index -lt $lines.Count; $index++) {
            foreach ($rule in $rules) {
                if ($lines[$index] -match $rule.Pattern) {
                    $findings += [pscustomobject]@{
                        ruleId = $rule.Id
                        severity = $rule.Severity
                        file = (Resolve-Path -LiteralPath $file.FullName -Relative)
                        line = $index + 1
                        description = $rule.Description
                        snippet = $lines[$index].Trim()
                    }
                }
            }
        }
    }
}

$result = [pscustomobject]@{
    generatedAt = [DateTimeOffset]::UtcNow.ToString("O")
    scannedRoots = @($roots)
    ruleCount = $rules.Count
    findingCount = $findings.Count
    findings = @($findings)
}

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputPath -Encoding UTF8

if ($findings.Count -gt 0) {
    $findings | Format-Table ruleId, severity, file, line, snippet -AutoSize | Out-String | Write-Host
    throw "Static source security scan found $($findings.Count) finding(s)."
}

Write-Host "Static source security scan passed with $($rules.Count) rules."
