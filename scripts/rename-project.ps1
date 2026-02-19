[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$NewName,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$OldName = 'Base',

    [Parameter()]
    [switch]$CodeOnly,

    [Parameter()]
    [switch]$SkipValidation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($NewName -eq $OldName) {
    throw "NewName and OldName cannot be equal."
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$escapedOld = [Regex]::Escape($OldName)
$selfScriptPath = $PSCommandPath

# Match project/folder/file names like Base.API, Base_Boilerplate, Base-Api, or just Base.
$itemNamePattern = "^$escapedOld([._-]|$)"

# Replace only whole-word occurrences to avoid changing identifiers like BaseRepository.
$contentPattern = "\b$escapedOld\b"

$skipDirs = @('.git', '.codex', '.config', '.vs', 'bin', 'obj')
$textExtensions = @(
    '.cs', '.csproj', '.sln', '.props', '.targets', '.json', '.yml', '.yaml', '.md',
    '.sh', '.ps1', '.config', '.xml', '.http', '.txt', '.env', '.example',
    '.dockerignore', '.gitignore', '.editorconfig'
)
$codeOnlyTextExtensions = @(
    '.cs', '.csproj', '.sln', '.props', '.targets', '.json', '.yml', '.yaml',
    '.config', '.xml', '.sh', '.ps1', '.http'
)
$textFileNames = @('Dockerfile')

function IsSkippedPath([string]$fullPath) {
    foreach ($name in $skipDirs) {
        if ($fullPath -match "[\\/]$([Regex]::Escape($name))([\\/]|$)") {
            return $true
        }
    }

    return $false
}

function RenamePathWithRetry {
    param(
        [Parameter(Mandatory = $true)]
        [string]$LiteralPath,
        [Parameter(Mandatory = $true)]
        [string]$NewName,
        [Parameter(Mandatory = $true)]
        [string]$Kind
    )

    $maxAttempts = 5
    $delayMs = 500
    $lastError = $null

    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        try {
            Rename-Item -LiteralPath $LiteralPath -NewName $NewName
            return
        }
        catch {
            $lastError = $_
            if ($attempt -lt $maxAttempts) {
                Start-Sleep -Milliseconds $delayMs
            }
        }
    }

    throw "Failed to rename $Kind '$LiteralPath' to '$NewName'. Close Visual Studio/VS Code, stop dotnet processes, and close terminals opened inside project folders. Last error: $($lastError.Exception.Message)"
}

function RunDotnetValidation([string]$rootPath) {
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        Write-Warning "dotnet not found in PATH. Skipping validation."
        return
    }

    $solution = Get-ChildItem -Path $rootPath -Filter *.sln -File | Select-Object -First 1
    if (-not $solution) {
        Write-Warning "No .sln found. Skipping validation."
        return
    }

    Write-Host "Running validation: dotnet build $($solution.Name)"
    & dotnet build $solution.FullName -v minimal
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed."
    }

    Write-Host "Running validation: dotnet test $($solution.Name)"
    & dotnet test $solution.FullName -v minimal
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet test failed."
    }
}

Push-Location $repoRoot
try {
    $renamedDirs = 0
    $renamedFiles = 0
    $updatedFiles = 0

    $dirsToRename = Get-ChildItem -Path . -Directory -Recurse -Force |
        Where-Object {
            -not (IsSkippedPath $_.FullName) -and $_.Name -match $itemNamePattern
        } |
        Sort-Object { $_.FullName.Length } -Descending

    foreach ($dir in $dirsToRename) {
        $newDirName = $dir.Name -replace $contentPattern, $NewName
        if ($newDirName -ne $dir.Name) {
            if ($PSCmdlet.ShouldProcess($dir.FullName, "Rename directory to $newDirName")) {
                RenamePathWithRetry -LiteralPath $dir.FullName -NewName $newDirName -Kind 'directory'
            }
            $renamedDirs++
        }
    }

    $filesToRename = Get-ChildItem -Path . -File -Recurse -Force |
        Where-Object {
            -not (IsSkippedPath $_.FullName) -and $_.Name -match $itemNamePattern
        } |
        Sort-Object { $_.FullName.Length } -Descending

    foreach ($file in $filesToRename) {
        $newFileName = $file.Name -replace $contentPattern, $NewName
        if ($newFileName -ne $file.Name) {
            if ($PSCmdlet.ShouldProcess($file.FullName, "Rename file to $newFileName")) {
                RenamePathWithRetry -LiteralPath $file.FullName -NewName $newFileName -Kind 'file'
            }
            $renamedFiles++
        }
    }

    $textFiles = Get-ChildItem -Path . -File -Recurse -Force |
        Where-Object {
            if (IsSkippedPath $_.FullName) {
                return $false
            }

            if ((Resolve-Path $_.FullName).Path -eq $selfScriptPath) {
                return $false
            }

            $nameMatch = $textFileNames -contains $_.Name
            $effectiveTextExtensions = if ($CodeOnly) { $codeOnlyTextExtensions } else { $textExtensions }
            $extMatch = $effectiveTextExtensions -contains $_.Extension
            return $nameMatch -or $extMatch
        }

    foreach ($file in $textFiles) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        $updated = [Regex]::Replace($content, $contentPattern, $NewName)

        if ($updated -ne $content) {
            if ($PSCmdlet.ShouldProcess($file.FullName, "Replace '$OldName' with '$NewName' in content")) {
                Set-Content -LiteralPath $file.FullName -Value $updated -Encoding UTF8
            }
            $updatedFiles++
        }
    }

    Write-Host "Rename complete."
    Write-Host "Directories renamed: $renamedDirs"
    Write-Host "Files renamed: $renamedFiles"
    Write-Host "Files updated: $updatedFiles"

    if (-not $WhatIfPreference -and -not $SkipValidation) {
        RunDotnetValidation -rootPath $repoRoot
    }
}
finally {
    Pop-Location
}
