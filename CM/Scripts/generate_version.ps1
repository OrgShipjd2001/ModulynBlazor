
Write-Host "----------------------------------------------"
Write-Host "  generate_version.ps1"
Write-Host "----------------------------------------------"

# Define GitLab API variables
$apiUrl = "$env:CI_API_V4_URL/projects/$env:CI_PROJECT_ID/variables"
$headers = @{ "PRIVATE-TOKEN" = "$env:GITLAB_API_TOKEN" }

Write-Host "API Url: $apiUrl"
Write-Host "Static Version: $env:STATIC_VERSION"

$createLastVersion = $false

# Fetch the last stored version for the branch
Write-Host "Fetching last stored version for branch $env:BRANCH_NAME..."
try {
    $lastVersionResponse = Invoke-RestMethod -Uri "$apiUrl/$env:LAST_VERSION_VAR" -Headers $headers -Method Get
    $lastVersion = $lastVersionResponse.value
} catch {
    Write-Host "No previous version found. Setting to empty."
    $lastVersion = ""
	$createLastVersion = $true
}

$createBuildCount = $false
# Determine the build number
   try {
        $buildCounterResponse = Invoke-RestMethod -Uri "$apiUrl/$env:BUILD_COUNTER_VAR" -Headers $headers -Method Get
        $buildNumber = [int]$buildCounterResponse.value + 1
    } catch {
        Write-Host "No previous build number found. Starting at 1."
        $buildNumber = 1
		$createBuildCount = $true
    }


if ($lastVersion -ne $env:STATIC_VERSION) {
    Write-Host "Version changed! Resetting build number to 1..."
    $buildNumber = 1
}

# Generate the new version
$env:VERSION = "$env:STATIC_VERSION.$buildNumber"
Write-Host "Generated Version: $env:VERSION"

# Update CI/CD Variables
Write-Host "Updating CI/CD variables..."

# Define the payload as a hashtable and convert it to JSON
if ($createBuildCount)
{
	Write-Host "Create variable $env:BUILD_COUNTER_VAR"
	$payloadBuildNum = @{
		key = $env:BUILD_COUNTER_VAR
		value = "$buildNumber"
	} | ConvertTo-Json -Depth 10
	Invoke-RestMethod -Uri "$apiUrl" -Headers $headers -Method Post -Body $payloadBuildNum -ContentType "application/json"
}
else
{
	Write-Host "Update variable $env:BUILD_COUNTER_VAR"
	$payloadBuildNum = @{
		value = "$buildNumber"
	} | ConvertTo-Json -Depth 10
	Invoke-RestMethod -Uri "$apiUrl/$env:BUILD_COUNTER_VAR" -Headers $headers -Method Put -Body $payloadBuildNum -ContentType "application/json"
}

if ($createLastVersion)
{
	Write-Host "Create variable $env:LAST_VERSION_VAR"
	# Define the payload as a hashtable and convert it to JSON
	$payloadLastVer = @{
		key = $env:LAST_VERSION_VAR
		value = "$env:STATIC_VERSION"
	} | ConvertTo-Json -Depth 10
	Invoke-RestMethod -Uri "$apiUrl" -Headers $headers -Method Post -Body $payloadLastVer -ContentType "application/json"
}
else
{
	Write-Host "Update variable $env:LAST_VERSION_VAR"
	# Define the payload as a hashtable and convert it to JSON
	$payloadLastVer = @{
		value = "$env:STATIC_VERSION"
	} | ConvertTo-Json -Depth 10
	Write-Host "Invoke-RestMethod -Uri $apiUrl/$env:LAST_VERSION_VAR -Headers [redacted] -Method $createLastVersion -Body $payloadLastVer -ContentType application/json"
	Invoke-RestMethod -Uri "$apiUrl/$env:LAST_VERSION_VAR" -Headers $headers -Method Put -Body $payloadLastVer -ContentType "application/json"
}

# Save version to file
$env:VERSION | Out-File -FilePath "version.txt"
