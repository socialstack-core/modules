import {getUrl} from 'UI/FileRef';
import aceJs from './static/ace.js';

inputTypes.json = function (props) {
	const { field } = props;
	return <AceEditor 
		type="json"
		{...field}
	/>;
};

inputTypes.sql = function (props) {
	const { field } = props;
	return <AceEditor 
		type="sql"
		{...field}
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
		script.src = getUrl(aceJs);
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
			
			var editor = ace.edit(this.d, {
				behavioursEnabled: true,
				wrapBehavioursEnabled: true,
				wrap: true,
				cursorStyle: "smooth",
            });
			this.setState({editor});
			global.editor = editor;

			var html = document.querySelector("html");

			if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ||
				(html && html.getAttribute("data-theme-variant") == "dark")) {
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
		var type = p.type || 'json';
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