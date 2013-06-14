from array import *
import csv, sys, os

# Instantiate the lookup table locations
fLookups = os.path.join(os.environ['SPLUNK_HOME'], 'etc', 'apps', 'Splunk_for_Sharepoint', 'lookups')
fSites   = os.path.join(fLookups, 'SPSite.csv')
fWebs    = os.path.join(fLookups, 'SPWeb.csv') 

def load_csv(fName, sortField):
	ll = list()
	
	with open(fName, 'rb') as csvfile:
		reader = csv.DictReader(csvfile)
		for row in reader:
			ll.append(row)
	ll.sort(reverse=True, key=lambda l: len(l[sortField]))
	return ll

def lookup_csv(objs, field, id, uristem):
	for o in objs:
		if (o[field] == id and uristem.startswith(o['ServerRelativeUrl'])):
			return o
	return None

if __name__ == '__main__':
	sites   = load_csv(fSites, "ServerRelativeUrl")
	webs    = load_csv(fWebs,  "ServerRelativeUrl")
	
	reader = csv.DictReader(sys.stdin)
	writer = csv.DictWriter(sys.stdout, fieldnames=reader.fieldnames)
	writer.writeheader()
	
	for row in reader:
		webAppId = row['WebApplicationId']

		if (webAppId is not None):
			site = lookup_csv(sites, 'WebApplicationId', webAppId, row['cs_uri_stem'])
			if (site is not None):
				web = lookup_csv(webs, 'SiteId', site['Id'], row['cs_uri_stem'])
				if (web is not None):
					row['SiteId']            = site['Id']
					row['ContentDatabaseId'] = site['ContentDatabaseId']
					row['WebId']             = web['Id']
		writer.writerow(row)