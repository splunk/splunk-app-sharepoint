Import-Module "$SplunkHome\etc\apps\TA-Sharepoint2013-Audit\bin\SPCommon.psm1"
Load-PsSnapIn Microsoft.SharePoint.PowerShell

$State = Import-LocalStorage "SPFarm.xml" -DefaultValue (New-Object PSObject -Property @{ Farms = @{} })

$FarmList = Get-SPFarm | Select-Object `
	@{n="Type";e={$_.TypeName}}, `
	Id, `
	Name, `
	DisplayName, ` 
	Status, `
	Version, `
	BuildVersion, `
	DiskSizeRequired, `
	PersistedFileChunkSize, `
	CEIPEnabled, `
	@{n="DefaultServiceAccount";e={$_.DefaultServiceAccount.Name}}, `
	DownloadErrorReportingUpdates, `
	ErrorReportingAutomaticUpload, `
	ErrorReportingEnabled, `
	IsBackwardsCompatible, `
	PasswordChangeEmailAddress, `
	PassswordChangeGuardTime, `
	PasswordChangeMaximumTries, `
	DaysBeforePasswordExpirationToSendEmail

foreach ($Farm in $FarmList) {
	$DoEmit = $false
	
	if (-not $State.Farms.ContainsKey($Farm.Id)) {
		$DoEmit = $true
	} else {
		$FarmState = $State.Farms.Get_Item($Farm.Id)
		if ($FarmState.EmitTime.AddHours(24) -le [DateTime]::Now) {
			$DoEmit = $true
		}
		if ($FarmState.Checksum -ne Get-Checksum($Farm)) {
			$DoEmit = $true
		}
	}
	
	if ($DoEmit -eq $true) {
		$Farm | Write-Output
		
		$FarmState = New-Object PSObject
		$FarmState | Add-Member -MemberType NoteProperty -Name Checksum -Value (Get-Checksum($Farm))
		$FarmState | Add-Member -MemberType NoteProperty -Name EmitTime -Value ([DateTime]::Now)
		$State.Farms.Set_Item($Farm.Id, $FarmState)
	}
}

$State | Export-LocalStorage "SPFarm.xml"
