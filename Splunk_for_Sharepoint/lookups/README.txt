Farm-Friendly Naming

Edit the file FarmNames.csv.  Each line should contain one Farm 
specified by Id and it's "friendly" name.  For example

FarmId,FarmName
730eee5b-de80-4b27-9b11-d8ff7aca7509,Production
aeeeb157-e531-4969-9f54-8050a1dbc001,Development

The FarmId must be listed as the Id in the SPFarm.csv lookup and the
FarmName can be anything you want.  Use standard CSV formatting (i.e.
quote if necessary).


Office Locations

Edit the file OfficeLocations.csv.  Each line should contain a CIDR
block and a friendly name for the office.  For example:

c_ip,office
10.155.80.0/24,New York
10.155.82.0/23,San Francisco
10.155.84.0/24,Bethesda
10.155.85.0/28,Seattle
10.155.85.16/28,Denver
10.160.0.0/16,VPN

Note the VPN field.  This is how we associate the VPN access dashboards. 
Ensure that the VPN concentrator IP address range is called "VPN".
