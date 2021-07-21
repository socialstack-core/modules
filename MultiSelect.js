import Search from 'UI/Search';

/**
 * A general use "multi-selection"; primarily used for tags and categories.
 */

export default class MultiSelect extends React.Component {
	
    constructor(props) {
        super(props);
		
		
		this.state = {
			value: (props.value || props.defaultValue || []).filter(t => t!=null)
		};
		
    }
	
	componentWillReceiveProps(props){
		if(props.value){
			this.setState({
				value: props.value.filter(t => t!=null)
			});
		}
	}
	
	remove(entry) {
        this.setState({
			value: this.state.value.filter(t => t!=entry && t!=null)
		});
    }
	
	render() {
		var fieldName = this.props.field || 'name';
		var displayFieldName = this.props.displayField || fieldName;
		if(displayFieldName.length){
			displayFieldName = displayFieldName[0].toLowerCase() + displayFieldName.substring(1);
		}
		
		return (
			<div className="mb-3">
				{this.props.label && (
					<label className="form-label">{this.props.label}</label>
				)}
				<div className="admin-multiselect">
					{this.state.value.map((entry, i) => (
						<div key={entry.id} className="entry" onClick={() => this.remove(entry)}>
							{entry[displayFieldName]} <i className="remove-icon fa fa-times-circle" />
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
						<Search for={this.props.contentType.toLowerCase()} field={fieldName} limit={5} placeholder={"Find " + this.props.label + " to add.."} onFind={entry => {
							if(!entry){
								return;
							}
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
