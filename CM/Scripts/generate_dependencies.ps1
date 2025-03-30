param (
    [string[]]$csprojFiles,
    [string]$outputDir = "."
)

if ($csprojFiles.Count -eq 0) {
    Write-Host "Usage: .\Generate-Dependencies.ps1 -csprojFiles <csproj1> <csproj2> ... -outputDir <output-path>"
    exit 1
}

# Ensure the output directory exists
if (-Not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

$dependenciesByFramework = @{}

foreach ($csproj in $csprojFiles) {
    if (-Not (Test-Path $csproj)) {
        Write-Host "Skipping missing file: $csproj"
        continue
    }

    Write-Host "Processing $csproj"

    $json = dotnet list $csproj package --format json | ConvertFrom-Json
    
    foreach ($frameworkData in $json.projects[0].frameworks) {
        $framework = $frameworkData.framework
        if (-not $dependenciesByFramework.ContainsKey($framework)) {
            $dependenciesByFramework[$framework] = @{}
        }

        foreach ($package in $frameworkData.topLevelPackages) {
            $id = $package.id
            $version = $package.resolvedVersion

            # Store the highest version per framework
            if (-not $dependenciesByFramework[$framework].ContainsKey($id) -or [System.Version]$version -gt [System.Version]$dependenciesByFramework[$framework][$id]) {
                $dependenciesByFramework[$framework][$id] = $version
            }
        }
    }
}

# Generate separate XML files for each framework
foreach ($framework in $dependenciesByFramework.Keys) {
    $safeFramework = $framework -replace "[^a-zA-Z0-9\.-]", "_"  # Ensure valid filename
    $fileName = "$outputDir\dependencies-$safeFramework.xml"
    Write-Host "Generating $fileName"

    $dependencyList = "<group targetFramework=""$framework"">`n"
    foreach ($key in $dependenciesByFramework[$framework].Keys) {
        $dependencyList += "  <dependency id='$key' version='$($dependenciesByFramework[$framework][$key])' />`n"
    }
    $dependencyList += "</group>"

    # Write without BOM
    [System.IO.File]::WriteAllText($fileName, $dependencyList, (New-Object System.Text.UTF8Encoding($false)))
}

Write-Host "Dependency lists saved to $outputDir"
