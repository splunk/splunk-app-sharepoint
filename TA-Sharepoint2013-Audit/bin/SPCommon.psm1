function Count-Object
{
	BEGIN 	{ $count = 0  }
	PROCESS { $count += 1 }
	END 	{ $count      }
}

<#
	.SYNOPSIS
		Load-PsSnapIn
			
	.DESCRIPTION
		Loads a PS SnapIn if it isn't already loaded
#>
function Load-PsSnapIn
{
	[CmdletBinding()]
	param(
		[Parameter(Mandatory=$true)]
		$SnapIn
	)

	PROCESS {
		$Count = Get-PsSnapIn | Where-Object { $_.Name -eq $SnapIn } | Count-Object
		if ($Count -eq 0) {
			Write-Verbose "Loading SnapIn $SnapIn"
			Add-PsSnapIn $SnapIn
		}
	}
}

<#
    .SYNOPSIS
        Get-Checksum

    .DESCRIPTION
        Returns the MD5 Checksum for an object, by concatenating all the Properties together in
        alphabetical order and then calculating the MD5 sum

    .NOTES
        Dependencies: None
#>
function Get-Checksum
{
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory=$True)]
        [object]$Object
    )

    PROCESS {
        $MD5 = New-Object -TypeName System.Security.Cryptography.MD5CryptoServiceProvider
        $UTF8 = New-Object -TypeName System.Text.UTF8Encoding

        [string]$str = ""
        $Object.PSObject.Properties | Sort-Object -Property Name | Foreach-Object {
            if ($_.Value -eq $Null) {
                $str = $str + "{$($_.Name)=null}"
            } else {
                $str = $str + "{$($_.Name)=" + $_.Value.ToString() + "}"
            }
       }

        return [System.BitConverter]::ToString($MD5.ComputeHash($UTF8.GetBytes($str)))
    }
}
