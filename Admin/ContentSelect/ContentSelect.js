import webRequest from 'UI/Functions/WebRequest';
import omit from 'UI/Functions/Omit';
import Input from 'UI/Input';
import Search from 'UI/Search';
import AutoForm from 'Admin/AutoForm';
import Modal from 'UI/Modal';

/**
 * Dropdown to select a piece of content.
 */
export default class ContentSelect extends React.Component {
	
	constructor(props){
		super(props);

		this.state = {
			searchSelected: null,
			selected: this.getDefaultValue(props),
			showCreateOrEditModal: false
		};
		this.load(props, true);
	}
	
	componentWillReceiveProps(props){
		this.load(props);
	}

	getDefaultValue(props) {
		var value = null;

		if (props.defaultValue) {
			if (props.defaultValue.id) {
				value = props.defaultValue.id;
			} else {
				value = props.defaultValue;
			}
		}

		return value;
	}
	
	load(props, first){
		if(props.search){
			var value = props.value || props.defaultValue;
			if(value){
				webRequest(props.contentType.toLowerCase() + '/' + value).then(response => {
					this.setState({
						searchSelected: response ? response.json : null
					});
				});
			}else if(!first){
				this.setState({
					searchSelected: null
				});
			}
		}else{
			webRequest(props.contentType.toLowerCase() + '/list').then(response => {
				var all = response.json.results;
				if (!props.hideDefaultValue) {
					all.unshift(null);
				}
				var selected = this.state.selected ? this.state.selected : this.getDefaultValue(props);
				this.setState({all, selected: selected });
			});
		}
	}

	onChange(e) {
		if (this.props.onChange) {
			this.props.onChange(e);
		}

		var value = null;

		if (e && e.target && e.target.value && e.target.value > 0) {
			value = e.target.value;
		}

		this.setState({ selected: value });
	}
		
	render(){
		if(this.props.search){
			
			var {searchSelected} = this.state;
			var title = '';

			if (searchSelected) {
				var titleField = this.props.titleField;
				title = titleField && titleField.length && searchSelected[titleField] ? searchSelected[titleField] : searchSelected.title || searchSelected.firstName || searchSelected.username || searchSelected.name || searchSelected.url;
			}

			var value = this.props.defaultValue || this.props.value;

			return <div className="mb-3 content-select">
				{this.props.label && (
					<label className="form-label">{this.props.label}</label>
				)}
				<input type="hidden" ref={
					ele => {
						this.input = ele;
						if(ele != null){
							ele.onGetValue=(v, input, e) => {
								if(input != this.input){
									return v;
								}
								
								return this.state.searchSelected ? this.state.searchSelected.id : '0';
							}
						}
					}
				}
				name={this.props.name} />
				<Search for={this.props.contentType} field={this.props.titleField || this.props.search} limit={5} placeholder={"Search for a " + this.props.contentType + ".."} onFind={entry => {
					this.setState({
						searchSelected: entry
					});
				}}/>
				{
					<div className="selected-content">
						{searchSelected ? title : (
							value ? 'Item #' + value : 'None selected'
						)}
					</div>
				}
			</div>;
			
		}

		var all = this.state.all;
		var titleField = this.props.titleField;

		if (all) {

			if (!titleField) {
				var firstValidItem = all.find(arg => arg != null);

				if (firstValidItem) {
					var fieldNames = ['title', 'firstName', 'username', 'name', 'url'];

					fieldNames.forEach(field => {
						if (!titleField && firstValidItem[field]) {
							titleField = field;
						}
					});

				}

			}

			// sort by title field
			if (titleField && titleField.length) {
				all = all.sort((a, b) => {

					if (a && b) {
						let titleA = (a[titleField] != undefined && a[titleField].trim() != "") ? a[titleField] : ` No identified title`;
						let titleB = (b[titleField] != undefined && b[titleField].trim() != "") ? b[titleField] : ` No identified title`;

						return titleA.localeCompare(titleB);
					}

					return -1;
				});

				// ensure "none" item is first in the list
				let noneIndex = -1;

				all.forEach((content, i) => {
					if (!content && noneIndex == -1) {
						noneIndex = i;
					}
				});

				if (noneIndex > -1) {
					all.splice(noneIndex, 1);
					all.unshift(null);
				}

			}
		}

		var noSelection = this.props.noSelection || `None`;
		var mobileNoSelection = this.props.mobileNoSelection || `None`;
		
		if (window.matchMedia('(max-width: 752px) and (pointer: coarse) and (orientation: portrait)').matches ||
			window.matchMedia('(max-height: 752px) and (pointer: coarse) and (orientation: landscape)').matches) {
			noSelection = mobileNoSelection;
		}

		return (<div className="content-select">
			<Input {...omit(this.props, ['contentType'])} noWrapper
				type="select" defaultValue={this.state.selected ? this.state.selected : null} onChange={e => { this.onChange(e); }}>{
				all ? all.map(content => {
					if(!content){
						return <option value={'0'}>{noSelection}</option>;
					}

					var title = titleField && titleField.length && content[titleField] ? content[titleField] : ` No identified title`;
					return <option value={content.id}>{`${title} (#${content.id})`}</option>;
				}) : [
				]
			}</Input>

			<footer className="content-select__footer">
				<button className="btn btn-sm btn-outline-primary btn-content-select-action btn-add-content"
					onClick={e => {
						e.preventDefault();
						this.setState({ showCreateOrEditModal: true });
					}}>
					{/*<i className="fal fa-fw fa-plus"></i> {`New ${this.props.label}`}*/}
					<i className="fal fa-fw fa-plus"></i> {`New`}
				</button>

				{this.state.selected &&
					<button className="btn btn-sm btn-outline-primary btn-content-select-action btn-edit-content"
						onClick={e => {
							e.preventDefault();
							this.setState({ showCreateOrEditModal: true, entityToEditId: this.state.selected });
					}}>
						{/*<i className="fal fa-fw fa-edit"></i> {`Edit ${this.props.label}`}*/}
						<i className="fal fa-fw fa-edit"></i> {`Edit`}
					</button>
				}

				{this.state.selected &&
					<a className="btn btn-sm btn-outline-secondary btn-content-select-action btn-visit-content"
						href={"/en-admin/" + this.props.contentType.toLowerCase() + "/" + this.state.selected}>
						{/*<i className="fal fa-fw fa-external-link"></i> {`Visit ${this.props.label}`}*/}
						<i className="fal fa-fw fa-external-link"></i> {`Visit`}
					</a>
				}

			</footer>
			
			{this.state.showCreateOrEditModal &&
				<Modal
					title={this.state.entityToEditId ? `Edit ${this.props.label}` : `Create New ${this.props.label}`}
					onClose={() => {
						this.setState({ showCreateOrEditModal: false, entityToEditId: null });
					}}
					visible
					isExtraLarge
				>
				<AutoForm
					canvasContext={this.props.canvasContext ? this.props.canvasContext : this.props.currentContent}
					modalCancelCallback={() => {
						this.setState({ showCreateOrEditModal: false, entityToEditId: null });
					}}
						endpoint={this.props.contentType.toLowerCase()}
						singular={this.props.label} 
						plural={this.props.label + "s"}
						id={this.state.entityToEditId ? this.state.entityToEditId : null}
						onActionComplete={entity => {
							this.setState({ showCreateOrEditModal: false, entityToEditId: null, selected: entity.id });
							this.load(this.props);

							/* 
								Todo: Does the user expect the parent entity to be saved upon completing this action? 
								Currently you still need to press 'Save and Publish' on the parent form to save the value of this field.
							*/
						}} />
				</Modal>
			}
		</div>);
	}
	
}