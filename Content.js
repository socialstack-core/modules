import webRequest from 'UI/Functions/WebRequest';

/*
* A convenience mechanism for obtaining 1 piece of content. Outputs no DOM structure.
* Very similar to <Loop> with a where:{Id: x}.
*/
export default class Content extends React.Component {
	
	constructor(props){
		super(props);
		this.state={
			loading: true
		};
	}
	
	componentWillReceiveProps(props){
		this.load(props);
	}
	
	componentDidMount(){
		this.load(this.props);
		// Future: add support for live, like Loop.
	}
	
	load(props){
		var url = props.type + '/' + props.id;
		if(url == this.state.url){
			return;
		}
		
		this.setState({url, content: null, loading: true});
		webRequest(url).then(response => {
			this.setState({content: response.json, loading: false});
		}).catch(e => {
			// E.g. doesn't exist.
			this.setState({content: null, loading: false});
		});
	}
	
	render(){
		
		return <div className="content">
			{this.props.children && this.props.children(this.state.content, this.state.loading)}
		</div>;
		
	}
	
}