from array import *
from urlparse import urlsplit
import csv, sys, os

# Instantiate the lookup table locations
fLookups = os.path.join(os.environ['SPLUNK_HOME'], 'etc', 'apps', 'Splunk_for_Sharepoint', 'lookups')
fWebApps = os.path.join(fLookups, 'SPWebApplication.csv')
fSites   = os.path.join(fLookups, 'SPSite.csv')
fWebs    = os.path.join(fLookups, 'SPWeb.csv') 

def parseurl(url):
	if (url.find(';')!=-1):
		(zone, url) = url.split(';')
	else:
		zone = "Default"
	o = urlsplit(url)
	if (o.scheme == "http"):
		port = 80
	elif (o.scheme == "https"):
		port = 443
	if (o.netloc.find(":")!=-1):
		(host,port) = o.netloc.split(':')
	else:
		host = o.netloc
	return (host, port, zone)

def load_csv(fName, sortField):
	ll = list()
	
	with open(fName, 'rb') as csvfile:
		reader = csv.DictReader(csvfile)
		for row in reader:
			ll.append(row)
	ll.sort(reverse=True, key=lambda l: len(l[sortField]))
	return ll

def load_webapps():
	webapps = list()
	
	with open(fWebApps, 'rb') as csvfile:
		reader = csv.DictReader(csvfile)
		for row in reader:
			url_list = row['UrlList'].split(',')
			for url in url_list:
				(host, port, zone) = parseurl(url)
				webapps.append({'host':host,'port':port,'zone':zone,'Id':row['Id']})
	return webapps

def lookup_webapp(webapps, s_host, s_port):
	for o in webapps:
		if (o['host'] == s_host and o['port'] == s_port):
			return o
	return None

def lookup_csv(objs, field, id, uristem):
	for o in objs:
		if (o[field] == id and uristem.startswith(o['ServerRelativeUrl'])):
			return o
	return None

if __name__ == '__main__':
	webapps = load_webapps()
	sites   = load_csv(fSites, "ServerRelativeUrl")
	webs    = load_csv(fWebs,  "ServerRelativeUrl")
	
	reader = csv.DictReader(sys.stdin)
	writer = csv.DictWriter(sys.stdout, fieldnames=reader.fieldnames)
	writer.writeheader()
	
	for row in reader:
		webapp = lookup_webapp(webapps, row['s_ip'], row['s_port'])
		if (webapp is not None):
			site = lookup_csv(sites, 'WebApplication', webapp['Id'], row['cs_uri_stem'])
			if (site is not None):
				web = lookup_csv(webs, 'Site', site['Id'], row['cs_uri_stem'])
				if (web is not None):
					row['SPWebApplication']  = webapp['Id']
					row['SPSite']            = site['Id']
					row['SPContentDatabase'] = site['ContentDatabase']
					row['SPFarm']            = site['Farm']
					row['SPWeb']             = web['Id']
		writer.writerow(row)