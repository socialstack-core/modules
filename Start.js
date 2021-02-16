import App from './App.js';

export default function(custom){
	
	if(!custom){
		// Init all modules.
		for(var m in __mm){
			__mm[m] && global.getModule(m);
		}
	}
	
	if(!global.server){
		// We're server side otherwise. It would've set global.app internally.
		
		// Start logging in now and update the loader text:
		var loader = document.getElementById('react-loading');
		if(loader){
			loader.parentNode.removeChild(loader);
		}

		global.hashFields = {};

		(location && location.hash) && location.hash.substring(1).split('&').forEach(e => {
			var parts = e.split('=');
			global.hashFields[parts[0]] = parts.length>1 ? parts[1] : true;
		});
		
		// Render the root now! When the App instance is created, it sets itself up as global.app and this.context.app from components (use context.app whenever possible).
		(React.render || ReactDom.render)(
			<App />,
			document.getElementById('react-root')
		);
		
	}
	
};