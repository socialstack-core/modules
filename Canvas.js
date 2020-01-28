import Text from 'UI/Text'; 
import webRequest from 'UI/Functions/WebRequest';

/**
 * This component renders canvas JSON. It takes canvas JSON as its child
 */
class Canvas extends React.Component {
	
	constructor(props){
		super(props);

		this.state = {
		};
		
		this.loadJson(props);
	}
	
	componentWillReceiveProps(props){
		this.loadJson(props);
	}
	
	loadJson(props, set){
		var content;
		var dataSource = props.bodyJson || props.children[0];
		
		if(typeof dataSource == 'string'){
			try{
				content = JSON.parse(dataSource);
				}catch(e){
				console.log("Canvas failed to load JSON: ", dataSource);
				console.error(e);
				}
		}else{
			content = dataSource;
		}
		
		if(content){
			content = this.expand(content);
		}
		
		this.setState({content});
	}
	
	expand(contentNode){
		// We recurse the tree via each 'content' array, expanding each node as we go.
		// The goal is to eliminate all convenience/ shorthand JSON such that e.g:
		// {"canvas": 1}  -> {"module": "Canvas", data: {"url": "canvas/1"}}
		// We'll also send off the requests for any of those url's to load.
		
		if(Array.isArray(contentNode)){
			return contentNode.map(e => this.expand(e));
		}
		
		if(typeof contentNode != 'object'){
			// String or number
			return {
				module: Text,
				content: contentNode
			};
		}
		
		// - Add a filter function here, so other modules can also expand canvas content too.
		// contentNode = _some_global_filter_technique_(contentNode);
		
		// Canvas filter:
		if(contentNode.canvas){
			contentNode.module = "UI/Canvas";
			contentNode.data = {url: "canvas/" + contentNode.canvas};
		}
		
		// data filter:
		if(contentNode.data && typeof contentNode.data != 'object'){
			// Mapped to one prop just called data:
			contentNode.data = {
				data: contentNode.data
			};
		}
		
		// URL load filter (order matters - this last):
		if(contentNode.data && contentNode.data.url){
			// Expand the data object.
			
			var url = contentNode.data.url;
			
			webRequest(url).then(obj => {
				contentNode.data = obj.json;
				this.forceRender();
			});
		}
		
		if(contentNode.content){
			contentNode.content = this.expand(contentNode.content);
		}else{
			contentNode.content = [];
		}
		
		// Module filter - connect the right module to use.
		if(contentNode.module){
			
			var mdRef = contentNode.module;
			
			if(mdRef.indexOf('.') == -1){
				// Expansion of e.g. UI/Canvas to UI/Canvas/Canvas.js
				var mdRefParts = mdRef.split('/');
				mdRef = mdRef + '/' + mdRefParts[mdRefParts.length-1] + '.js';
			}
			
			var module = require(mdRef);
			
			if(module){
				// Use the mapped module. It could be imported JSON, 
				// in which case we've got a regular JS object which should be passed to a canvas.
				
				if(module.default){
					contentNode.module = module.default;
				}else{
					contentNode.module = Canvas;
					contentNode.data = {
						data: module
					};
				}
				
				contentNode.useCanvasRender = true;
				
			}else{
				var first = contentNode.module.charAt(0);
				if(first == first.toUpperCase()){
					// Module is missing - prompt dev to install it.
					throw new Error("Attempted to use a UI module called '" + contentNode.module + "' which you don't have installed.");
				}
				
				// It's a HTML element being used directly. Must use Canvas render for the content of these.
				contentNode.useCanvasRender = contentNode.content != null;
				
				if(!Array.isArray(contentNode.content)){
					// Must be an array here.
					contentNode.content = [contentNode.content];
				}
				
				module = contentNode.module;
			}
			
		}else{
			// Use this canvas to render the content unless there is also data.
			if(contentNode.data){
				contentNode.module = Canvas;
			}else if(Array.isArray(contentNode.content)){
				contentNode.useCanvasRender = true;
			}else{
				contentNode = contentNode.content;
			}
		}
		
		this.props.onContentNode && this.props.onContentNode(contentNode);
		
		return contentNode;
	}
	
	forceRender() {
		this.setState({content: this.state.content});
	}
	
	mapTokens(obj){
		
		for(var e in obj) {
			
			var value = obj[e];
			
			if(!value){
				continue;
			}
			
			if(value.type && value.name){
			
				if(value.type == "urlToken"){
					obj[e] = global.pageRouter.state.tokens[value.name];
				}
				
			}else if(typeof value === 'object'){
				// Go deeper:
				this.mapTokens(value);
			}
		}
		
	}
	
	renderNode(contentNode, index) {
		var Module = contentNode.module || "div";
		
		var dataFields = {...contentNode.data};
		
		// Resolve runtime field values now:
		this.mapTokens(dataFields);
		
		return (
			<Module key={index} {...dataFields}>
			{
				contentNode.useCanvasRender ? contentNode.content.map((e,i)=>this.renderNode(e,i)) : contentNode.content
			}
			</Module>
		);
	}
	
	render() {
		
		var content = this.state.content;
		var substitutions = null;
		
		if(this.props.data){
			// Rendering some other canvas by the given ID (or loaded JSON).
			if(typeof this.props.data == 'object'){
				substitutions = content;
				content = this.props.data;
			}else{
				// It's an ID. Must wait for it to load in full.
				return null;
			}
		}
		
		// Otherwise, render the (preprocessed) child nodes.
		if(!content){
			return null;
		}
		
		if(Array.isArray(content)){
			return content.map((e,i)=>this.renderNode(e,i));
		}
		
		return this.renderNode(content,0);
	}
}

export default Canvas;
