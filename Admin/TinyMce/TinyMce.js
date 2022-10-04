import {lazyLoad} from 'UI/Functions/WebRequest';
import tinyMceRef from './static/TinyMce.js';
import getRef from 'UI/Functions/GetRef';
import omit from 'UI/Functions/Omit';

var inputTypes = global.inputTypes = global.inputTypes || {};

// type="html", contentType="text/html"
inputTypes['text/html'] = inputTypes.ontypehtml = function(props, _this){
	
	return <TinyMce
		id={props.id || _this.fieldId}
		className={props.className || "form-control"}
		{...omit(props, ['id', 'className', 'type', 'inline'])}
	/>;
	
};

export default class TinyMce extends React.Component {
	
	constructor(props){
		super(props);
	}
	
	componentDidMount(){
		var tinyMceUrl = getRef(tinyMceRef, {url:1});
		
		var doc = this._textarea.ownerDocument;
		var win = doc.defaultView || doc.parentWindow;
		
		lazyLoad(tinyMceUrl, win).then(imported => {
			var tinymce = win.tinymce;
			tinymce.baseURL = tinyMceUrl.replace(/\/tinymce\.js/gi, '');
			return this.initEditor(this._textarea, tinymce);
		}).then(editors => {
			this.setState({
				editor: editors[0],
				target: this._textarea
			})
		});
	}
	
	initEditor(target, tinymce){
		var _this = this;
		
		return tinymce.init({
			target,
			setup:(ed) => {
				var _timeout;
				
			   ed.on('Paste Change input Undo Redo', (e) => {
					if(this.props.onChange){
						clearTimeout(_timeout);
						_timeout = setTimeout(function() {
							var evt = {target: {
								value: _this.state.editor.getContent()
							}};
							_this.props.onChange(evt);
						}, 100);
					}
			   });
		   }
		});
	}
	
	render(){
		return <textarea ref={e => {
			this._textarea = e;
			
			if(this.state.editor && e && e != this.state.target){
				// Init again:
				var doc = e.ownerDocument;
				var win = doc.defaultView || doc.parentWindow;
				this.initEditor(e, win.tinymce).then(editors => {
					this.setState({
						editor: editors[0],
						target: e
					});
				});
			}
			
			if(e){
				e.onGetValue = (val, ele)=>{
					if(ele == e && this.state.editor){
						var htmlContent = this.state.editor.getContent();
						return htmlContent;
					}
				};
			}
			
		}} {...this.props} />;
	}
}