Import-Module "$SplunkHome\etc\apps\TA-Sharepoint2013-Audit\bin\SPCommon.psm1"
Load-PsSnapIn Microsoft.SharePoint.PowerShell

$FarmId = (Get-SPFarm).Id
$SaveFile = "SPAlternateUrl.xml"

$State = Import-LocalStorage $SaveFile -DefaultValue (New-Object PSObject -Property @{ Objects = @{} })

$List = Get-SPAlternateURL | `
| Select-Object @{n="Type";e={"AlternateUrl"}}, `
			    @{n="FarmId";e={$FarmId}}, `
				Uri, Zone, IncomingUrl, PublicUrl 

foreach ($O in $List) {
	$DoEmit = $false
	$Key = $O.Uri
	
	if (-not $State.Farms.ContainsKey($Key)) {
		$DoEmit = $true
	} else {
		$S = $State.Farms.Get_Item($Key)
		if ($S.EmitTime.AddHours(24) -le [DateTime]::Now) {
			$DoEmit = $true
		}
		if ($S.Checksum -ne Get-Checksum($O)) {
			$DoEmit = $true
		}
	}
	
	if ($DoEmit -eq $true) {
		$O | Write-Output
		
		$S = New-Object PSObject
		$S | Add-Member -MemberType NoteProperty -Name Checksum -Value (Get-Checksum($O))
		$S | Add-Member -MemberType NoteProperty -Name EmitTime -Value ([DateTime]::Now)
		$State.Farms.Set_Item($Key, $S)
	}
}

$State | Export-LocalStorage $SaveFile
