import App from './App.js';

export default function(custom){
	if(!custom){
		// Init all modules.
		for(var m in __mm){
			__mm[m] && global.getModule(m);
		}
	}
	
	var root = document.getElementById('react-root');
	
	if(root){
		// We're server side otherwise.
		
		// Start logging in now and update the loader text:
		var loader = document.getElementById('react-loading');
		loader && loader.parentNode.removeChild(loader);
		
		// Reset load cache index:
		global.cIndex = 0;
		
		// Render the root now! When the App instance is created, it sets itself up as this.context.app (as seen by other mounted components).
		(root.childNodes.length ? React.hydrate : (React.render || ReactDom.render))(
			<App />,
			root
		);
		
		// Mark load cache index as used:
		global.cIndex = undefined;
	}
};