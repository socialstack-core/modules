import App from './App.js';

if(typeof document != 'undefined'){
	// We're server side otherwise. It would've set global.app internally.

	/*
	* Navigates in a safe in-page way.
	*/
	global.navigateReact = function(url, data){
		global.__history.push(url);
	};

	// Start logging in now and update the loader text:
	var loader = document.getElementById('react-loading');
	if(loader){
		loader.parentNode.removeChild(loader);
	}

	global.hashFields = {};

	(global.location && global.location.hash) && global.location.hash.substring(1).split('&').forEach(e => {
		var parts = e.split('=');
		global.hashFields[parts[0]] = parts.length>1 ? parts[1] : true;
	});

	// Render the root now! When the App instance is created, it sets itself up as global.app
	React.render(
		<App />,
		document.getElementById('react-root')
	);
	
}