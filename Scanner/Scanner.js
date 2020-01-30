import Qr from 'UI/Functions/Qr';
import emailAddress from 'UI/Functions/EmailAddress';

// Uses https://github.com/davidshimjs/qrcodejs (the imported module)
export default class Scanner extends React.Component{
	
	constructor(props){
		super(props);
        this.divRef = React.createRef();
	}
	componentDidMount(){
		
		global.document.body.className+=" qr-scanner";
		
        if(global.QRScanner){
            var QRScanner = global.QRScanner;
            QRScanner.prepare(function(err, status){
                if(err){
                    return;
                }
                
				// Calling destroy here dodges a height bug:
				QRScanner.destroy(function(status){
					QRScanner.scan(function(err, text){
						console.log('Scanned: ' + text);
						if(text.indexOf("/app/share/") != -1){
							var emailKey = text.split('/app/share/')[1];
							emailAddress.set(emailKey);
							global.pageRouter.go('/how');
						}
					});
				});
            })
        }
        else{
            console.log("global.QRScanner is undefined.");
        }
    }
    
    componentWillUnmount(){
		var classNames = global.document.body.className.split(' ');
		global.document.body.className = classNames.filter(name => name != 'qr-scanner').join(' ');
		global.document.body.style.backgroundColor = '';
		global.document.body.parentNode.style.backgroundColor = '';
        global.QRScanner && global.QRScanner.destroy(function(status){
            console.log(status);
			global.document.body.style.backgroundColor = '';
			global.document.body.parentNode.style.backgroundColor = '';
			setTimeout(function(){
				global.document.body.style.backgroundColor = '';
				global.document.body.parentNode.style.backgroundColor = '';
			}, 500);
        });
    }
	
	componentWillReceiveProps(props){
		//this.redraw(props);
	}
	
	render(){	
		return (<div ref={this.divRef}></div>);
	}
	
}