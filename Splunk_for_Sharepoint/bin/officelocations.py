from array import *
import csv, sys, os
import ipaddr

fLookups = os.path.join(os.environ['SPLUNK_HOME'], 'etc', 'apps', 'Splunk_for_Sharepoint', 'lookups')
fOffices = os.path.join(fLookups, "OfficeLocations.csv")

def load_csv(fName):
	ll = list()
	
	with open(fName, 'rb') as csvfile:
		reader = csv.DictReader(csvfile)
		for row in reader:
			ll.append(row)
	return ll

def lookup_csv(lookup, field):
	try:
		address = ipaddr.ip_address(field)
	except ValueError:
		return None
		
	for v in lookup:
		try:
			network = ipaddr.ip_network(v['c_ip'])
			if address in network:
				return v
		except ValueError:
			pass
	return None

if __name__ == '__main__':
	offices = load_csv(fOffices)
	
	reader = csv.DictReader(sys.stdin)
	writer = csv.DictWriter(sys.stdout, fieldnames=reader.fieldnames)
	writer.writeheader()
	
	for row in reader:
		ipAddr = row['c_ip']

		if (ipAddr is not None):
			office = lookup_csv(offices, ipAddr)
			if (office is not None):
				row['office'] = office['office']
				row['office_lat'] = office['office_lat']
				row['office_lon'] = office['office_lon']
		writer.writerow(row)