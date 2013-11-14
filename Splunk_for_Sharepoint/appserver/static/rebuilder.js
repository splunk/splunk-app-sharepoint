require.config({
	paths: { "app": "../app" }
});

require([
	'jquery',
	'splunkjs/mvc',
	'splunkjs/mvc/savedsearchmanager',
	'splunkjs/mvc/utils',
	'splunkjs/mvc/simplexml/ready!'
], function($, mvc, SearchManager, utils) {

	// List of saved searches that we want to generate
	document.Lookups = {
		List: [
			'Lookup - Host Information',
			'Lookup - IP Address',
			'Lookup - SharepointGuid',
			'Lookup - SPApplicationPool',
			'Lookup - SPContentDatabase',
			'Lookup - SPFarm',
			'Lookup - SPList',
			'Lookup - SPServer',
			'Lookup - SPServiceInstance',
			'Lookup - SPSite',
			'Lookup - SPWeb',
			'Lookup - SPWebApplication',
			'Lookup - SPWebTemplate',
			'Lookup - SPUser'
		],
		Complete:	0,
		Current:	"",
		// This kicks off a new search
		NewSearch: 	function() {
			var ll = document.Lookups;

			// If Current === "", then it's a new loop
			if (ll.Current !== "") {
				// It's a completed one
				ll.Complete++;
			}
			$('#lookups_not_started').html('<h1>'+ll.List.length+'</h1>');
			$('#lookup_in_process').html('<h1></h1>');
			$('#lookups_completed').html('<h1>'+ll.Complete+'</h1>');

			if (ll.List.length > 0) {
				// Kick off a new Search
				var srch = ll.List.shift();
				$('#lookup_in_process').html('<h1>'+srch+'</h1>');
				ll.Current = new SearchManager({
					id: 'SPLookupSearchBuilder-'+ll.Complete,
					app: utils.getCurrentApp(),
					autoStart: false,
					cache: false,
					cancelOnUnload: true,
					searchname: srch,
				});
				ll.Current.on('search:done', ll.NewSearch);
				ll.Current.startSearch();
			} else {
				$('#lookup_in_process').html('<h1>COMPLETED!</h1>');
			}
		}
	};

	$('#start_lookups').click(document.Lookups.NewSearch);
});
