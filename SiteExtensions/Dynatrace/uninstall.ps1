$siteExtensionPath = "$env:HOME\SiteExtensions\Dynatrace"

if (Test-Path env:DT_INSTALL_DIR) {
	$installDir = $env:DT_INSTALL_DIR
} else {
	$installDir = "$env:HOME\SiteExtensions\Dynatrace.Agent"
}

function IsAgentInUse($agentPath) {
	$filesToCheck = @(
		"$agentPath\agent\bin\windows-x86-32\oneagentloader.dll",
		"$agentPath\agent\bin\windows-x86-64\oneagentloader.dll")

	# Do they exist?
	if (!(Test-Path -Path $filesToCheck)) {
		return false
	}

	try {
		foreach ($file in $filesToCheck) {
			# Exception thrown if the file is locked.
			[System.IO.File]::Open($file, "Open", "Write").Dispose()
		}
		return false
	} catch [System.IO.IOException] {
		return true
	}
}

function UninstallAgent {
	# After deleting the ApplicationHost.xdt file, the customer can restart their application
	# (if applicable) and the agent won't be used. 
	Remove-Item "$siteExtensionPath\ApplicationHost.xdt"

	# If the agent is being used, deleting the directory wouldn't delete all since some files
	# are in use, so for safety we skip the step.

	# Check non-version-based agent.
	if (IsAgentInUse($installDir)) {
		return
	}

	# Check version-based agents.
	foreach ($directory in (Get-ChildItem -Path $installDir -Directory)) {
		if (IsAgentInUse($directory.FullName)) {
			return
		}
	}

	# The agent is not in use.
	Remove-Item -Path $installDir -Recurse
}

function StopSiteExtension {
	# The Site Extension may be running, in which case Kudu will fail to delete the directory fully.
	
	# By creating this file, the ASP.Net Core Module will shutdown gracefully the Site Extension website.
	Set-Content -Path "$siteExtensionPath\app_offline.htm" -Value ""

	# By default, the website will be forcefully shut down after 10 seconds once the file is detected, if
	# it doesn't happen earlier. Sleep through that, plus some grace period.
	Start-Sleep -Seconds 20
}

function RemoveSettings {

	$settingsDir = "$env:HOME\Config\Dynatrace.AzureSiteExtension\"
	Remove-Item -Path $settingsDir -Recurse

}
function RemoveStatus {

	$settingsDir = "$env:HOME\site\siteextensions\Dynatrace\"
	Remove-Item -Path $settingsDir -Recurse

}


UninstallAgent
StopSiteExtension
RemoveSettings
RemoveStatus
