Import-Module "$SplunkHome\etc\apps\TA-Sharepoint2013-Audit\bin\SPCommon.psm1"
Load-PsSnapIn Microsoft.SharePoint.PowerShell

$FarmId = (Get-SPFarm).Id
$SaveFile = "SPServiceInstance.xml"

$State = Import-LocalStorage $SaveFile -DefaultValue (New-Object PSObject -Property @{ Objects = @{} })

$List = Get-SPServiceInstance | `
| Select-Object @{n="Type";e={"ServiceInstance"}}, `
			    @{n="FarmId";e={$FarmId}}, `
				Id, Name, DisplayName, Status, Version, `
				@{n="Server";e={$_.Server.Name}}, `
				Hidden, Instance, SystemService, `
				@{n="Service";e={$_.Service.Name}}
				
foreach ($O in $List) {
	$DoEmit = $false
	$Key = $O.Id
	
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
