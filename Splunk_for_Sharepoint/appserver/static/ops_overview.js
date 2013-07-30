/*
** There is a "feature" in Splunk 5.0.x where you cannot click through
** a single value without a search, which replaces the first search on
** a page.  This is unfortunately replicated in Splunk 6.0.x.  This 
** code fixed that issue.
*/
require([ 
	'jquery',
	'splunkjs/mvc',
	'splunkjs/mvc/simplexml/ready!' 
], function($, mvc) {
	$("#element1").on("click", ".MSSP_Ops_Farm", function(event) {
		document.location.href = "/app/Splunk_for_Sharepoint/ops_farm";
	});
});
