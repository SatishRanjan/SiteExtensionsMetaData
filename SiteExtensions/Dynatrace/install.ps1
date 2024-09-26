# Logging

$logPath = "$env:HOME\SiteExtensions\Dynatrace\install.log"

function Log($level, $message) {
	$line = "{0} {1} {2}" -f (Get-Date), $level, $message

	try {
		Write-Host ("LOG: {0}" -f $line)
	} catch {
	}

	try {
		Write-Output $line | Out-File -Encoding ascii -Append $logPath
	} catch {
	}
}

function LogError($message) {
	if ($null -ne $_.Exception) {
		Log "ERROR" ("Exception.Message = {0}" -f $_.Exception.Message)
	}
	Log "ERROR" $message
}

function LogInfo($message) {
	Log "INFO" $message
}

# Main script

Set-Location "$env:HOME\SiteExtensions\Dynatrace"

# Kudu tries to add/delete/update all of the changed files when upgrading the Site Extension, and *then* runs install.cmd.
# Unfortunately, some DLLs may be in use. Because of this, we copy such DLLs into a different location. Even if they are
# inside the SE directory, Kudu won't touch them.

LogInfo "Shutting down Site Extension dashboard (if running)..."

# So, after Kudu updates the directory, we try to stop the Site Extension if it's running.
Set-Content -Path "app_offline.htm" -Value ""
Start-Sleep -Seconds 15
Remove-Item -Path "app_offline.htm"

# We should be now free to delete the old DLL files.
if (Test-Path -Path "bin") {
	Remove-Item -Path "bin" -Recurse
}

# And then we copy the new ones there.
New-Item -Path "bin" -ItemType Directory
Copy-Item -Path "Dynatrace.AzureSiteExtension*","*.dll" -Destination "bin"

# Old versions for the Site Extension were adding a snippet to the main JavaScript file if found. Since we're now using
# NODE_OPTIONS, rollback changes to remove the snippet, otherwise we'll be injecting the agent twice.
function FindMainNodeFile {
	LogInfo "Looking for main node file..."
	$fallbackResult = "$env:WEBROOT_PATH\server.js"

	try {
		if ($null -ne $env:DT_NODE_MAIN) {
			return $env:DT_NODE_MAIN
		}

		$searchDirectory = ""

		if (Test-Path "$env:WEBROOT_PATH\web.config") {
			LogInfo "web.config found."

			[xml]$webConfigContent = Get-Content "$env:WEBROOT_PATH\web.config"

			$handlerQuery = "//configuration/system.webServer/handlers/add[@name='iisnode']/attribute::path"
			$handler = Select-Xml $handlerQuery $webConfigContent
			$handler = $handler | ForEach-Object { $_.Node.'#text' }

			$handlerPath = "$env:WEBROOT_PATH\$handler"

			if ((Test-Path $handlerPath) -and (Get-Item $handlerPath) -is [System.IO.DirectoryInfo]) {
				$searchDirectory = $handlerPath
			} elseif ((Test-Path $handlerPath) -and (Get-Item $handlerPath) -is [System.IO.FileInfo]) {
				return $handlerPath
			} else {
				throw "$handlerPath is invalid"
			}
		} else {
			LogInfo "No web.config found. Using default directory."
			$searchDirectory = $env:WEBROOT_PATH
		}

		LogInfo "Looking for main node file at: $searchDirectory"

		foreach ($fileName in @("server", "server.js", "app", "app.js", "index", "index.js")) {
			$filePath = "$searchDirectory\$fileName"
			if ((Test-Path $filePath) -and ((Get-Item $filePath) -is [System.IO.FileInfo])) {
				return $filePath
			}
		}

		return $fallbackResult
	} catch {
		LogError "Failed to detect a main node file, using fallback."
		return $fallbackResult
	}
}

$mainNodeFile = FindMainNodeFile
if ($mainNodeFile -and (Test-Path $mainNodeFile)) {
	LogInfo "Main node file found: $mainNodeFile"
	if (Test-Path "$mainNodeFile.orig_dynatrace") {
		Move-Item -Path "$mainNodeFile.orig_dynatrace" -Destination $mainNodeFile -Force
	}
}

Copy-Item -Path "Web.Release.config" -Destination "Web.config" -Force

LogInfo "Installed completed."
