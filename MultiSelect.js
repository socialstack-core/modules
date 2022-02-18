import Search from 'UI/Search';
import webRequest from 'UI/Functions/WebRequest';

/**
 * A general use "multi-selection"; primarily used for tags and categories.
 */

export default class MultiSelect extends React.Component {
	
    constructor(props) {
        super(props);
		
		var mustLoad = false;
		var initVal = (props.value || props.defaultValue || []).filter(t => t!=null);
		
		if(initVal.length && typeof initVal[0] == 'number'){
			initVal = initVal.map(id => {return {id}});
			mustLoad = true;
		}
		
		this.state = {
			value: initVal,
			mustLoad
		};
		
    }
	
	componentDidMount(){
		if(this.state.mustLoad){
			webRequest(this.props.contentType + '/list', {where: {Id: this.state.value.map(e => e.id)}}).then(response => {
				
				// Loading the values and preserving order:
				var idLookup = {};
				response.json.results.forEach(r => {idLookup[r.id+''] = r;});
				
				this.setState({
					mustLoad: false,
					value: this.state.value.map(e => idLookup[e.id+'']).filter(t=>t!=null)
				});
				
			});
		}
	}
	
	componentWillReceiveProps(props){
		if(props.value){
			this.setState({
				value: props.value.filter(t => t!=null)
			});
		}
	}
	
	remove(entry) {
		var value = this.state.value.filter(t => t!=entry && t!=null);
        this.setState({
			value
		});
		this.props.onChange && this.props.onChange({target: {value: value.map(e => e.id)}, fullValue: value});
    }
	
	render() {
		var fieldName = this.props.field || 'name';
		var displayFieldName = this.props.displayField || fieldName;
		if(displayFieldName.length){
			displayFieldName = displayFieldName[0].toLowerCase() + displayFieldName.substring(1);
		}

		var contentTypeLower = this.props.contentType ? this.props.contentType.toLowerCase() : "";

		return (
			<div className="mb-3">
				{this.props.label && !this.props.hideLabel && (
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
						<Search host={this.props.host} requestOpts={this.props.requestOpts} for={contentTypeLower} field={fieldName} limit={5} placeholder={"Find " + this.props.label + " to add.."} onFind={entry => {
							if(!entry){
								return;
							}
							var value = this.state.value;
							value.push(entry);
							this.setState({
								value
							});
							this.props.onChange && this.props.onChange({target: {value: value.map(e => e.id)}, fullValue: value});
						}}/>
					</div>
				</div>
			</div>
		);
	}
}
