/*
** There is a "feature" in Splunk 5.0.x where you can't click through
** a Single Value without a search, which replaces the first search 
** on the page.  This fixes that issue.
*/
$(document).ready(function() {

	/*
	** Health Overview - Farm Count Click-through
	*/
	$(".MSSP_Ops_Farm").click(function() {
		document.location.href = "/app/Splunk_for_Sharepoint/ops_farm";
	});
})
