import webRequest from 'UI/Functions/WebRequest';
import omit from 'UI/Functions/Omit';
import Input from 'UI/Input';
import Search from 'UI/Search';

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
		if(props.search){
			var value = props.value || props.defaultValue;
			if(value){
				webRequest(props.contentType.toLowerCase() + '/' + value).then(response => {
					this.setState({
						selected: response.json
					});
				});
			}else{
				this.setState({
					selected: null
				});
			}
		}else{
			webRequest(props.contentType.toLowerCase() + '/list').then(response => {
				var all = response.json.results;
				all.unshift(null);
				this.setState({all});
			});
		}
	}
	
	render(){
		if(this.props.search){
			
			var {selected} = this.state;
			var title = '';
			
			if(selected){
				title = selected.title || selected.firstName || selected.username || selected.name || selected.url;
			}
			
			var value = this.props.defaultValue || this.props.value;
			
			return <div className="form-group content-select">
				{this.props.label && (
					<label>{this.props.label}</label>
				)}
				<input type="hidden" ref={
					ele => {
						this.input = ele;
						if(ele != null){
							ele.onGetValue=(v, input, e) => {
								if(input != this.input){
									return v;
								}
								
								return this.state.selected ? this.state.selected.id : '0';
							}
						}
					}
				}
				name={this.props.name} />
				<Search for={this.props.contentType} field={this.props.search} limit={5} placeholder={"Search for a " + this.props.contentType + ".."} onFind={entry => {
					this.setState({
						selected: entry
					});
				}}/>
				{
					<div className="selected-content">
						{selected ? title : (
							value ? 'Item #' + value : 'None selected'
						)}
					</div>
				}
			</div>;
			
		}
		
		var all = this.state.all;
		
		return (<Input {...omit(this.props, ['contentType'])} type="select">{
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