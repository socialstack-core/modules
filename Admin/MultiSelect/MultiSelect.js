import Search from 'UI/Search';
import webRequest from 'UI/Functions/WebRequest';
import Modal from 'UI/Modal';

var AutoForm = null;

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
			mustLoad,
			showCreateOrEditModal: false
		};

		if (!AutoForm) {
			AutoForm = require("Admin/AutoForm").default;
		}
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

		var atMax = false;
		
		if(this.props.max > 0){
			atMax = (this.state.value.length >= this.props.max);
		}

		let excludeIds = this.state.value.map(a => a.id);

		return (
			<div className="mb-3">
				{this.props.label && !this.props.hideLabel && (
					<label className="form-label">{this.props.label}</label>
				)}
				<div className="admin-multiselect">
					{
						this.state.value.map((entry, i) => (
							<div className="entry-wrapper">
								<div key={entry.id} className="entry">
									<div>
										{entry[displayFieldName]} <i className="remove-icon fa fa-times-circle" />
									</div>
										{<br />}
									<div>
										<button
											className="btn btn-secondary btn-entry-select-action btn-view-entry"
											onClick={e => {
												e.preventDefault();
												this.setState({ showCreateOrEditModal: true, entityToEditId: entry.id });
											}}
										>
											{<i className="fa fa-edit"></i>}
										</button>
									</div>
									<div>
										<button className="btn btn-secondary btn-entry-select-action btn-remove-entry"
											onClick={() => this.remove(entry)}
										>
											{<i className="fa fa-trash"></i>}
										</button>
									</div>
								</div>
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
						{atMax ? <p>
							<i>Max of {this.props.max} added</i>
						</p> : 
						<Search host={this.props.host} exclude={excludeIds} requestOpts={this.props.requestOpts} for={contentTypeLower} field={fieldName} limit={5} placeholder={"Find " + this.props.label + " to add.."} onFind={entry => {
							if(!entry || this.state.value.some(entity => entity.id === entry.id)){
								return;
							}

							var value = this.state.value;
							value.push(entry);
							this.setState({
								value
							});
							this.props.onChange && this.props.onChange({target: {value: value.map(e => e.id)}, fullValue: value});
							}}/>}
					</div>
			</div>
			<div>
				<button className="btn btn-secondary btn-sm btn-entry-select-action btn-new-entry"
					onClick={e => {
						e.preventDefault();
						this.setState({
							showCreateOrEditModal: true,
							entityToEditId: this.state.selected
						});
					}}
				>
						{`New ${this.props.label}...`}
					</button>
			</div>

				{this.state.showCreateOrEditModal &&
				<Modal
					title={"Edit " + this.props.label}
					onClose={() => {
						this.setState({ showCreateOrEditModal: false, entityToEditId: null })
					}}
					visible
					isExtraLarge
				>
					<AutoForm
						endpoint={this.props.contentType.toLowerCase()}
						singular={this.props.label}
						plural={this.props.label}
						id={this.state.entityToEditId ? this.state.entityToEditId : null}
						onActionComplete={entity => {						
							var value = this.state.value; 
							var valueIndex = value.findIndex(
								(checkIndex) => checkIndex.id === entity.id
							)
							
							if (valueIndex !== -1) {
								value[valueIndex] = entity
							} else {
								value.push(entity)
							}

							this.setState({
								showCreateOrEditModal: false,
								entityToEditId: null,
								value: value
							});
						}
						} />
				</Modal>
				}
			</div>);
	}
}
