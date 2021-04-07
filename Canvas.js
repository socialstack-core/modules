import {expand, mapTokens} from 'UI/Functions/CanvasExpand';
import { RouterConsumer } from 'UI/Session';

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
	
	renderNode(contentNode, index, pageRouter) {
		if(!contentNode){
			return null;
		}
		
		if(!contentNode.module && !contentNode.content){
			// E.g. strings, numbers.
			return contentNode;
		}
		
		// note that some elements are just arrays of content, which will be wrapped in a div.
		var Module = contentNode.module || "div";
		
		// Resolve runtime field values now:
		var dataFields = mapTokens(contentNode.data, this, pageRouter);
		
		return (
			<Module key={index} {...dataFields}>
			{
				contentNode.useCanvasRender && contentNode.content ? contentNode.content.map((e,i)=>this.renderNode(e,i, pageRouter)) : contentNode.content
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
		
		return <RouterConsumer>{
			pageRouter => Array.isArray(content) ? 
				content.map((e,i)=>this.renderNode(e,i, pageRouter)) : 
				this.renderNode(content, 0, pageRouter)
		}</RouterConsumer>;
	}
}

export default Canvas;
