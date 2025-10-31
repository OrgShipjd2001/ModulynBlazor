Write-Host "----------------------------------------------"
Write-Host "  setup.ps1"
Write-Host "----------------------------------------------"

# Read static version from MasterVersion.txt
$versionFilePath = "$env:CI_PROJECT_DIR\CM\Version\MasterVersion.txt"

if (Test-Path $versionFilePath) {
    $env:STATIC_VERSION = Get-Content $versionFilePath
} else {
    Write-Host "Error: MasterVersion.txt not found at $versionFilePath"
    exit 1
}

if (Test-Path "version.txt")
{
	Write-Host "Loading version from version.txt"
	$env:CI_BUILD_VERSION = Get-Content version.txt
}

if (Test-Path "buildnumber.txt")
{
	Write-Host "Loading buildnumber from buildnumber.txt"
	$env:CI_BUILD_NUMBER = Get-Content buildnumber.txt
}

# Convert current branch name to a safe format
$env:BRANCH_NAME = $env:CI_COMMIT_REF_NAME -replace '[^a-zA-Z0-9]', '-'
$env:BUILD_COUNTER_VAR = "BUILD_COUNTER_$($env:BRANCH_NAME)"
$env:LAST_VERSION_VAR = "LAST_VERSION_$($env:BRANCH_NAME)"

# --- NEW LOGIC FOR RELEASE BUILD COUNTER ---
# We calculate the variable name for the default branch (usually 'main' or 'master').
# This is needed by generate_version.ps1 when a tag is detected.
$mainBranchName = $env:CI_DEFAULT_BRANCH -replace '[^a-zA-Z0-9]', '-'
$env:MAIN_BUILD_COUNTER_VAR = "BUILD_COUNTER_$($mainBranchName)"
# -------------------------------------------

Write-Host "Static Version: $env:STATIC_VERSION"
Write-Host "Branch Name: $env:BRANCH_NAME"
Write-Host "Using CI/CD Variables: $env:BUILD_COUNTER_VAR, $env:LAST_VERSION_VAR"
Write-Host "Main Build Counter Variable: $env:MAIN_BUILD_COUNTER_VAR"
Write-Host "CI_BUILD_VERSION: $env:CI_BUILD_VERSION"
Write-Host "CI_BUILD_NUMBER: $env:CI_BUILD_NUMBER"
