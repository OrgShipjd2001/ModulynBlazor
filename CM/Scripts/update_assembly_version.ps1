
Write-Host "----------------------------------------------"
Write-Host "  update_assembly_version.ps1"
Write-Host "----------------------------------------------"

$propsFilePath = "$env:CI_PROJECT_DIR\CM\Version\assemblyversion.props"
$versionFilePath = "version.txt"

if (!(Test-Path $propsFilePath)) {
    Write-Host "Error: MSBuild props file not found at $propsFilePath"
    exit 1
}

if (!(Test-Path $versionFilePath)) {
    Write-Host "Error: Version file not found at $versionFilePath"
    exit 1
}

# Read the generated version
$version = Get-Content $versionFilePath -Raw
$version = $version.Trim()
Write-Host "Updating assemblyversion.props with version: $version"

# Load XML
[xml]$xml = Get-Content $propsFilePath

# Update Version and FileVersion
$xml.Project.PropertyGroup.Version = $version
$xml.Project.PropertyGroup.FileVersion = $version

# Save changes
$xml.Save($propsFilePath)
Write-Host "Updated assemblyversion.props successfully."
