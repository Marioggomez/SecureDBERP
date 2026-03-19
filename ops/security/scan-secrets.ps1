[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-PlaceholderValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $normalized = $Value.Trim()
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return $true
    }

    $placeholderMarkers = @(
        "__",
        "<",
        ">",
        "CHANGE_ME",
        "YOUR_",
        "EXAMPLE",
        "PLACEHOLDER",
        "LOCALHOST",
        "INTEGRATED SECURITY=TRUE",
        "TRUSTED_CONNECTION=TRUE"
    )

    foreach ($marker in $placeholderMarkers) {
        if ($normalized.ToUpperInvariant().Contains($marker)) {
            return $true
        }
    }

    return $false
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
Set-Location $repoRoot

$excludedExtensions = @(
    ".dll", ".exe", ".pdb", ".png", ".jpg", ".jpeg", ".gif", ".pdf", ".zip",
    ".woff", ".woff2", ".ttf", ".ico", ".suo", ".user", ".nupkg", ".snupkg"
)

$violations = New-Object System.Collections.Generic.List[object]

$trackedFiles = git -C $repoRoot ls-files
foreach ($relativePath in $trackedFiles) {
    if ([string]::IsNullOrWhiteSpace($relativePath)) {
        continue
    }

    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        continue
    }

    if ($fullPath -match "\\.git\\|\\bin\\|\\obj\\") {
        continue
    }

    $extension = [System.IO.Path]::GetExtension($fullPath)
    if ($excludedExtensions -contains $extension.ToLowerInvariant()) {
        continue
    }

    $lines = @(Get-Content -LiteralPath $fullPath -ErrorAction SilentlyContinue)
    if ($null -eq $lines) {
        continue
    }

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = [string]$lines[$i]
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        $connectionMatch = [regex]::Match(
            $line,
            '(?i)\b(server|data source)\s*=\s*[^;]+;[^\\r\\n]*?\b(user id|uid)\s*=\s*[^;]+;[^\\r\\n]*?\b(password|pwd)\s*=\s*(?<pwd>[^;"\r\n]+)')

        if ($connectionMatch.Success) {
            $passwordValue = $connectionMatch.Groups["pwd"].Value
            if (-not (Test-PlaceholderValue -Value $passwordValue)) {
                $violations.Add([pscustomobject]@{
                    Path   = $relativePath
                    Line   = $i + 1
                    Rule   = "ConnectionStringWithPassword"
                    Detail = "Connection string with real password detected."
                })
            }
        }

        if ([regex]::IsMatch($line, '\bgh[pousr]_[A-Za-z0-9]{30,}\b')) {
            $violations.Add([pscustomobject]@{
                Path   = $relativePath
                Line   = $i + 1
                Rule   = "GitHubTokenPattern"
                Detail = "Possible GitHub token detected."
            })
        }

        if ([regex]::IsMatch($line, '\bAKIA[0-9A-Z]{16}\b')) {
            $violations.Add([pscustomobject]@{
                Path   = $relativePath
                Line   = $i + 1
                Rule   = "AwsAccessKeyPattern"
                Detail = "Possible AWS access key detected."
            })
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "Secret scan failed. Potential exposed secrets:"
    foreach ($v in $violations) {
        Write-Host "$($v.Path):$($v.Line) [$($v.Rule)] $($v.Detail)"
    }
    exit 1
}

Write-Host "Secret scan passed. No exposed credentials detected in tracked files."
exit 0
