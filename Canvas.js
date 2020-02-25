import expand from 'UI/Functions/CanvasExpand';

/**
 * This component renders canvas JSON. It takes canvas JSON as its child
 */
class Canvas extends React.Component {
	
	constructor(props){
		super(props);

		this.state = {
			content: this.loadJson(props)
		};
	}
	
	componentWillReceiveProps(props){
		this.setState({content: this.loadJson(props)});
	}
	
	loadJson(props, set){
		var content;
		var dataSource = props.bodyJson || props.children;
		
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
			content = expand(content, () => this.forceRender(), this.props.onContentNode);
		}
		
		return content;
	}
	
	forceRender() {
		this.setState({content: this.state.content});
	}
	
	mapTokens(obj){
		var tokenSet = this.props.tokens || global.pageRouter.state.tokens;
		
		for(var e in obj) {
			
			var value = obj[e];
			
			if(!value){
				continue;
			}
			
			if(value.type && value.name){
			
				if(value.type == "urlToken"){
					obj[e] = tokenSet[value.name];
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
				contentNode.useCanvasRender && contentNode.content ? contentNode.content.map((e,i)=>this.renderNode(e,i)) : contentNode.content
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
