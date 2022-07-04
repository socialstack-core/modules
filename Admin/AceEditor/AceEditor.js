import omit from 'UI/Functions/Omit';
import getRef from 'UI/Functions/GetRef';
import aceJs from './static/ace.js';

var inputTypes = global.inputTypes = global.inputTypes || {};

inputTypes['application/json'] = inputTypes['text/html'] = inputTypes['text/javascript'] = inputTypes['text/css']  = inputTypes['application/sql']  = function(props, _this){
	
	return <AceEditor 
		id={props.id || _this.fieldId}
		className={props.className || "form-control"}
		{...omit(props, ['id', 'className', 'type', 'inline'])}
	/>;
	
};

var aceLoading;

function loadAce(){
	if(aceLoading){
		return aceLoading;
	}
	
	return aceLoading = new Promise((s, r) => {
		// Ace is lazy loaded. Go get it now:
		var script = document.createElement("script");
		script.src = getRef(aceJs, {url: true});
		script.onload = () => {
			s(global.ace);
		};
		document.head.appendChild(script);
	});
}

export default class AceEditor extends React.Component{
	
	constructor(props){
		super(props);
		this.state = {
		};
		
		loadAce().then(ace => {
			var p = this.props;
			
			console.log(p.contentType);
			
			var type = 'json';
			
			if(p.contentType){
				var cType = p.contentType.split('/');
				type = cType[cType.length - 1];
				if(type == 'canvas'){
					type = 'json';
				}
			}
			
			// this.d
			var editor = ace.edit(this.d);
			this.setState({editor});
			global.editor = editor;
			// editor.setTheme("ace/theme/monokai"); (dark mode)
			editor.session.setMode("ace/mode/" + type);
			editor.session.setValue(p.defaultValue || p.value || '');
			
		})
	}
	
	render(){
		return <div className="aceeditor">
			<div ref={d => this.d = d} />
			<input type="hidden" name={this.props.name} ref={ref => {
				this.ref = ref;
				this.props.inputRef && this.props.inputRef(ref);
				if (ref) {
					ref.onGetValue = (val, field) => {
						if (field != this.ref) {
							return;
						}
						return this.state.editor.getValue();
					}
				}
			}} />
		</div>;
	}
	
}