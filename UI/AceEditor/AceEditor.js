import omit from 'UI/Functions/Omit';
import getRef from 'UI/Functions/GetRef';
import aceJs from './static/ace.js';

var inputTypes = global.inputTypes = global.inputTypes || {};

inputTypes.ontypecode = inputTypes['application/json'] = inputTypes['text/html'] = inputTypes['text/javascript'] = inputTypes['text/css']  = inputTypes['application/sql']  = function(props, _this){
	
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
			
			var editor = ace.edit(this.d);
			this.setState({editor});
			global.editor = editor;
			
			if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
				// dark mode
				editor.setTheme("ace/theme/monokai");
			}
			
			this.goToType(p, editor);
			editor.session.setValue(p.defaultValue || p.value || '');
			
			if(p.readonly){
				editor.session.setUseWorker(false);
				editor.setShowPrintMargin(false);
				editor.setReadOnly(true);
			}
		})
	}
	
	goToType(p, editor){
		var type = 'json';
		
		if(p.contentType){
			var cType = p.contentType.split('/');
			type = cType[cType.length - 1];
			if(type == 'canvas'){
				type = 'json';
			}
		}
		
		editor.session.setMode("ace/mode/" + type);
	}
	
	componentWillReceiveProps(props){
		var editor = this.state.editor;
		
		if(props.contentType != this.props.contentType){
			this.goToType(props, editor);
		}
		
		if(props.readonly && props.value != this.props.value){
			editor.session.setValue(props.defaultValue || props.value || '');
		}
	}
	
	render(){
		return <div className="aceeditor">
			<div ref={d => this.d = d} />
			{!this.props.readonly &&
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
			}} />}
		</div>;
	}
	
}