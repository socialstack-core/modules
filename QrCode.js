import Qr from 'UI/Functions/Qr';

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
		var div = this.divRef.current;

		while(div.firstChild){
			div.removeChild(div.firstChild);
		}

		var qrcode = new Qr(this.divRef.current, {
			text: this.props.text,
			width: this.props.width || 128,
			height: this.props.height || 128,
			colorDark : "#000000",
			colorLight : "#ffffff",
			correctLevel : Qr.CorrectLevel.H
		});
	}
	
	render(){	
		return (<div ref={this.divRef}></div>);
	}
	
}