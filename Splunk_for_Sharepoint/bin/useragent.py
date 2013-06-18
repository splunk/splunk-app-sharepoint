'''
######################################################
#
# Splunk for Microsoft Exchange
# 
# Copyright (C) 2011 Splunk, Inc.
# All Rights Reserved
#
######################################################
'''


import urllib,csv,re,sys

'''
Parse a User Agent string (which must be un-encoded, so de-quote if necessary first)
into the following dict

    {
        "browser":
        "browserversion":
        "os":
        "osvariant":
        "osversion":
    }
    
This is tested against a huge list of browsers from a standard browsecap file in main
so use that
'''
def parse_stuffone(stuff, n, dict):
    sep = stuff[n].split('; ')
    for ss in sep:
        if (ss == 'Windows' or ss == 'Macintosh' or ss == 'Linux' or ss == 'Android'):
            if (dict['os'] == "unknown"):
                dict['os'] = ss
            continue
        if (ss == 'iPhone' or ss == 'iPad' or ss == 'iPod'):
            dict['os'] = 'Apple iOS'
            dict['osvariant'] = ss
            continue
        if (ss.find('Symbian') >= 0):
            dict['os'] = 'Symbian'
            continue
        if (ss == "BlackBerry"):
            dict['os'] = "BlackBerry"
            dict['osvariant'] = "BlackBerry"
            continue
        if (ss == "J2ME/MIDP"):
            dict['osvariant'] = "J2ME"
            continue
        match = re.match('CrOS (\w) ([0-9\.]+)', ss)
        if (match is not None):
            dict['os'] = "Linux"
            dict['osvariant'] = "ChromeOS"
            dict['osversion'] = match.group(2)
            continue
        match = re.match('Windows NT ([0-9\.]+)', ss)
        if (match is not None):
            dict['os'] = "Windows"
            dict['osvariant'] = "Windows NT"
            dict['osversion'] = match.group(1)
            continue
        match = re.match('Linux (.*)', ss)
        if (match is not None):
            dict['os'] = 'Linux'
            dict['osvariant'] = ss
            continue
        match = re.match('FreeBSD (.*)', ss)
        if (match is not None):
            dict['os'] = 'FreeBSD'
            dict['osvariant'] = ss
            continue
        match = re.match('(CPU iPhone|iPhone|iPhone OS|CPU OS|CPU iPhone OS) ([0-9\_\.]+)', ss)
        if (match is not None):
            dict['osversion'] = match.group(2).replace('_','.')
            continue
        match = re.match('(.* Mac OS .*) ([0-9\.\_]+)', ss)
        if (match is not None):
            dict['os'] = "Macintosh"
            dict['osvariant'] = match.group(1)
            dict['osversion'] = match.group(2).replace('_','.')
            continue
        match = re.match('Android (.*)', ss)
        if (match is not None):
            dict['os'] = "Google Android"
            dict['osvariant'] = "Android"
            dict['osversion'] = match.group(1)
            continue
        match = re.match('Series60/([0-9\.]+)', ss)
        if (match is not None):
            dict['osvariant'] = "Series60"
            dict['osversion'] = match.group(1)
            continue
        match = re.match("Opera Mini/([0-9]+\.[0-9]+)", ss)
        if (match is not None):
            dict['browser'] = "Opera Mini"
            dict['browserversion'] = match.group(1)
            continue
        match = re.match("Konqueror/([0-9\.]+)", ss)
        if (match is not None):
            dict['browser'] = "Konqueror"
            dict['browserversion'] = match.group(1)
            
    # If it's Linux, then we have to do more work to find the variants
    if (dict['os'] == "Linux"):
        for ss in stuff:
            match = re.search("(CentOS|Debian|Fedora|Gentoo|Mint|PCLinuxOS|SUSE|Ubuntu)/([0-9a-z\.]+)", ss)
            if (match is not None):
                dict['osvariant'] = match.group(1)
                dict['osversion'] = match.group(2)
        
        
def parse_useragent(useragent):
    dict = { "browser": "unknown", "browserversion": "unknown", "os": "unknown", "osvariant": "unknown", "osversion": "unknown" }

    # If the useragent is not well-formed, then we can't do anything with it
    stuff = useragent.replace(')','(').split('(')
    if (len(stuff) > 1):
        parse_stuffone(stuff, 1, dict)
    
    # Microsoft Internet Explorer
    if (useragent.find('MSIE') > 0):
        dict['browser'] = "Internet Explorer"
        sep = stuff[1].split('; ')
        for ss in sep: 
            match = re.match("MSIE ([0-9]+\.[0-9]+)", ss)
            if (match is not None):
                dict['browserversion'] = match.group(1)
                continue
            match = re.match("(Windows .*) ([0-9\.]+)", ss)
            if (match is not None):
                dict['os'] = "Windows"
                dict['osvariant'] = match.group(1)
                dict['osversion'] = match.group(2)
                continue
            if (ss == "Windows CE"):
                dict['os'] = "Windows Mobile"
                dict['osvariant'] = "Windows CE"
                continue
        return dict

    # Microsoft Entourage
    if (useragent.find('Entourage') >= 0):
        dict['browser'] = "Microsoft Entourage"
        match = re.match("Entourage/([0-9\.]+)", stuff[0])
        dict['browserversion'] = match.group(1)
        match = re.match("([^0-9]+) ([0-9\.\_]+)", stuff[1])
        if (match is not None):
            dict['os'] = "Macintosh"
            dict['osvariant'] = match.group(1)
            dict['osversion'] = match.group(2).replace('_','.')
        return dict
    
    # Google Chrome
    if (useragent.find('Chrome/') > 0):
        dict['browser'] = "Google Chrome"
        for s in stuff:
            match = re.match('.*Chrome/([0-9]+\.[0-9]+)', s)
            if (match is not None):
                dict['browserversion'] = match.group(1)
        return dict
    
    # Opera Browser
    if (useragent.find('Opera/') >= 0):
        dict['browser'] = "Opera"
        match = re.match("Opera/([0-9\.]+)", stuff[0])
        if (match is not None):
            dict['browserversion'] = match.group(1)
        match = re.match("Version/([0-9\.]+)", useragent)
        if (match is not None):
            dict['browserversion'] = match.group(1)
        for x in range(1, len(stuff)-1):
            parse_stuffone(stuff, x, dict)
        return dict        
    
    # Symbian BrowserNG
    if (useragent.find("BrowserNG/") >= 0):
        dict['browser'] = "BrowserNG"
        match = re.search("BrowserNG/([0-9\.]+)", useragent)
        if (match is not None):
            dict['browserversion'] = match.group(1)
        return dict
    
    # Mozilla Firefox 3.x
    if (useragent.find('Firefox') > 0 or useragent.find('Iceweasel') > 0):
        dict['browser'] = "Mozilla Firefox"
        for s in stuff:
            if (s.find("Gentoo") >= 0):
                dict['osvariant'] = "Gentoo"
            match = re.match('.*Firefox/([0-9\.]+)', s)
            if (match is not None):
                dict['browserversion'] = match.group(1)
            # Those sneaky Iceweasel folks think they can hide...
            match = re.match('.*Iceweasel/([0-9\.]+)', s)
            if (match is not None):
                dict['browser'] = "Iceweasel"
                dict['browserversion'] = match.group(1)
        return dict

    # Apple Safari - note order is important here
    if (useragent.find('Safari') > 0):
        dict['browser'] = "Apple Safari"
        for s in stuff:
            match = re.match('.*Version/([0-9\.]+) Safari/', s)
            if (match is not None):
                dict['browserversion'] = match.group(1)
            match = re.match('.*Version/([0-9\.]+) Mobile Safari/', s)
            if (match is not None):
                dict['browser'] = "Mobile Safari"
                dict['browserversion'] = match.group(1)
            match = re.match('.*Version/([0-9\.]+) Mobile/', s)
            if (match is not None):
                dict['browser'] = "Mobile Safari"
                dict['browserversion'] = match.group(1)
        return dict
    
    # Blackberry Browser
    if (stuff[0].find("BlackBerry") >= 0):
        match = re.search("BlackBerry[0-9]+/([0-9]+\.[0-9]+)", useragent)
        if (match is not None):
            dict['os'] = "BlackBerry"
            dict['osvariant'] = "BlackBerry"
            dict['osversion'] = match.group(1)
            dict['browserversion'] = match.group(1)
            dict['browser'] = "BlackBerry"
            return dict
    
    # Other stuff we can find out - pass through each one
    match = re.search("Darwin/([0-9\.\_]+)", useragent)
    if (match is not None):
        dict['os'] = "Macintosh"
        dict['osvariant'] = "Mac OS X"
        dict['osversion'] = match.group(1).replace('_','.')
    
    match = re.search("Mac OS X/([0-9\.\_]+)", useragent)
    if (match is not None):
        dict['os'] = "Macintosh"
        dict['osvariant'] = "Mac OS X"
        dict['osversion'] = match.group(1).replace('_','.')
     
    if (useragent.find("Microsoft Windows XP") >= 0):
        dict['os'] = "Windows"
        dict['osvariant'] = "Windows NT"
        dict['osversion'] = "5.2"
        
    # We have done all we can, so let's return the package
    return dict

#
# Main routine - basically it's the standard python recipe for handling
# Splunk lookups
#
windows_mapping = {
	'5.0':		'Windows 2000',
	'5.1':		'Windows XP',
	'5.2':		'Windows XP/Server 2003',
	'6.0':		'Windows Vista/Server 2008',
	'6.1':		'Windows 7/Server 2008R2',
    '6.2':      'Windows 8/Server 2012'
}

if __name__ == '__main__':
    r = csv.reader(sys.stdin)
    w = csv.writer(sys.stdout)
    have_header = False
    
    header = []
    idx = -1
    for row in r:
        if (have_header == False):
            header = row
            have_header = True
            z = 0
            for h in row:
                if (h == "cs_user_agent"):
                    idx = z
                z = z + 1
            w.writerow(row)
            continue
        
        # We only care about the cs_user_agent field - everything else is filled in
        cs_user_agent = row[idx]
        useragent = urllib.unquote_plus(cs_user_agent)
        dict = parse_useragent(useragent)
        
        # We have a mapping for the WindowsNT stuff to the more normal names
        if dict['osvariant'] == 'Windows NT' and dict['osversion'] in windows_mapping:
        	dict['osvariant'] = windows_mapping[dict['osversion']]
        
        # Now write it out
        orow = []
        for xx in header:
            if (xx == "cs_user_agent"):
                orow.append(cs_user_agent)
            else:
                orow.append(dict[xx])
        w.writerow(orow)
            

