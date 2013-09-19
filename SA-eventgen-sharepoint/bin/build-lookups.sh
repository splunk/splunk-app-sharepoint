#!/bin/sh

splunk search '|savedsearch "Lookup - Host Information"'
splunk search '|savedsearch "Lookup - IP Address"'
splunk search '|savedsearch "Lookup - SPContentDatabase"'
splunk search '|savedsearch "Lookup - SPFarm"'
splunk search '|savedsearch "Lookup - SPList"'
splunk search '|savedsearch "Lookup - SPServer"'
splunk search '|savedsearch "Lookup - SPSite"'
splunk search '|savedsearch "Lookup - SPUser"'
splunk search '|savedsearch "Lookup - SPWeb"'
splunk search '|savedsearch "Lookup - SPWebApplication"'
splunk search '|savedsearch "Lookup - SPWebTemplate"'
splunk search '|savedsearch "Lookup - SharepointGuid"'

