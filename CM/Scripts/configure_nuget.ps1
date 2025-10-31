# Only run the configuration if the required variables are set
if ($GroupRepo -and $GroupRepoUser -and $GroupRepoPW) {
	Write-Host "Configuring PackageStore NuGet source..."
	
	$SourceName = "PackageStore"
	$SourceList = (& $NugetExe sources list -Format Detailed -ForceEnglishOutput | Out-String)
	
	if ($SourceList -like "*$SourceName*") {
		& $NugetExe sources Update -Name $SourceName -Source $GroupRepo -Username $GroupRepoUser -Password $GroupRepoPW
	} else {
		& $NugetExe sources Add -Name $SourceName -Source $GroupRepo -Username $GroupRepoUser -Password $GroupRepoPW
	}
} else {
	Write-Host "Skipping NuGet source configuration: Required variables not set."
}
