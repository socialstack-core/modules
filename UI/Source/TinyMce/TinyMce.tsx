import {lazyLoad} from 'UI/Functions/WebRequest';
import tinyMceRef from './static/TinyMce.js';
import {getUrl} from 'UI/FileRef';
import {PublicError} from "UI/Failed";

inputTypes.html = function (props) {
	const { field } = props;
	return <TinyMce
		{...field}
	/>;
}

// Used by typescript props via HtmlString
inputTypes.htmlstring = function (props) {
	const { field } = props;
	return <TinyMce
		{...field}
	/>;
}

export default class TinyMce extends React.Component {
	
	constructor(props){
		super(props);
	}
	
	componentDidMount(){
		var tinyMceUrl = getUrl(tinyMceRef);
		
		var doc = this._textarea.ownerDocument;
		var win = doc.defaultView || doc.parentWindow;

		// fix for cordova
		if (tinyMceUrl[0] != '/') {
			tinyMceUrl = './' + tinyMceUrl;
		}

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
		
		var config = 
		{
			target,
			statusbar: false,
			height: this.props.height,
			plugins: this.props.plugins,
			mentionsLookupUrl: this.props.mentionsLookupUrl,
			mentionsQuery:this.props.mentionsQuery,

			//plugins: 'lists advlist anchor autolink autosave code fullscreen image link quickbars searchreplace table',
			toolbar: this.props.toolbar,

			//// override mobile styling 
			mobile: {
				theme: 'silver',
				toolbar: this.props.mobileToolbar
			},

			content_css: ["/pack/static/tinymce/static/themes/socialstack.css"],
			
			//font_formats: 'Arial=arial,helvetica,sans-serif; Courier New=courier new,courier,monospace',
			quickbars_insert_toolbar: false,

			setup: (ed) => {
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
		};

		if (this.props.showMenubar) {
			/*
			config.menu: {
				file: { title: 'File', items: 'newdocument' }
				//edit: { title: 'Edit', items: 'undo redo | cut copy paste | selectall' },

				//file: { title: 'File', items: 'newdocument restoredraft | preview | print ' },
				//edit: { title: 'Edit', items: 'undo redo | cut copy paste | selectall | searchreplace' },
				//view: { title: 'View', items: 'code | visualaid visualchars visualblocks | spellchecker | preview fullscreen' },
				//insert: { title: 'Insert', items: 'image link media template codesample inserttable | charmap emoticons hr | pagebreak nonbreaking anchor toc | insertdatetime' },
				//format: { title: 'Format', items: 'bold italic underline strikethrough superscript subscript codeformat | formats blockformats fontformats fontsizes align lineheight | forecolor backcolor | removeformat' },
				//tools: { title: 'Tools', items: 'spellchecker spellcheckerlanguage | code wordcount' },
				//table: { title: 'Table', items: 'inserttable | cell row column | tableprops deletetable' },
				//help: { title: 'Help', items: 'help' }
			},
			*/
			config.menubar = this.props.menubar;
		} else {
			config.menubar = '';
		}

		if(this.props.allowCode) {
			config.plugins = config.plugins + " code ";
			config.toolbar = config.toolbar + " | code ";
		}

		if (this.props.allowImages) {
			config.plugins = config.plugins + " image ";
			config.toolbar = config.toolbar + " | image ";

			config.mobile.toolbar = config.mobile.toolbar + " | image ";
			
			config.image_description = false;
			config.image_dimensions = false;
		}

		if (this.props.allowImages && this.props.embedImages) {
			/* enable automatic uploads of images represented by blob or data URIs*/
			config.automatic_uploads = true;
			
			/*
			URL of our upload handler (for more details check: https://www.tiny.cloud/docs/configure/file-image-upload/#images_upload_url)
			images_upload_url: 'postAcceptor.php',
			here we add custom filepicker only to Image dialog
			*/
			config.file_picker_types = 'image';

			/* and here's our custom image picker*/
			config.file_picker_callback = function (cb, value, meta) {
			
				var input = document.createElement('input');
				input.setAttribute('type', 'file');
				input.setAttribute('accept', 'image/*');

				/*
					Note: In modern browsers input[type="file"] is functional without
					even adding it to the DOM, but that might not be the case in some older
					or quirky browsers like IE, so you might want to add it to the DOM
					just in case, and visually hide it. And do not forget do remove it
					once you do not need it anymore.
				*/

				input.onchange = function () {
				  var file = this.files[0];

				  var reader = new FileReader();
				  reader.onload = function () {
					/*
					  Note: Now we need to register the blob in TinyMCEs image blob
					  registry. In the next release this part hopefully won't be
					  necessary, as we are looking to handle it internally.
					*/
					var id = 'blobid' + (new Date()).getTime();
					var blobCache =  tinymce.activeEditor.editorUpload.blobCache;
					var base64 = reader.result.split(',')[1];
					var blobInfo = blobCache.create(id, file, base64);
					blobCache.add(blobInfo);

					/* call the callback and populate the Title field with the file name */
					cb(blobInfo.blobUri(), { title: file.name });
				  };
				  reader.readAsDataURL(file);
				};

				input.click();
			};
			
		}

		return tinymce.init(config);
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
						
						if (typeof this.props.required === 'boolean' && this.props.required && !htmlContent) {
							throw new PublicError("tiny-mce/validation", `This field is required.`);
						}
						if (typeof this.props.required === 'string') {
							var mtd = require("UI/Functions/Validation/" + valType).default;
							
							var error = mtd(htmlContent);
							
							if (error) {
								throw error;
							}							
						}
							
						return htmlContent;
					}
				};
			}
			 
		}}
		// reason for this:
		// the required attribute requires the textarea to be focusable, when "display: none" is in effect
		// it isn't focusable, "display:none" happens due to tinymce, so instead I've omitted 
		// the required attribute, and handled it the same way the common Input component handles it
		// @see https://stackoverflow.com/questions/22148080/an-invalid-form-control-with-name-is-not-focusable	
		{...omit(this.props, ['required'])}  />;
	}
}

const omit = (obj, fields) => {
	const out = {}
	
	Object.keys(obj).forEach(key => {
		if (fields.includes(key)) {
			return;
		}
		out[key] = obj[key];
	})
	return out;
}

TinyMce.propTypes = {
	showMenubar : "bool",
	allowCode: "bool",
    allowImages: "bool",
    embedImages: "bool",
	height: "int",
	menubar: "string",
	toolbar: "string",
	mobileToolbar: "string",
	plugins: "string"
};

TinyMce.defaultProps = {
	showMenubar: false,		
	allowCode: false,
    allowImages: false,
    embedImages: false,
	height: 500,
	menubar: 'edit insert view format table help',
	toolbar: 'bold italic underline | alignleft aligncenter alignright alignjustify | outdent indent | numlist bullist',
	mobileToolbar: 'bold italic underline',
	plugins: 'lists quickbars searchreplace table'
};