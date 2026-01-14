import Search from 'UI/Search';
import Modal from 'UI/Modal';
import Canvas from 'UI/Canvas';
import Image from 'UI/Image';
import * as fileRef from 'UI/FileRef';

import addressApi from 'Api/Address';

var AutoForm = null;

/**
 * A general use "multi-selection"; primarily used for tags and categories.
 */

export default class AddressSelect extends React.Component {
	
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
		if (!this.state.mustLoad) {

			var filter = {
				query: "Id=[?]",
				args: [this.state.value.map(e => e.id)]
			}
			
			addressApi.list(filter).then(response => {
				
				console.log("ProductAttributeValue", response);

				// Loading the values and preserving order:
				var idLookup = {};
				response.results.forEach(r => {idLookup[r.id+''] = r;});

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

    renderResult(result) {

		return (
			<>
				{result.name}{`:`}&nbsp;{result.postcode}
			</>
			);
    }

	render() {
		var fieldName = this.props.field;

		if (!fieldName) {
		var fieldName = 'name';
		}

		var displayFieldName = this.props.displayField || fieldName;
		if(displayFieldName.length){
			displayFieldName = displayFieldName[0].toLowerCase() + displayFieldName.substring(1);
		}

		// check to see if the object has a media ref
		var mediaRefFieldName = 'featureRef';

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

								<div>
									{
										displayFieldName.indexOf("Json") != -1 ? <Canvas>{entry[displayFieldName]}</Canvas> : entry[displayFieldName]
									}
								</div>

								<div className="admin-multiselect__entry-options">
									{mediaRefFieldName && mediaRefFieldName.length > 0 && entry[mediaRefFieldName] && entry[mediaRefFieldName].length > 0 && 
										<div className="admin-multiselect__avatar">
											{fileRef.isImage(entry[mediaRefFieldName]) && <>
												<Image fileRef={entry[mediaRefFieldName]} size={32} />
											</>}
											{fileRef.isVideo(entry[mediaRefFieldName]) && <>
												<i className="fa fa-2x far-file"></i>
											</>}
										</div>
									}

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
							<Search endpoint={addressApi.list} exclude={excludeIds} field={fieldName} limit={5}
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
								}} 
								
								onRender={result => this.renderResult(result)}

								/>
						}
					</div>
				</footer>
				{this.state.showCreateOrEditModal &&
					<Modal
						title={this.state.entityToEditId ? `Edit ${this.props.contentType}` : `Create New ${this.props.contentType}`}
						visible
						isExtraLarge
						onClose={() => {
						this.setState({ showCreateOrEditModal: false, entityToEditId: null })
						}}
					>
						<AutoForm
							canvasContext={this.props.canvasContext ? this.props.canvasContext : this.props.currentContent}
							modalCancelCallback={() => {
								this.setState({ showCreateOrEditModal: false, entityToEditId: null });
							}}
								endpoint={this.props.contentType}
								singular={this.props.contentType}
								plural={this.props.contentType + "s"}
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

									this.props.onChange && this.props.onChange({ target: { value: value.map(e => e.id) }, fullValue: value });
								}} 
						/>
					</Modal>
				}
			</div>
		</>;
	}
}
