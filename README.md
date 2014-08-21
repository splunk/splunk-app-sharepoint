The Splunk App for SharePoint
=============================

License
=======
Splunk App for Microsoft SharePoint

Copyright 2013-14 Splunk Inc.  All rights reserved.

Licensed under the Apache License, Version 2.0 (the “License"); you may not use this file except in compliance with the License.
You may obtain a copy of the License at:   http://www.apache.org/licenses/LICENSE-2.0
 
Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
“AS IS” BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the License for the specific 
language governing permissions and limitations under the License.

Introduction
============
The Splunk App for SharePoint provides dashboards and saved searches for use in a Splunk 6 environment when monitoring SharePoint
2010 or SharePoint 2013 with the appropriate Technology Add-on (available from http://apps.splunk.com/)

This release does not provide the data inputs - just the required app.

Build Instructions
==================
To build the Splunk App for SharePoint, you will need a recent version of Java and Apache Ant 1.9.2.

To build a full distribution:
	ant dist

To build the directories, but not package:
	ant build
	
The build is created within the build directory, and the distribution file is built in the build/dist directory.

Contributing to the project
===========================
Fork this project, do your changes, then send us a pull request - we will merge or request more information.

Ask questions, contribute and provide support by joining the splunk-app-sharepoint mailing list here:  
	https://groups.google.com/forum/#!forum/splunk-app-sharepoint

File issues and requests under the GitHub Issues Tracker.	
	