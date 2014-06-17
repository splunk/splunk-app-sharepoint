function Get-LocalIPAddress
{
    $AdapterSet = Get-WmiObject -Class Win32_NetworkAdapterConfiguration
    $IPAddressSet = @()
    foreach ($adapter in $AdapterSet) {
        foreach ($ipaddress in $adapter.IPAddress) {
            $IPAddressSet = $IPAddressSet + $ipaddress
        }
    }
    $IPAddressSet
} 

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

function Get-NetworkConnections
{
    PROCESS
    {
        netstat -no | Select-String "\s+TCP" | Foreach-Object {
            $arr = $_.Line.Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)

            if ($arr[1] -match ":\d+$") {
                $localPort = $arr[1].Split(':')[-1]
            } else {
                $localPort = $null
            }
            if ($arr[1] -match "^\d+\.\d+\.\d+\.\d+:") {
                $localAddress = $arr[1].Split(':')[0]
            } elseif ($arr[1] -match "^\[[0-9a-f:]+%\d+\]:") {
                $localAddress = $arr[1].Split('%')[0].Remove(0,1)
            } else {
                $localAddress = $null
            }

            if ($arr[2] -match ":\d+$") {
                $remotePort = $arr[2].Split(':')[-1]
            } else {
                $remotePort = $null
            }
            if ($arr[2] -match "^\d+\.\d+\.\d+\.\d+:") {
                $remoteAddress = $arr[2].Split(':')[0]
            } elseif ($arr[2] -match "^\[[0-9a-f:]+%\d+\]:") {
                $remoteAddress = $arr[2].Split('%')[0].Remove(0,1)
            } else {
                $remoteAddress = $null
            }

            $obj = New-Object -TypeName PSObject -Property @{
                Protocol = $arr[0]
                LocalEndpoint = $arr[1]
                LocalPort = $localPort
                LocalAddress = $localAddress
                RemoteEndpoint = $arr[2]
                RemotePort = $remotePort
                RemoteAddress = $remoteAddress
                State = $arr[3]
                PID = $arr[4]
            }

            $obj
        }
    }
}

$LocalIPAddresses = Get-LocalIPAddress
Get-NetworkConnections `
    | Where-Object { $LocalIPAddresses -contains $_.LocalAddress -and $_.State -eq "ESTABLISHED" } `
    | Select-Object -Unique RemoteAddress `
    | Foreach-Object { Test-Connection -Count 1 -ComputerName $_.RemoteAddress -ErrorAction SilentlyContinue } `
    | Select-Object Address,ResponseTime `


