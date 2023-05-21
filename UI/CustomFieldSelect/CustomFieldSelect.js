import webRequest from 'UI/Functions/WebRequest';
import Input from 'UI/Input';
import omit from 'UI/Functions/Omit';

export default class CustomFieldSelect extends React.Component {
	
	constructor(props) {
		super(props);
	}

	componentDidMount() {
		webRequest("customContentTypeSelectOption/list", {
			where: {
				customContentTypeFieldId: this.props.field
			}
		}).then(response => {
			if (response && response.json && response.json.results) {
				var results = response.json.results;

				// sort results
				results = results.sort((a, b) => {

					if (a && b) {
						return a.value.trim().localeCompare(b.value.trim());
					}

					return -1;
				});

				results.unshift({ value: null });
				this.setState({ options: results });
			}
		}).catch(e => {
			console.error(e);
		});
	}

	render(){
		var defaultValue = this.props.defaultValue;

		return <div className="custom-field-select">
			<Input {...omit(this.props, ['contentType'])} type="select">{
				this.state.options ? this.state.options.map(option => {
					if(!option || !option.value){
						return <option value={null}>{this.props.placeHolder ? this.props.placeHolder : "None"}</option>;
					}
					
					return <option value={option.value}>{
						option.value
					}</option>;
				}) : defaultValue
				? [<option value={defaultValue}>{
						defaultValue
					}</option>] 
				: []
			}</Input>
		</div>;
		
	}
	
}