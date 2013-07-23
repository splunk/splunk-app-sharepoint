function Out-Splunk
{
	[CmdletBinding()]
	param(
		[Parameter(Mandatory=$True,ValueFromPipeline=$True,ValueFromPipelineByPropertyName=$True)]
		[PSObject[]] $Objects
	)

	PROCESS {
		foreach ($object in $Objects)
		{
			$arr = New-Object System.Collections.ArrayList
			[void]$arr.Add("$(Get-Date -format 'yyyy-MM-ddTHH:mm:sszzz')")
			foreach ($p in $object.PSObject.Properties)
			{
				[void]$arr.Add("$($p.Name)=`"$($p.Value)`"")
			}
			Write-Host ($arr -join " ")
		}
	}
}

Get-Service | Out-Splunk