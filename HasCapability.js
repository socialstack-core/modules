import hasCapability from 'UI/Functions/HasCapability';

/*
Displays its content only if the named capability is actually granted.
*/
export default class HasCapability extends React.Component {
	
	constructor(props){
		super(props);
		
		this.state={
			name: props.called,
			granted: false
		};
	}
	
	componentDidMount(){
		if(props.called){
			hasCapability(props.called, this.context).then(granted => this.setState({granted}));
		}
	}
	
	componentWillReceiveProps(props){
		if(props.called == this.state.name){
			return;
		}
		
		this.setState({name: props.called});
		hasCapability(props.called).then(granted => this.setState({granted}));
	}
	
	render(){
		var g = this.state.granted;
		this.props.invert && (g=!g);
		return g ? this.props.children : null;
	}
	
}

HasCapability.propTypes = {
	called: 'string',
	invert: 'bool',
	children: true
};

HasCapability.icon = 'times-circle';