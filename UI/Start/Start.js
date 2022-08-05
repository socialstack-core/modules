import App from './App.js';

const providers = [];

export default function start(custom){
	if(!custom){
		// Init all modules.
		for(var m in __mm){
			var moduleValue = __mm[m] && global.require(m);
			var prov = moduleValue.Provider;
			if(prov) {
				if(!prov.priority){
					prov.priority = 10;
				}
				providers.push(prov);
			}
		}
		
		// Sorted backwards because the App component stacks them up in reverse
		providers.sort((a,b) => {return b.priority - a.priority;});
	}
	
	var root = document.getElementById('react-root');
	
	if(root){
		// We're server side otherwise.
		
		// Start logging in now and update the loader:
		var loader = document.getElementById('react-loading');
		loader && loader.parentNode.removeChild(loader);
		
		// Reset load cache index:
		global.cIndex = 0;
		
		// Render the root now! When the App instance is created, it sets itself up as this.context.app (as seen by other mounted components).
		(root.childNodes.length ? React.hydrate : (React.render || ReactDom.render))(
			<App providers = {providers}/>,
			root
		);
		
		// Mark load cache index as used:
		global.cIndex = undefined;
	}
};