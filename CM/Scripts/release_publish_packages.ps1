# Requires: PowerShell (pwsh or powershell.exe)

# --- 1. Variable Assignment and Validation ---

# Get required environment variables
$NugetApiKey = $env:NugetUploadToken
$NugetPackagesDir = $env:pubNugetDir
$NugetExePath = $env:NugetExe
$NugetSource = "https://api.nuget.org/v3/index.json"

Write-Host "--- NuGet Package Publishing Script ---"

# Validate required variables
if ([string]::IsNullOrEmpty($NugetApiKey)) {
    Write-Error "ERROR: Environment variable 'NugetUploadToken' (API Key) is missing."
    exit 1
}
if ([string]::IsNullOrEmpty($NugetPackagesDir)) {
    Write-Error "ERROR: Environment variable 'pubNugetDir' (Packages Directory) is missing."
    exit 1
}
if ([string]::IsNullOrEmpty($NugetExePath)) {
    Write-Error "ERROR: Environment variable 'NugetExe' (NuGet Executable Path) is missing."
    exit 1
}

# Validate NuGet executable existence
if (-not (Test-Path -Path $NugetExePath -PathType Leaf)) {
    Write-Error "ERROR: NuGet executable not found at path: '$NugetExePath'"
    exit 1
}

# --- 2. Find Packages ---

Write-Host "Searching for packages in: '$NugetPackagesDir'..."
$NugetPackages = Get-ChildItem -Path $NugetPackagesDir -Filter "*.nupkg" -Recurse -ErrorAction SilentlyContinue

if ($NugetPackages.Count -eq 0) {
    Write-Host "WARNING: No .nupkg files found in '$NugetPackagesDir'. Exiting gracefully."
    exit 0 # Exit with 0 since no packages were found, which might be acceptable if the release didn't generate any.
}

Write-Host "Found $($NugetPackages.Count) package(s) to publish."

# --- 3. Iterate and Publish ---

$FailureCount = 0
foreach ($Package in $NugetPackages) {
    $PackagePath = $Package.FullName
    $PackageName = $Package.Name

    Write-Host ""
    Write-Host "Attempting to push package: '$PackageName'"

    # Build the nuget push command arguments
    # -Source specifies nuget.org endpoint
    # -ApiKey uses the GitLab CI variable
    # -NonInteractive prevents prompts
    $Arguments = @(
        "push",
        $PackagePath,
        "-Source", $NugetSource,
        "-ApiKey", $NugetApiKey,
        "-NonInteractive"
    )

    # Use a try/catch block to execute the NuGet push command
    try {
        # The '&' operator runs the external executable. 2>&1 redirects both standard output and error to Write-Host.
        & $NugetExePath $Arguments 2>&1 | Write-Host

        # Check the exit code of the last external command (nuget.exe)
        if ($LASTEXITCODE -eq 0) {
            Write-Host "SUCCESS: Package '$PackageName' pushed successfully."
        }
        else {
            Write-Error "FAILURE: NuGet push failed for '$PackageName' with exit code $LASTEXITCODE. Details above."
            $FailureCount++
        }
    }
    catch {
        # Catch unexpected PowerShell-level errors
        Write-Error "CRITICAL ERROR during execution for package '$PackageName':"
        Write-Error $_
        $FailureCount++
    }
}

# --- 4. Final Summary ---

Write-Host ""
Write-Host "--- Publishing Summary ---"
Write-Host "Total packages found: $($NugetPackages.Count)"
Write-Host "Total packages failed: $($FailureCount)"

if ($FailureCount -gt 0) {
    Write-Error "FINAL RESULT: One or more NuGet packages failed to publish. Job failed."
    exit 1
} else {
    Write-Host "FINAL RESULT: All packages published successfully. Job succeeded."
    exit 0
}
