Preparing to Install the Splunk Technology Add-on for SharePoint 2010
=====================================================================

1.	Identify a single SharePoint 2010 server in each farm to be the Audit Reader.

This is a special server that gathers the inventory information and audit logs for the entire farm.

2.	Install Windows PowerShell 3.0 and the .NET Framework 4.5 on each server.

These items are freely downloadable from Microsoft.

3.	Create a service account for the Audit Reader in the domain.

4.	Use Add-SPShellAdmin to add the service account to the SharePoint Shell Admin group.

Installing the Splunk Universal Forwarder on the Audit Reader
=============================================================

Install the Splunk Universal Forwarder as a domain user using the service account you
created in the first section.  Do not use Low-Priv for this - it must be granted full
access to the system.

Splunk Universal Forwarder v6.1.1 for Windows x64 is required.

Installing the Splunk Universal Forwarder on all other SharePoint systems
=========================================================================

You can install the Splunk Universal Forwarder as the local system account or a domain
user with appropriate permissions to read everything.  Low-Priv is not supported for
SharePoint deployments.  The local system account is recommended.

Splunk Universal Forwarder v6.1.1 for Windows x64 is required.

Installing the Splunk Technology Add-on for SharePoint 2010
===========================================================
To gather all data from a SharePoint 2010 server:

1.	Deploy a copy of TA-SharePoint2010 "as downloaded" to the indexers and search
	heads.  Restart the indexers to create the appropriate indices.
1.	Download Splunk Technology Add-on for Windows (v4.6.6 or later) and configure it to 
	gather the Security, Application and System Windows Event Logs.  Deploy this to all 
	SharePoint servers.
2.	Download and deploy the SA-ModularInput-PowerShell (v1.1 or later) to all SharePoint
	servers.
3.	Create a copy of the TA-SharePoint2010 add-on and enable all inputs.  Deploy this to the
	Audit Reader.
4.	For all other servers, enable all inputs EXCEPT sp10audit and sp10inventory (at the top
	of the inputs.conf file), then deploy the TA-SharePoint2010 to all other SharePoint 
	servers EXCEPT the audit reader.

	
Overview
========
	Search-heads:	
		Splunk_TA_windows (all inputs disabled)
		TA-SharePoint2010 (all inputs disabled)
	Indexers:		
		TA-SharePoint2010 (all inputs disabled)
	Audit Reader:	
		Splunk_TA_windows (standard Event Logs)
		TA-SharePoint2010 (all inputs enabled)
		SA-ModularInput-PowerShell
	Other Servers:	
		Splunk_TA_windows (standard Event Logs)
		TA-SharePoint2010 (all but sp10audit and sp10inventory inputs enabled)
		SA-ModularInput-PowerShell
		
	



