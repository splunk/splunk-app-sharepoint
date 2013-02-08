Add-PsSnapIn Microsoft.SharePoint.PowerShell

function New-SplunkArray {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory=$true)]
		[string[]]$Fields,
		
		[Parameter(Mandatory=$true)]
		[string]$Type,
		
		[Parameter(Mandatory=$true)]
		$Object
	)
	
	process {
		$array = New-Object System.Collections.ArrayList
		$date = Get-Date -format 'yyyy-MM-ddTHH:mm:sszzz'
		[void]$array.Add($date)
		[void]$array.Add("Type=`"$Type`"")
		foreach ($prop in $Fields) {
			if (($Object.PSObject.Properties[$prop] -ne $null) -and ($Object.PSObject.Properties[$prop].Value -ne $null)) {
				[void]$array.Add("$prop=`"$($f.PSObject.Properties[$prop].Value)`"")
			}
		}
		Return ,$array		
	}
}

function Output-SPFarm {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory=$True, ValueFromPipeline=$True, ValueFromPipelineByPropertyName=$True)]
		$farmlist
	)
	
	process {
		$Fields = @( "Name", "DisplayName", "Id", "Status", "Version", "BuildVersion", "DefaultServiceAccount" )
		foreach ($f in $farmlist) {
			$array = New-SplunkArray -Type "SPFarm" -Fields $Fields -Object $f 
			Write-Host ($array -join " ")
		}
	}
}

function Output-SPServer {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory=$True, ValueFromPipeline=$True, ValueFromPipelineByPropertyName=$True)]
		$serverlist
	)
	
	process {
		$Fields = @( "Name", "DisplayName", "Id", "Status", "Version", "Address", "Role" )
		foreach ($f in $serverlist) {
			$array = New-SplunkArray -Type "SPServer" -Fields $Fields -Object $f 
			[void]$array.Add("Farm=`"$($f.Farm.Id)`"")
			Write-Host ($array -join " ")
		}
	}
}

function Output-SPSite {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory=$True, ValueFromPipeline=$True, ValueFromPipelineByPropertyName=$True)]
		$sitelist
	)
	
	process {
		$Fields = @( "Id", "ServerRelativeUrl", "Zone", "Url", 
					 "LastContentModifiedDate", "LastSecurityModifiedDate", "CertificationDate",
					 "IISAllowsAnonymous", "ReadLocked", "WriteLocked", "ReadOnly" )
		foreach ($f in $sitelist) {
			$array = New-SplunkArray -Type "SPSite" -Fields $Fields -Object $f 			
			[void]$array.Add("Usage-Storage=`"$($f.Usage.Storage)`"")
			[void]$array.Add("Usage-Bandwidth=`"$($f.Usage.Bandwidth)`"")
			[void]$array.Add("Usage-Visits=`"$($f.Usage.Visits)`"")
			[void]$array.Add("Usage-Hits=`"$($f.Usage.Hits)`"")
			[void]$array.Add("Usage-DiscussionStorage=`"$($f.Usage.DiscussionStorage)`"")
			[void]$array.Add("WebApplication=`"$($f.WebApplication.Id)`"")
			[void]$array.Add("Farm=`"$($f.WebApplication.Farm.Id)`"")
			[void]$array.Add("ContentDatabase=`"$($f.ContentDatabase.Id)`"")
			[void]$array.Add("RootWeb=`"$($f.RootWeb.Id)`"")
			
			Write-Host ($array -join " ")
		}
	}
}


function Output-SPWebApplication {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory=$True, ValueFromPipeline=$True, ValueFromPipelineByPropertyName=$True)]
		$webapplist
	)
	
	process {
		$Fields = @( "Name", "DisplayName", "Id", "Status", "Version", "Url" )
		foreach ($f in $webapplist) {
			$array = New-SplunkArray -Type "SPWebApplication" -Fields $Fields -Object $f 			
			foreach ($url in $f.AlternateUrls) {
				[void]$array.Add("AltUrl=`"$($url.Zone);$($url.PublicUrl)`"")
			}
			Write-Host ($array -join " ")
		}
	}
}

function Output-SPContentDatabase {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory=$True, ValueFromPipeline=$True, ValueFromPipelineByPropertyName=$True)]
		$dblist
	)
	
	process {
		$Fields = @( "Name", "Id", "Server", "CurrentSiteCount" )
		foreach ($f in $dblist) {
			$array = New-SplunkArray -Type "SPContentDatabase" -Fields $Fields -Object $f 			
			[void]$array.Add("WebApplication=`"$($f.WebApplication.Id)`"")
			Write-Host ($array -join " ")
		}
	}
}

function Output-SPWeb {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory=$True, ValueFromPipeline=$True, ValueFromPipelineByPropertyName=$True)]
		$weblist
	)
	
	process {
		$Fields = @( "Name", "Title", "Description", "Author", "Id", "WebTemplateId", "Language",
					 "Created", "LastItemModifiedDate", "Url", "ServerRelativeUrl", "IsRootWeb",
					 "Authenticationmode" )
		foreach ($f in $weblist) {
			$array = New-SplunkArray -Type "SPWeb" -Fields $Fields -Object $f 			
			[void]$array.Add("Site=`"$($f.Site.Id)`"")
			[void]$array.Add("WebTemplate=`"$($f.WebTemplate)#$($f.Configuration)`"")
			Write-Host ($array -join " ")
		}
	}
}

function Output-SPWebTemplate {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory=$True, ValueFromPipeline=$True, ValueFromPipelineByPropertyName=$True)]
		$weblist
	)
	
	process {
		$Fields = @( "Name", "Id", "Title", "DisplayCategory", "LocaleId",
		             "IsUnique", "IsHidden", "IsCustomTemplate", "IsRootWebOnly", "IsSubWebOnly" )
		foreach ($f in $weblist) {
			$array = New-SplunkArray -Type "SPWebTemplate" -Fields $Fields -Object $f 			
			Write-Host ($array -join " ")
		}
	}
}

####
#### Actual Output Routines
####
Get-SPFarm                     | Output-SPFarm
Get-SPServer $env:ComputerName | Output-SPServer
Get-SPSite                     | Output-SPSite
Get-SPWebApplication           | Output-SPWebApplication
Get-SPContentDatabase          | Output-SPContentDatabase
Get-SPSite | Get-SPWeb         | Output-SPWeb
Get-SPWebTemplate              | Output-SPWebTemplate