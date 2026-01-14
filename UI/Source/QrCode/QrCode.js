import {lazyLoad} from 'UI/Functions/WebRequest';
import {getUrl} from 'UI/FileRef';
import qrRef from './static/qr.js';

// Uses https://github.com/davidshimjs/qrcodejs (the imported module)
export default class QrCode extends React.Component{
	
	constructor(props){
		super(props);
        this.divRef = React.createRef();
	}
	componentDidMount(){
		this.redraw(this.props);
	}
	
	componentWillReceiveProps(props){
		this.redraw(props);
	}
	
	redraw(props){
		lazyLoad(getUrl(qrRef)).then(exp => {
			var div = this.divRef.current;

			while(div.firstChild){
				div.removeChild(div.firstChild);
			}

			var qrcode = new exp.QRCode(this.divRef.current, {
				text: this.props.text,
				width: this.props.width || 128,
				height: this.props.height || 128,
				colorDark : this.props.dark || "#000000",
				colorLight : this.props.light || "#ffffff",
				correctLevel : exp.QRCode.CorrectLevel.H
			});
		});
	}
	
	render(){	
		return (<div ref={this.divRef}></div>);
	}
	
}