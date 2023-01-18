import TestComponent from './TestComponent';
import ThemePreview from './ThemePreview';
import Loading from 'UI/Loading';
import webRequest from 'UI/Functions/WebRequest';
import PanelledEditor from 'Admin/Layouts/PanelledEditor';
import {calculateSpecifity, compareSpecifity} from './Specifity';
import { ErrorCatcher } from 'UI/Canvas';

// Connect the input "ontypecanvas" render event:
var inputTypes = global.inputTypes = global.inputTypes || {};

// type="theme"
inputTypes.ontypetheme = function(props, _this){
	return <>
		<ThemeEditor name={props.name} value={props.value} />
	</>;
};

function collectPropertiesUsingVariables(targetEle){
	var properties = {};
	
	// Collects all applied properties which use variables (usually originating from a theme but not necessarily) on the given element.
	// Considers specifity such that if multiple rules apply, the most specific one wins.
	
	for (var x = 0; x < document.styleSheets.length; x++) {
		var rules = document.styleSheets[x].cssRules;
		for (var i = 0; i < rules.length; i++) {
			var selector = rules[i].selectorText;
			
			// The element might be in the hover/ focus/ active state currently, so omit those ones.
			if (targetEle.matches(selector) && selector.indexOf(':hover') === -1 && selector.indexOf(':focus') === -1 && selector.indexOf(':active') === -1) {
				
				// Found an applied selector. Does it use theme vars?
				var rule = rules[i];
				var style = rule.style;
				var specifity = null;
				
				for (var n = style.length; n--;) {
					var property = style[n];
					var propValue = style[property];
					
					if(!propValue || !typeof propValue === 'string'){
						continue;
					}
					
					propValue = propValue.trim();
					
					if(propValue.indexOf('var(') === 0){
						
						// At least one. Calc specifity if it's now needed:
						if(!specifity){
							specifity = calculateSpecifity(selector);
							
							// If the selector had more than one part, find the most specific one for the target element.
							if(specifity.length > 1){
								
								var mostSpecific = null;
								
								specifity.forEach(selectorPartSpecifity => {
									
									if(targetEle.matches(selectorPartSpecifity.selector)){
										
										// Is this one more specific than the current most specific?
										if(!mostSpecific){
											mostSpecific = selectorPartSpecifity;
										}else{
											if(compareSpecifity(selectorPartSpecifity, mostSpecific) == 1){
												// It's more specific.
												mostSpecific = selectorPartSpecifity;
											}
										}
										
									}
									
								});
								
								specifity = mostSpecific ? mostSpecific : specifity[0];
							}else{
								specifity = specifity[0];
							}
						}
						
						var varName = propValue.substring(4, propValue.length - 1).trim(); // remove var( and the ).
						
						// Should now start with -- as well.
						if(varName.indexOf('--') === 0){
							varName = varName.substring(2);
						}
						
						var set = properties[varName];
						
						if(!set){
							set = {
								specifity,
								value: varName
							};
							properties[property] = set;
						}else{
							// Is this property more specific?
							if(compareSpecifity(specifity, set.specifity) == 1){
								// Yes! this one wins.
								set.specifity = specifity;
								set.value = varName;
							} 
						}
						
					}
				}
			}
		}
	}
	
	return properties;
}

function getLocalTheme(element){
	return element.getAttribute("data-theme");
}

function collectThemes(element, allThemes){
	var themes = [];
	
	while(element){
		var themeKey = getLocalTheme(element);
		
		if(themeKey){
			// The first theme in the array "wins" on the priority.
			var theme = allThemes[themeKey.toLowerCase()];
			
			if(theme){
				themes.push(theme);
			}else{
				console.warn("Theme with key '" + themeKey +"' was not found. It is used in the DOM hierarchy of the element you just clicked on.");
			}
		}
		
		element = element.parentElement;
	}
	
	return themes;
}

function findVariableInThemes(varName, themes){
	// Find the first theme that has the given variable. Stop when it is found.
	for(var i=0;i<themes.length;i++){
		var theme = themes[i];
		
		if(theme.data.variables && theme.data.variables[varName] !== undefined){
			return theme;
		}
	}
	
	return null;
}

function renderNode(node){
	var NodeType = node.type;
	
	if(!node.dom){
		node.dom = React.createRef();
	}
	
	// Node is a canvas editor structured node.
	// It could be a graph node or a regular node (with child nodes as well).
	if(node.graph){
		
		// Graph node.
		return <ErrorCatcher node={node}>{node.graph.render()}</ErrorCatcher>;
	
	}else{
		// Custom component
		var props = {...node.props, _rte: this};
		
		if(node.roots){
			var children = null;
			
			for(var k in node.roots){
				var root = node.roots[k];
				
				var isChildren = k == 'children';
				
				if(!root.dom){
					root.dom = React.createRef();
				}
				
				if(!root.key){
					root.key = nodeKeys++;
				}
				
				var rendered = <div key={root.key} className="rte-component" ref={root.dom}>{renderRootNode(root)}</div>;
				
				if(isChildren){
					children = rendered;
				}else{
					props[k] = rendered;
				}
			}
			
			return <div key={node.key} className="rte-component" ref={node.dom}>
					<ErrorCatcher node={node}><NodeType {...props}>{children}</NodeType></ErrorCatcher>
				</div>;
			
		}else{
			// It has no content inside it; it's purely config driven.
			return <div key={node.key} className="rte-component" ref={node.dom}>
				<ErrorCatcher node={node}><NodeType {...props} /></ErrorCatcher>
			</div>;
		}
	}
}

function renderRootNode(node){
	if(!Array.isArray(node.content)){
		throw new Error("Root nodes must have an array as their content.");
	}
	var nodeSet = node.content.map((n,i) => this.renderNode(n));
	return nodeSet;
}

function getGraphRoot(graph){
	if(!graph.structure || !Array.isArray(graph.structure.c)){
		return null;
	}
	
	var c = graph.structure.c;
	
	// c(ontent) is an array of nodes. One has r:true.
	var root = null;
	
	for(var i=0;i<c.length;i++){
		if(c[i].r){
			root = c[i];
			break;
		}
	}
	
	return root;
}

function getCurrentTheme(node){
	if(!node){
		return null;
	}
	
	if(node.graph){
		// Find the root component and check for a d field on it.
		var root = getGraphRoot(node.graph);
		if(!root || !root.d){
			return null;
		}
		
		// d only - can't link theme dynamically (at least, not with this editor anyway).
		return root.d['data-theme'];
	}
	
	// Otherwise it's in the node props:
	var props = node.props || node.data || node.d;
	return props ? props['data-theme'] : null;
}

function setCurrentTheme(node, key){
	if(!node){
		return;
	}
	
	if(node.graph){
		// Find the root component and check for a d field on it.
		var root = getGraphRoot(node.graph);
		if(!root){
			// Can't set the theme on a graph with no root node.
			return;
		}
		
		if(!root.d){
			root.d = {};
		}
		
		// Set the theme:
		root.d['data-theme'] = key;
		return;
	}
	
	// Otherwise it's in the node props:
	var props = node.props || node.data || node.d;
	
	if(!props){
		node.props = props = {};
	}
	
	props['data-theme'] = key;
}

export default function ThemeEditor(props){
	
	var [allThemes, setAllThemes] = React.useState();
	
	/*
	var [previewButton, setPreviewButton] = React.useState(1);
	
	React.useEffect(() => {
		
		var i = setInterval(() => {
			
			setPreviewButton(previewButton == 8 ? 1 : previewButton+1);
			
		}, 2000);
		
		return () => clearInterval(i);
		
	}, [previewButton]);
	*/
	
	React.useEffect(() => {
		
		// Get all themes:
		webRequest('configuration/list').then(response => {
			
			var configs = response.json.results;
			
			// filter to just themes:
			var themeConfigs = configs.filter(config => config.key && config.key.toLowerCase() == 'theme');
			
			// Build an obj by theme key:
			var themes = {};
			
			themeConfigs.forEach(cfg => {
				
				try{
					var theme = JSON.parse(cfg.configJson);
					
					if(theme.Key && !theme.key){
						theme.key = theme.Key;
					}
					
					if(theme.Css && !theme.css){
						theme.css = theme.Css;
					}
					
					if(theme.Variables && !theme.variables){
						theme.variables = theme.Variables;
					}
					
					var key = theme.key;
					
					if(!key){
						console.warn("Theme #" + cfg.id + " is invalid because it doesn't have a key in its config.");
					}else{
						themes[key.toLowerCase()] = {id: cfg.id, config: cfg, data: theme, key};
					}
				}catch(e){
					console.warn("Theme #" + cfg.id + " is invalid (its json doesn't load). Here's why: ", e);
				}
			});
			
			setAllThemes(themes);
		});
		
	}, []);
	
	if(!allThemes){
		return <Loading />;
	}
	
	// Get current theme name:
	var currentThemeKey = getCurrentTheme(props.node);
	var currentTheme = currentThemeKey ? allThemes[currentThemeKey] : null;
	
	return <>
		<PanelledEditor 
			toolbar={false}
			showRightPanel={true}
			showLeftPanel={true}
			leftPanelTitle={`Themes`}
			leftPanel={() => {
				
				var comps = [];
				
				for(var key in allThemes){
					var theme = allThemes[key];
					((theme) => {
						
						var isCurrent = (theme == currentTheme);
						
						comps.push(<p>
							<ThemePreview selected={isCurrent} theme={theme} previewButton={1} onClick={e => {
								
								e.preventDefault();
								
								// Apply this theme to the node.
								setCurrentTheme(props.node, theme.key);
								
								// Trigger an onChange:
								props.onChange && props.onChange();
								
							}} />
						</p>);
						
					})(theme);
					
				}
				
				return comps;
			}}
			
			rightPanel={() => {
				
				return `Theme opts`;
				
			}}
		>
			<div onClick={e => {
				
				var target = e.target;
				
				// Get all properties on this object which use variables (not necessarily originating from themes):
				var properties = collectPropertiesUsingVariables(target);
				
				// Get all active themes on the element:
				var activeThemes = collectThemes(target, allThemes);
				
				// Next for each property, establish which theme the value is actually coming from. 
				// The activeThemes set is in order of priority, with the first one in the array being the most important.
				// I.e. The first encountered theme which specifies a variable value wins.
				for(var property in properties){
					var pv = properties[property];
					var activeTheme = findVariableInThemes(pv.value, activeThemes);
					pv.theme = activeTheme;
				}
				
				// Now have a set of properties on the clicked object with each one specifying which theme the value comes from.
				
				console.log(properties);
				// console.log(window.getComputedStyle(target));
				
			}}>
			{props.node ? renderNode(props.node) : <TestComponent />}
			</div>
		</PanelledEditor>
		{props.name && <input ref={ir=>{
				if(ir){
					ir.onGetValue=(val, ele)=>{
						
					};
				}
			}} name={props.name} type='hidden' />}
	</>;
	
}