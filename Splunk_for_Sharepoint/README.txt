Notes for installing the TA-Sharepoint2010

1) Certain things (such as audit) only run on Central Administration

2) The Splunk instance that is running on Central Administration MUST be a domain user

3) That domain user MUST be a farm administrator

4) that domain user MUST be a sysadmin on the SQL Server that houses Sharepoint_Config and WSS_Content databases

These are not optional.  No, you can't run as NT AUTHORITY\SYSTEM.  Sorry - this is Sharepoint issue, not Splunk issue.