import Search from 'UI/Search';
import Modal from 'UI/Modal';
import Canvas from 'UI/Canvas';
import Image from 'UI/Image';
import Link from 'UI/Link';
import Icon from 'UI/Icon';
import * as fileRef from 'UI/FileRef';
import { isoConvert } from 'UI/Functions/DateTools';

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
		if (this.state.mustLoad) {

			var filter = {
				query: "Id=[?]",
				args: [this.state.value.map(e => e.id)]
			}
			
			var contentType = this.props.contentType || '';
			
			var api = require('Api/' + contentType).default;
			
			api.list(filter, this.props.includes).then(response => {
				
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
		this.runChange(value);
	}

	runChange(value) {
		this.setState({
			value
		});
		var e = { target: { value: value.map(e => e.id) }, fullValue: value };
		this.props.onRawChange && this.props.onRawChange(e);
		this.props.onChange && this.props.onChange(e);
	}

	render() {
		var fieldName = this.props.field;

		if (!fieldName) {

			if (this.state.value && this.state.value.length) {
				let val = this.state.value[0];
				let strings = Object.keys(val).filter(e => typeof val[e] === 'string' && e != 'type' && !e.endsWith('Ref') && e != 'media');

				if (strings.length) {
					var pref = ['name', 'description', 'title', 'summary'];

					pref.every(check => {

						if (strings.includes(check)) {
							fieldName = check;
							return false;
						}

						return true;
					});

				}

			}

		}
		
		var api = require('Api/' + this.props.contentType).default;
		
		if (!fieldName) {
			fieldName = 'name';
		}

		var displayFieldName = this.props.displayField || fieldName;
		if(displayFieldName.length){
			displayFieldName = displayFieldName[0].toLowerCase() + displayFieldName.substring(1);
		}

		// check to see if the object has a media ref
		var mediaRefFieldName = '';

		if (this.state.value != undefined && this.state.value.length > 0) {
			var tempObject = this.state.value[0];

			Object.keys(tempObject).every(key => {

				if (!tempObject[key]) {
					return true;
				}

				let val = tempObject[key].toString();

				if (fileRef.isRef(val)) {
				 	mediaRefFieldName = key;
				 	return false;
				}

				return true;
			});

        }

		// check to see if the object has a date range
		var metadataFields = [];
		var showmetadataFields = ["startdate", "enddate"];
		if (this.state.value != undefined && this.state.value.length > 0) {
			var tempObject = this.state.value[0];

			Object.keys(tempObject).forEach(function (key, index) {
				if (tempObject[key] != null) {
					if (showmetadataFields.includes(key.toLowerCase())){
						metadataFields.push(key);
					}
				}
			});
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
					<label className="form-label admin-multiselect__label">
						{this.props.label}
						<Link href={'/en-admin/' + contentTypeLower} target='_blank'>
							<Icon type='fa-external-link' />
						</Link>
					</label>
				)}
				<ul className="admin-multiselect__entries">
					{
						this.state.value.map((entry, i) => (
							<li key={entry.id} className="admin-multiselect__entry">
								<div>
									{this.props.renderEntry ? this.props.renderEntry(entry) : (
										displayFieldName.indexOf("Json") != -1 ? <Canvas>{entry[displayFieldName]}</Canvas> : entry[displayFieldName]
									)}
								</div>

								{metadataFields && metadataFields.length > 0 &&
									<div className="admin-multiselect__metadata">
										{metadataFields.map((metadataField) => (
											<div>{isoConvert(entry[metadataField]).toUTCString()}</div>
										))}
									</div>
								}

								<div className="admin-multiselect__entry-options">
									{mediaRefFieldName && mediaRefFieldName.length > 0 &&
										<div className="admin-multiselect__avatar">
											{fileRef.isImage(entry[mediaRefFieldName]) && <>
												<Image fileRef={entry[mediaRefFieldName]} size={32} />
											</>}
											{fileRef.isVideo(entry[mediaRefFieldName]) && <>
												<i className="fa fa-2x far-file"></i>
											</>}
										</div>
									}

									{this.props.showEntryActions && this.props.onEditEntry && (
										<button className="btn btn-sm btn-outline-primary btn-entry-select-action btn-view-entry" title={`Edit`}
											onClick={e => {
												e.preventDefault();
												this.props.onEditEntry(entry);
											}}>
											<i className="fal fa-fw fa-edit"></i> <span className="sr-only">{`Edit`}</span>
										</button>
									)}

									<button className="btn btn-sm btn-outline-danger btn-entry-select-action btn-remove-entry" title={`Remove`}
										onClick={e => {
											this.remove(entry);
											e.preventDefault();
										}}>
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
					{this.props.onCreateEntry && (
						<button type="button" className="btn btn-sm btn-outline-primary btn-entry-select-action btn-new-entry"
							disabled={atMax ? true : undefined}
							onClick={e => {
								e.preventDefault();
								this.props.onCreateEntry();
							}}
						>
							<i className="fal fa-fw fa-plus"></i> {`New`}
						</button>
					)}
					<div className="admin-multiselect__search">
						{atMax ?
							<span className="admin-multiselect__search-max">
								<i>{`Max of ${this.props.max} added`}</i>
							</span> :
							<Search endpoint={api.list} exclude={excludeIds} includes={this.props.includes} field={fieldName} limit={5}
								placeholder={`Find ${this.props.label} to add..`} onFind={entry => {
									if (!entry || this.state.value.some(entity => entity.id === entry.id)) {
										return;
									}

									var value = this.state.value;
									value.push(entry);
									this.runChange(value);
								}}
								onQuery={this.props.onQuery}
								onRender={this.props.renderSearchResult}
								/>
						}
					</div>
				</footer>
			</div>
		</>;
	}
}
