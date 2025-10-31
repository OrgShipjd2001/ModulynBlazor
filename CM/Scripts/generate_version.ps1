Write-Host "----------------------------------------------"
Write-Host "  generate_version.ps1"
Write-Host "----------------------------------------------"

# Define GitLab API variables
$apiUrl = "$env:CI_API_V4_URL/projects/$env:CI_PROJECT_ID/variables"
$headers = @{ "PRIVATE-TOKEN" = "$env:GITLAB_API_TOKEN" }

Write-Host "API Url: $apiUrl"
Write-Host "Static Version: $env:STATIC_VERSION"
Write-Host "Current Branch: $env:CI_COMMIT_REF_NAME"

# Check if this pipeline was triggered by a Git tag
$isTagRelease = $env:CI_COMMIT_TAG -ne $null
if ($isTagRelease) {
    Write-Host "Detected Git tag: $env:CI_COMMIT_TAG. Adjusting build number logic for release."
}

$createLastVersion = $false
$createBuildCount = $false
$buildNumber = 0 # Initialize buildNumber

# -------------------------------------------------------------------
# STEP 1: Determine the next build number / Final Version
# -------------------------------------------------------------------

if ($isTagRelease) {
    # --- NEW LOGIC FOR TAG RELEASE ---
    # The tag itself will be used as the final version.
    # We validate that the tag name has a version format (e.g., v1.2.3 or 1.2.3.4)
    
    $tag = $env:CI_COMMIT_TAG
    Write-Host "Validating Git tag '$tag' as the final version..."
    
    # Simple regex to check for version format (e.g., 1.2.3 or v1.2.3)
    # Allows for 'v' prefix, major.minor.patch, and optional fourth component.
    $versionPattern = '^(v|\d+\.){1}\d+\.\d+(\.\d+)?$' 
    
    if ($tag -match $versionPattern) {
        # Tag is valid, use it directly as the final version
        $env:VERSION = $tag
        Write-Host "Validated tag as version: $env:VERSION"
    } else {
        # Tag is not in a valid version format
        Write-Error "Error: Git tag '$tag' is not in a valid version format (e.g., 1.2.3, v1.2.3, 1.2.3.4). Pipeline must stop."
        # Use 'exit 1' to fail the pipeline
        exit 1
    }

} else {
    # --- LOGIC FOR REGULAR BRANCH BUILD (Original Logic) ---

    # Fetch the last stored static version for the current branch
    Write-Host "Fetching last stored static version for branch variable '$env:LAST_VERSION_VAR'..."
    try {
        $lastVersionResponse = Invoke-RestMethod -Uri "$apiUrl/$env:LAST_VERSION_VAR" -Headers $headers -Method Get
        $lastVersion = $lastVersionResponse.value
    } catch {
        Write-Host "No previous static version found. Setting to empty."
        $lastVersion = ""
        $createLastVersion = $true
    }

    # Fetch and increment the build number for the current branch
    Write-Host "Fetching and incrementing build number for branch variable '$env:BUILD_COUNTER_VAR'..."
    try {
        $buildCounterResponse = Invoke-RestMethod -Uri "$apiUrl/$env:BUILD_COUNTER_VAR" -Headers $headers -Method Get
        # Increment the fetched value
        $buildNumber = [int]$buildCounterResponse.value + 1
    } catch {
        Write-Host "No previous build number found. Starting at 1."
        $buildNumber = 1
        $createBuildCount = $true
    }

    # Check for static version change
    if ($lastVersion -ne $env:STATIC_VERSION) {
        Write-Host "Static Version changed! Resetting build number to 1..."
        $buildNumber = 1
    }
    
    # Generate the new version (for regular branch builds only)
    $env:VERSION = "$env:STATIC_VERSION.$buildNumber"
	$env:BuildNumber = $buildNumber
}

# -------------------------------------------------------------------
# STEP 2: Generate version and set environment variable
# -------------------------------------------------------------------

Write-Host "Generated/Final Version: $env:VERSION"

# Save version to file
$env:VERSION | Out-File -FilePath "version.txt"
$env:BuildNumber | Out-File -FilePath "buildnumber.txt"

$variablesToExport = @(
	"STATIC_VERSION=$env:STATIC_VERSION"
	"CI_BUILD_NUMBER=$env:BuildNumber"
	"CI_BUILD_VERSION=$env:VERSION"
)

$variablesToExport | Set-Content -Path variables.env -Encoding ASCII -Force

# Debugging check
Write-Host "--- Generated variables.env Content ---"
Get-Content -Path variables.env -Raw 
Write-Host "---------------------------------------"

# -------------------------------------------------------------------
# STEP 3: Update CI/CD Variables (Only for Regular Branch Builds)
# -------------------------------------------------------------------

if (-not $isTagRelease) {
    Write-Host "Updating CI/CD variables for regular build (incrementing counter)..."

    # --- Update BUILD_COUNTER_VAR ---
    if ($createBuildCount) {
        Write-Host "Creating variable $env:BUILD_COUNTER_VAR"
        $payloadBuildNum = @{
            key = $env:BUILD_COUNTER_VAR
            value = "$buildNumber"
        } | ConvertTo-Json -Depth 10
        Invoke-RestMethod -Uri "$apiUrl" -Headers $headers -Method Post -Body $payloadBuildNum -ContentType "application/json"
    } else {
        Write-Host "Updating variable $env:BUILD_COUNTER_VAR"
        $payloadBuildNum = @{
            value = "$buildNumber"
        } | ConvertTo-Json -Depth 10
        Invoke-RestMethod -Uri "$apiUrl/$env:BUILD_COUNTER_VAR" -Headers $headers -Method Put -Body $payloadBuildNum -ContentType "application/json"
    }

    # --- Update LAST_VERSION_VAR ---
    if ($createLastVersion) {
        Write-Host "Creating variable $env:LAST_VERSION_VAR"
        $payloadLastVer = @{
            key = $env:LAST_VERSION_VAR
            value = "$env:STATIC_VERSION"
        } | ConvertTo-Json -Depth 10
        Invoke-RestMethod -Uri "$apiUrl" -Headers $headers -Method Post -Body $payloadLastVer -ContentType "application/json"
    } else {
        Write-Host "Updating variable $env:LAST_VERSION_VAR"
        $payloadLastVer = @{
            value = "$env:STATIC_VERSION"
        } | ConvertTo-Json -Depth 10
        Invoke-RestMethod -Uri "$apiUrl/$env:LAST_VERSION_VAR" -Headers $headers -Method Put -Body $payloadLastVer -ContentType "application/json"
    }
} else {
    Write-Host "Skipping CI/CD variable updates because this is a release tag. Version is set directly from tag."
}
