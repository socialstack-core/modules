import {expand, mapTokens} from 'UI/Functions/CanvasExpand';

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
			content = expand(content, this.props.onContentNode);
		}
		
		return content;
	}
	
	forceRender() {
		this.setState({content: this.state.content});
	}
	
	onChange(){
		this.forceRender();
		this.props.onCanvasChanged && this.props.onCanvasChanged();
	}
	
	renderNode(contentNode, index) {
		if(!contentNode){
			return null;
		}
		var Module = contentNode.module || "div";
		
		// Resolve runtime field values now:
		var dataFields = mapTokens(contentNode.data, this, Canvas);
		
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
