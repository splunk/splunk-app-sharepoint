<#
    .SYNOPSIS
        & .\Invoke-MonitoredScript.ps1 "MyScript.ps1"
        
    .DESCRIPTION
        Outputs additional Splunk events related to the running and
        errors in the script.
#>
[CmdletBinding()]
param(
    #Command to execute.
    [Parameter(Position=0, Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string] $Command,
	
	# Splunk Sourcetype Prefix for generated events
    [Parameter()]
    [ValidateNotNull()]
    [string] $SourceTypePrefix="Powershell:",
	
	# Maximum number of errors to convert into events
    [Parameter()]
    [ValidateRange(0, 100)]
    [int] $MaxErrorCount
)

$WrappedScriptExecutionSummary= New-Object -TypeName PSObject -Property (
    [ordered]@{
        SplunkSourceType="$($SourceTypePrefix)ScriptExecutionSummary";
        Identity=[guid]::NewGuid().ToString();
        InvocationLine=$MyInvocation.Line;
        TerminatingError=$false; ErrorCount=0; Elapsed=""
    })
$originalLocation = Get-Location

try
{
    Set-Location (Split-Path -Parent $MyInvocation.MyCommand.Definition)
    $ScriptStopWatch = [System.Diagnostics.Stopwatch]::StartNew()
    $Error.Clear()
    Invoke-Expression $Command
}
catch
{
    $WrappedScriptExecutionSummary.TerminatingError = $true;
}
finally
{
    Set-Location $originalLocation
    $WrappedScriptExecutionSummary.Elapsed = $ScriptStopWatch.Elapsed.ToString("hh\:mm\:ss\.fff")
    $WrappedScriptExecutionSummary.ErrorCount = $Error.Count
        
    if ($Error.Count -gt 0) {
        $ei = $Error.Count - 1
        if ($PSBoundParameters.ContainsKey('MaxErrorCount')) {
            if ($MaxErrorCount -lt $Error.Count) {
                $ei = $MaxErrorCount - 1
            }
            # Always emit terminating errors
            if ($ei -eq -1 -and $WrappedScriptExecutionSummary.TerminatingError) {
                $ei = 1
            }
        }

        for(; $ei -ge 0; $ei--) {
            $errorRecord = New-Object -TypeName PSObject -Property (
                [ordered]@{
                    SplunkSourceType="$($SourceTypePrefix)ScriptExecutionErrorRecord";
                    ParentIdentity=$WrappedScriptExecutionSummary.Identity;
                    ErrorIndex=$ei;
                    ErrorMessage=$Error[$ei].ToString();
                    PositionMessage=$Error[$ei].InvocationInfo.PositionMessage;
                    CategoryInfo=$Error[$ei].CategoryInfo.ToString();
                    FullyQualifiedErrorId=$Error[$ei].FullyQualifiedErrorId
                })

            if ($Error[$ei].Exception -ne $null) {
                Add-Member -InputObject $errorRecord -MemberType NoteProperty -Name Exception -Value $Error[$ei].Exception.ToString()
                if ($Error[$ei].Exception.InnerException -ne $null) {
                    Add-Member -InputObject $errorRecord -MemberType NoteProperty -Name InnerException -Value $Error[$ei].Exception.InnerException.ToString()
                }
            }

            Write-Output $errorRecord
        }
    }

    Write-Output $WrappedScriptExecutionSummary
}