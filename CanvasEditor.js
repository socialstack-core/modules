import tinymce from 'UI/TinyMce';
import omit from 'UI/Functions/Omit';
import Text from 'UI/Text';
import expand from 'UI/Functions/CanvasExpand';


export default class CanvasEditor extends React.Component {
	
	constructor(props){
		super(props);
		this.reset();
		this.state = {
			content: this.loadJson(props)
		};
	}
	
	componentWillReceiveProps(props){
		this.reset();
		this.setState({content: this.loadJson(props)});
	}
	
	reset(){
		this.element = null;
		if(this.editor != null){
			tinymce.remove(this.editor);
			this.editor = null;
		}
	}
	
	loadJson(props){
		var json = props.value || props.defaultValue;
		if(typeof json === 'string'){
			json = JSON.parse(json);
		}
		
		return expand(json, null, null, true);
	}
	
	componentWillUnmount(){
		this.reset();
	}
	
	mountEditor(e) {
		this.element = e;
		setTimeout(function(){
			
			tinymce.init({target:e, setup: editor => {
				
				this.editor = editor;
				editor.on('change', function () {
					tinymce.triggerSave();
				});
				
			}});
			
		}, 50);
	}
	
	renderNode(contentNode, index) {
		var Module = contentNode.module || "div";
		console.log('Render node', contentNode);
		
		if(Module == Text){
			return (
				<textarea 
					ref={e=>this.mountEditor(e)}
					onChange={this.onChange} 
					className={this.props.className || "form-control"}
					value={contentNode.content}
					{...omit(this.props, ['className', 'onChange', 'type'])}
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
		
		console.log('No support');
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
		
		return this.renderNode(content,0);
	}
}