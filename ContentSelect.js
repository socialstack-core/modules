import webRequest from 'UI/Functions/WebRequest';
import Input from 'UI/Input';

/**
 * Dropdown to select a piece of content.
 */
export default class ContentSelect extends React.Component {
	
	constructor(props){
		super(props);
		this.state = {};
		this.load(props);
	}
	
	componentWillReceiveProps(props){
		this.load(props);
	}
	
	load(props){
		webRequest(props.contentType.toLowerCase() + '/list').then(response => {
			var all = response.json.results;
			all.unshift(null);
			this.setState({all});
		});
	}
	
	render(){
		var all = this.state.all;
		
		return (<Input {...this.props} type="select">{
			all ? all.map(content => {
				if(!content){
					return <option value={'0'}>None</option>;
				}
				
				var title = content.title || content.firstName || content.username || content.name || content.url;
				
				return <option value={content.id}>{
					'#' + content.id + (title ? ' - ' + title : '(no identified title)')
				}</option>;
			}) : [
			]
		}</Input>);
		
	}
	
}