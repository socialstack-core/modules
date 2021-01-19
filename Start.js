import App from './App.js';

export default function(custom){
	
	if(!custom){
		// Setup a simple event mechanism for communication between modules (note: EventTarget isn't available in Node):
		global.events = {
			get: function(name){
				if(!global.events[name]){
					var t = {};
					t.add = (evtName, handle) => {
						if(!t[evtName]){
							var f = function(...args){
								for(var i=0;i<f.handles.length;i++){
									f.handles[i](...args);
								}
							};
							f.handles=[handle];
							t[evtName] = f;
						}else{
							t[evtName].handles.push(handle);
						}
					};
					return global.events[name] = t;
				}
				return global.events[name];
			}
		};
		
		// Init all modules.
		for(var m in __mm){
			__mm[m] && global.getModule(m);
		}
	}
	
	var document = global.document;
	
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
		
		// Specially look out for in-page edits:
		var editMode = false;
		if(global.hashFields.edit && global.hashFields.id && global.hashFields.field){
			global.events.get('UI/Functions/WebRequest')['on' + global.hashFields.edit] = (content) => {
				if(content.id == global.hashFields.id){
					var fld = global.hashFields.field;
					// Update content[global.hashFields.field] by wrapping it with a CanvasEditor.
					
					if(fld.indexOf("Json") != -1){
						// Only permitted on canvas JSON. We inject a CanvasEditor by simply wrapping the canvas JSON in an editor:
						var data = {
							live: true,
							type: global.hashFields.edit,
							id: global.hashFields.id,
							field: fld,
							value: content[fld]
						};
						content[fld] = '{"module": "UI/CanvasEditor", "data": ' + JSON.stringify(data) + '}';
						editMode = true;
					}
				}
			}
		}
		
		// ctrl + e for in-page edit:
		document.addEventListener("keydown", e => {
			if(e.defaultPrevented || !global.app.state.user || !global.pageRouter){
				return;
			}
			
			var pg = global.pageRouter.state.page;
			
			if(pg && e.ctrlKey){
				
				// ctrl + e
				if(!editMode && e.keyCode == 69){
					e.preventDefault();
					// This will force a full page reload:
					var l = document.location;
					l.assign(l.pathname + '#edit=Page&field=bodyJson&id=' + pg.id);
					l.reload();
				}
				
				// ctrl + s
				if(editMode && e.keyCode != 83){
					e.preventDefault();
					// This will force a full page reload:
					var l = document.location;
					l.assign(l.pathname);
					l.reload();
				}
				
			}
			
		});
		
		// Render the root now! When the App instance is created, it sets itself up as global.app
		(React.render || ReactDom.render)(
			<App />,
			document.getElementById('react-root')
		);
		
	}
	
};