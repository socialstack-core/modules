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

		return <>
			<div className="admin-multiselect mb-3">
				{this.props.label && !this.props.hideLabel && (
					<label className="form-label">
						{this.props.label}
					</label>
				)}
				<ul className="admin-multiselect__entries">
					{
						this.state.value.map((entry, i) => (
							<li key={entry.id} className="admin-multiselect__entry">
								<p>
									{entry[displayFieldName]}
								</p>
								<div className="admin-multiselect__entry-options">
									<button className="btn btn-sm btn-outline-primary btn-entry-select-action btn-view-entry" title={`Edit`}
										onClick={e => {
											e.preventDefault();
											this.setState({ showCreateOrEditModal: true, entityToEditId: entry.id });
										}}>
										<i className="fal fa-fw fa-edit"></i> <span className="sr-only">{`Edit`}</span>
									</button>
									<button className="btn btn-sm btn-outline-danger btn-entry-select-action btn-remove-entry" title={`Remove`}
										onClick={() => this.remove(entry)}>
										<i className="fal fa-fw fa-times"></i> <span className="sr-only">{`Remove`}</span>
									</button>
								</div>
							</li>
						))
					}
				</ul>
				<input type="hidden" name={this.props.name} ref={ele => {
					this.input = ele;

					if (ele != null) {
						ele.onGetValue = (v, input, e) => {

							if (input != this.input) {
								return v;
							}

							return this.state.value.map(entry => entry.id);
						}
					}
				}} />
				<footer className="admin-multiselect__footer">
					<button type="button" className="btn btn-sm btn-outline-primary btn-entry-select-action btn-new-entry"
						disabled={atMax ? true : undefined}
						onClick={e => {
							e.preventDefault();
							this.setState({
								showCreateOrEditModal: true,
								entityToEditId: this.state.selected
							});
						}}
					>
						{/*<i className="fal fa-fw fa-plus"></i> {`New ${this.props.label}...`}*/}
						<i className="fal fa-fw fa-plus"></i> {`New`}
					</button>
					<div className="admin-multiselect__search">
						{atMax ?
							<span className="admin-multiselect__search-max">
								<i>{`Max of ${this.props.max} added`}</i>
							</span> :
							<Search host={this.props.host} exclude={excludeIds} requestOpts={this.props.requestOpts} for={contentTypeLower} field={fieldName} limit={5}
								placeholder={`Find ${this.props.label} to add..`} onFind={entry => {
									if (!entry || this.state.value.some(entity => entity.id === entry.id)) {
										return;
									}

									var value = this.state.value;
									value.push(entry);

									this.setState({
										value
									});

									this.props.onChange && this.props.onChange({ target: { value: value.map(e => e.id) }, fullValue: value });
								}} />
						}
					</div>
				</footer>
				{this.state.showCreateOrEditModal &&
					<Modal title={`Edit ${this.props.label}`} visible isExtraLarge onClose={() => {
						this.setState({ showCreateOrEditModal: false, entityToEditId: null })
					}}>
						<AutoForm
						modalCancelCallback={() => {
							this.setState({ showCreateOrEditModal: false, entityToEditId: null });
						}}
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
							}} />
					</Modal>
				}
			</div>
		</>;
	}
}
