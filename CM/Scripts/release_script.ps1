Write-Host "Starting release creation via GitLab CLI (glab)..."
$glabExe = "$env:CI_PROJECT_DIR\buildtools\glab\glab.exe"
$zipTool = "$env:CI_PROJECT_DIR\buildtools\7-zip\7z.exe"
$version = "$env:CI_BUILD_VERSION"
$zipOutDir = "$env:zipDir"
$projectName = "$env:CI_PROJECT_NAME"

& $glabExe config set host $env:CI_SERVER_URL --global

Write-Host "Logging in..."
& $glabExe auth login --hostname $env:CI_SERVER_HOST --token $env:GITLAB_API_TOKEN

Write-Host "Creating GitLab Release..."

# Example: Define release description content (use markdown)
$releaseDescription = "## Release $env:CI_COMMIT_TAG`n`nAutomated release build."

Write-Host "Release Command: "
Write-Host "$glabExe release create $env:CI_COMMIT_TAG"

& $glabExe release create $env:CI_COMMIT_TAG --ref $env:CI_COMMIT_SHA --name "$projectName V$version" --notes "$releaseDescription"

Write-Host "Release created for tag $env:CI_COMMIT_TAG."

Write-Host "Creating Archives"
& $zipTool a $zipOutDir\ModulynInterface_$version.zip $env:pubRelDir\ModulynInterface\**
& $zipTool a $zipOutDir\ModulynServer_$version.zip $env:pubRelDir\ModulynServer\**

Write-Host "Upload zip files"
& $glabExe release upload $env:CI_COMMIT_TAG "$zipOutDir\ModulynInterface_$version.zip#Modulyn Interface#package"
& $glabExe release upload $env:CI_COMMIT_TAG "$zipOutDir\ModulynServer_$version.zip#Modulyn Server#package"


