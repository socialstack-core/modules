// Module import examples - none are required:
// import webRequest from 'UI/Functions/WebRequest';
// import Loop from 'UI/Loop';
import webSocket from 'UI/Functions/WebSocket';

export default class CallUI extends React.Component{
    
	componentDidMount(){
        this.ringInterval = setInterval(() => {
            webSocket.send({type: 'ring', users: this.props.users.map(user => user.id)});
        }, 1000);
    }

    cancel(){
        this.clear();
        this.props.onClose && this.props.onClose();
    }
    
	clear(){
    	clearInterval(this.ringInterval);
    }
    
	componentWillUnmount(){
        this.clear();
    }

    buildNames(users){
        return users.map(user=>user.fullName).join(', ');
    }
	
    render(){
         var names = this.buildNames(this.props.users);
         return <div className = "call-ui">
			 <div className = "call-ui-center">
				<div>{`Calling`}</div>
				<div>{`${names}...`}</div>
				<button className="btn btn-danger" onClick={() => this.cancel()}>Hangup</button>
			 </div>
         </div>;
    }
}

/*
// propTypes are used to describe configuration on your component in the editor.
// Just setting it to an empty object will make your component appear as something that can be added.
// Define your available props like the examples below.

CallUI.propTypes = {
	title: 'string', // text input
	size: [1,2,3,4], // dropdowns
	width: [ // dropdown with separate labels/values
		{ name: '1/12', value: 1 },
		{ name: '2/12', value: 2 },
		{ name: '3/12 (25%)', value: 3 },
		{ name: '4/12 (33%)', value: 4 },
		{ name: '5/12', value: 5 },
		{ name: '6/12 (50%)', value: 6 },
		{ name: '7/12', value: 7 },
		{ name: '8/12 (66%)', value: 8 },
		{ name: '9/12 (75%)', value: 9 },
		{ name: '10/12', value: 10 },
		{ name: '11/12', value: 11 },
		{ name: '12/12 (100%)', value: 12 }
	],
	
	// All <Input type='x' /> values are supported - checkbox, color etc.
	// Also the special id type which can be used to select some other piece of content (by callUI name), like this:
	templateToUse: {type: 'id', content: 'Template'}
};

// use defaultProps to define default values, if required
CallUI.defaultProps = {
	title: "Example string",
	size: 1,
	width: 6	
}

// icon used to represent component when adding to a page via /en-admin/page
// see https://fontawesome.com/icons?d=gallery for available classnames
// NB: exclude the "fa-" prefix
CallUI.icon='align-center'; // fontawesome icon
*/