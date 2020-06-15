import Search from 'UI/Search';

/**
 * A general use "multi-selection"; primarily used for tags and categories.
 */

export default class MultiSelect extends React.Component {
	
	input = null;
	
    constructor(props) {
        super(props);
		
		
		this.state = {
			value: props.value || props.defaultValue || []
		};
		
    }
	
	componentWillReceiveProps(props){
		if(props.value){
			this.setState({
				value: props.value
			});
		}
	}
	
	remove(entry) {
        this.setState({
			value: this.state.value.filter(t => t!=entry)
		});
    }
	
	render() {
		var fieldName = this.props.field || 'name';
		console.log(this.props);
		return (
			<div className="form-group">
				{this.props.label && (
					<label>{this.props.label}</label>
				)}
				<div className="multiselect">
					{this.state.value.map((entry, i) => (
						<div key={entry.id} className="entry" onClick={() => this.remove(entry)}>
							{entry[fieldName]} <i className="remove-icon fa fa-times-circle" />
						</div>
					))}
					<div>
						<input type="hidden" ref={
							ele => {
								this.input = ele;
								if(ele != null){
									ele.onGetValue=(v, input, e) => {
										if(input != this.input){
											return v;
										}
										
										return this.state.value.map(entry => entry.id);
									}
								}
							}
						}
						name={this.props.name} />
						<Search for={this.props.contentType} field={fieldName} limit={5} placeholder={"Find " + this.props.label + " to add.."} onFind={entry => {
							var value = this.state.value;
							value.push(entry);
							this.setState({
								value
							});
						}}/>
					</div>
				</div>
			</div>
		);
	}
}
