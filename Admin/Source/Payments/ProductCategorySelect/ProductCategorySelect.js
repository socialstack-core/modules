import Search from 'UI/Search';
import Link from 'UI/Link';
import Canvas from 'UI/Canvas';
import Image from 'UI/Image';
import Button from 'UI/Button';
import * as fileRef from 'UI/FileRef';

import productCategoryApi from 'Api/ProductCategory';


/**
 * A general use "multi-selection"; primarily used for tags and categories.
 */

export default class ProductCategorySelect extends React.Component {
	
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
		if (!this.state.mustLoad) {

			var filter = {
				query: "Id=[?]",
				args: [this.state.value.map(e => e.id)]
			}

			productCategoryApi.list(filter, this.props.includes).then(response => {
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
			fieldName = 'name';
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
						{this.props.label} <Link href='/en-admin/product/'><i className="fa fa-external-link" /></Link>
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
									
									<Button sm outlined variant="danger" className="btn-entry-select-action btn-remove-entry" title={`Remove`} onClick={() => this.remove(entry)}>
										<i className="fal fa-fw fa-times"></i> <span className="sr-only">{`Remove`}</span>
									</Button>
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
					<div className="admin-multiselect__search">
						{atMax ?
							<span className="admin-multiselect__search-max">
								<i>{`Max of ${this.props.max} added`}</i>
							</span> :
							<Search endpoint={productCategoryApi.list} exclude={excludeIds} field={fieldName} includes={this.props.includes} limit={5}
								placeholder={`Find ${this.props.label} to add..`} onFind={entry => {
									if (!entry || this.state.value.some(entity => entity.id === entry.id)) {
										return;
									}

									var value = this.state.value;
									value.push(entry);
									this.runChange(value);
								}} />
						}
					</div>
				</footer>
			</div>
		</>;
	}
}
