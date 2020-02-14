import omit from 'UI/Functions/Omit';
import Text from 'UI/Text';
import expand from 'UI/Functions/CanvasExpand';


export default class CanvasEditor extends React.Component {
	
	constructor(props){
		super(props);
		this.state = {
			content: this.loadJson(props)
		};
		this.buildJson = this.buildJson.bind(this);
	}
	
	componentWillReceiveProps(props){
		this.setState({content: this.loadJson(props)});
	}
	
	loadJson(props){
		var json = props.value || props.defaultValue;
		if(typeof json === 'string'){
			json = JSON.parse(json);
		}
		
		if(!json || (!json.module && !json.content)){
			json = {module: 'UI/Text', content: ''};
		}
		
		return expand(json, null, null, true);
	}
	
	buildJson(){
		return this.buildJsonNode(this.state.content, 0, true);
	}
	
	buildJsonNode(contentNode, index, isRoot){
		
		if(Array.isArray(contentNode)){
			return contentNode.map(this.buildJsonNode);
		}
		
		if(contentNode.module == Text){
			return isRoot ? {
				content: contentNode.content
			} : contentNode.content;
		}
		
		// Otherwise, get the module name:
		if(!contentNode.moduleName){
			var moduleName = null;
			
			for(var name in global.__mm){
				if(global.__mm[name] == contentNode.module){
					moduleName = name;
					break;
				}
			}
			
			contentNode.moduleName = moduleName;
		}
		
		return {
			module: contentNode.moduleName,
			data: contentNode.data,
			content: this.buildJsonNode(contentNode.content,0)
		};
	}
	
	renderNode(contentNode, index) {
		var Module = contentNode.module || "div";
		
		if(Module == Text){
			
			var divRef = null;
			
			return (
				<div
					contenteditable
					onInput={e => {
						contentNode.content = e.target.innerHTML;
						this.ref.value=JSON.stringify(this.buildJson());
					}}
					className={"canvas-editor-text " + (this.props.className || "form-control")}
					dangerouslySetInnerHTML={{__html: contentNode.content}}
				/>
			);
		}
		
		var dataFields = {...contentNode.data};
		
		if(contentNode.useCanvasRender){
			return (
				<div>
				{
					contentNode.content.map((e,i)=>this.renderNode(e,i))
				}
				</div>
			);
		}
		
		console.log('No support!');
		return '-Module not supported yet-';
	}
	
	render(){
		
		var content = this.state.content;
		
		// Otherwise, render the (preprocessed) child nodes.
		if(!content){
			return null;
		}
		
		if(Array.isArray(content)){
			return content.map((e,i)=>this.renderNode(e,i));
		}
		
		return (
			<div className="canvas-editor">
				<input type="hidden" name={this.props.name} ref={ref => {
					this.ref = ref;
					if(ref){
						ref.value=JSON.stringify(this.buildJson());
					}
				}}/>
				{this.renderNode(content,0)}
			</div>
		);
	}
}