import Loop from 'UI/Loop';
import Canvas from 'UI/Canvas';
import Search from 'UI/Search';
import ConfirmModal from 'UI/Modal/ConfirmModal';
import webRequest from 'UI/Functions/WebRequest';
import {useTokens} from 'UI/Token';
import {isoConvert} from "UI/Functions/DateTools";

export default class AutoList extends React.Component {
	
	constructor(props){
		super(props);
		
		this.state={
			// If an id field is specified, that's the default sort
			sort: this.props.fields.find(field => field == 'id') ? {field: 'id', direction: 'desc'} : null
		};
		
		this.renderHeader = this.renderHeader.bind(this);
		this.renderColgroups = this.renderColgroups.bind(this);
		this.renderEntry = this.renderEntry.bind(this);
	}
	
	componentWillReceiveProps(props){
		
		var { sort } = this.state;
		var { fields } = props;
		
		if(sort && !fields.find(field => field == sort.field)){
			// Restore to id sort:
			this.setState({
				sort: fields.find(field => field == 'id') ? {field: 'id', direction: 'desc'} : null
			});
		}
		
	}

	renderEmpty() {
		return <table className="table">
			<thead>
				<tr>
					{this.renderHeader()}
				</tr>
			</thead>
			<colgroup>
				{this.renderColgroups()}
			</colgroup>
			<tbody>
				<tr>
					<td colspan={this.props.fields.length + 1} className="table__empty-message">
						{this.state.searchText ? `No matching records for "${this.state.searchText}"` : `No data available`}
					</td>
				</tr>
			</tbody>
		</table>;
	}

	renderHeader(allContent){
		// Header (Optional)
		var fields = this.props.fields.map(field => {
			
			// Class for styling the sort buttons:
			var sortingByThis = this.state.sort && this.state.sort.field == field;
			var className = '';
			
			if(sortingByThis){
				className = this.state.sort.direction == 'desc' ? 'sorted-desc' : 'sorted-asc';
			}
			
			// Field name with its first letter uppercased:
			var ucFirstFieldName = field.length ? field.charAt(0).toUpperCase() + field.slice(1) : '';
			
			return (
				<th className={className}>
					{ucFirstFieldName} <i className="fa fa-caret-down" onClick={() => {
						// Sort desc
						this.setState({
							sort: {
								field,
								direction: 'desc'
							}
						});
					}}/> <i className="fa fa-caret-up" onClick={() => {
						// Sort asc
						this.setState({
							sort: {
								field,
								direction: 'asc'
							}
						});
					}}/>
				</th>
			);
		});
		
		// If everything in allContent is selected, mark this as selected as well.
		var checked = false;
		var {bulkSelections} = this.state;
		
		if(bulkSelections && allContent.length){
			checked = true;
			allContent.forEach(e => {
				if(!bulkSelections[e.id]){
					checked = false;
				}
			});
		}
		
		return [
			<th>
				<input type='checkbox' checked={checked} onClick={() => {
					
					// Check or uncheck all things.
					if(checked){
						this.setState({bulkSelections: null});
					}else{
						bulkSelections = {};
						allContent.forEach(e => bulkSelections[e.id] = true);
						this.setState({bulkSelections});
					}
					
				}} />
			</th>
		].concat(fields);
	}
	
	renderColgroups(allContent) {
		var fields = this.props.fields.map(field => {
			var className = '';

			switch (field) {
				case 'id':
					className = 'col__id';
					break;
            }

			return (
				<col className={className}>
				</col>
			);
		});

		return [
			<col className='col__select'>
			</col>
		].concat(fields);
	}

	selectedCount(){
		var {bulkSelections} = this.state;
		if(!bulkSelections){
			return 0;
		}
		var c = 0;
		for(var k in bulkSelections){
			c++;
		}
		return c;
	}
	
	renderEntry(entry) {
		var {bulkSelections} = this.state;
		var path = this.props.customUrl 
			? '/en-admin/' + this.props.customUrl + '/'
			: '/en-admin/' + this.props.endpoint + '/';
		var checked = bulkSelections && !!bulkSelections[entry.id];
		var checkbox = <td>
			<input type='checkbox' checked={checked} onChange={() => {
				
				if(bulkSelections && bulkSelections[entry.id]){
					delete bulkSelections[entry.id];
				}else{
					if(!bulkSelections){
						bulkSelections = {};
					}
					bulkSelections[entry.id] = true;
				}
				
				this.setState({bulkSelections});
			}}/>
		</td>;
		
		// Each row
		return [checkbox].concat(this.props.fields.map(field => {
			
			var fieldValue = entry[field];
	
			if(field.endsWith("Json") || (typeof fieldValue == "string" && fieldValue.toLowerCase().indexOf('"c":') >= 0)) {
				fieldValue = <Canvas>{fieldValue}</Canvas>;
			}else if(field == "id") {
				if(entry.isDraft){
					fieldValue = <span>{fieldValue} <span className="is-draft">(Draft)</span></span>;
				}
			}else if(typeof fieldValue == "number" && field.toLowerCase().endsWith("utc")) {
				fieldValue = isoConvert(fieldValue).toUTCString();
			}
			
			return <td>
				<a href={path + '' + entry.id + (entry.revisionId ? '?revision=' + entry.revisionId : '')}>
				{
					fieldValue
				}
				</a>
			</td>
		}));
	}
	
	renderBulkOptions(selectedCount){
		var message = (selectedCount > 1) ? `${selectedCount} items selected` : `1 item selected`;
		
		return <div className="admin-page__footer-actions">
			<span className="admin-page__footer-actions-label">
				{message}
			</span>
			<button type="button" className="btn btn-danger" onClick={() => this.startDelete()}>
				{`Delete selected`}
			</button>
		</div>;
	}
	
	startDelete() {
		this.setState({
			confirmDelete: true
		});
	}

	cancelDelete() {
		this.setState({
			confirmDelete: false
		});
	}

	confirmDelete() {
		this.setState({
			confirmDelete: false,
			deleting: true
		});
		
		// get the item IDs:
		var ids = Object.keys(this.state.bulkSelections);
		
		var deletes = ids.map(id => webRequest(
			this.props.endpoint + '/' + id,
			null,
			{ method: 'delete' }
		));
		
		Promise.all(deletes).then(response => {
			
			this.setState({bulkSelections: null});
			
		}).catch(e => {
			console.error(e);
			this.setState({
				deleting: false,
				deleteFailed: true
			});
		});
	}
	
	renderConfirmDelete(count) {
		return <ConfirmModal confirmCallback={() => this.confirmDelete()} confirmVariant="danger" cancelCallback={() => this.cancelDelete()}>
			<p>
				{`Are you sure you want to delete ${count} item(s)?`}
			</p>
		</ConfirmModal>
	}

	capitalise(name) {
		return name && name.length ? name.charAt(0).toUpperCase() + name.slice(1) : "";
	}

	render(){
		var {filter, filterField, filterValue, searchFields} = this.props;

		var searchFieldsDesc = searchFields ? searchFields.join(', ') : undefined;

		if(filterField){
            filterValue = useTokens(filterValue);

			var where = {};
			where[filterField] = filterValue;
			filter = {
				where
			};
		}
		
		var combinedFilter = {...filter};
		
		if(!combinedFilter.sort && this.state.sort){
			combinedFilter.sort = this.state.sort;
		}
		
		if(this.state.searchText && searchFields){
			var where = [];
			
			for(var i=0;i< searchFields.length;i++){
				var ob = {};
				
				if(filterField){
					ob[filterField] = filterValue;
				}
				
				var field = searchFields[i];
				var fieldNameUcFirst = field.charAt(0).toUpperCase() + field.slice(1);
				
				ob[fieldNameUcFirst] = {
					contains: this.state.searchText
				};
				
				where.push(ob);
			}
			
			combinedFilter.where = where;
		}
		
		var addUrl = this.props.customUrl 
			? '/en-admin/' + this.props.customUrl + '/' + 'add'
			: '/en-admin/' + this.props.endpoint + '/' + 'add';
		
		if(filterField){
			var filterFieldLC = filterField.charAt(0).toLowerCase() + filterField.slice(1);
			addUrl += '?' + filterFieldLC + '=' + filterValue;
		}
		
		// Todo: improve how revisions are found to be available or not
		var revisionsSupported = (this.props.endpoint != 'user');
		
		var selectedCount = this.selectedCount();

        if (!filterField)
            filterField = "";

		return <>
			<div className="admin-page">
				<header className="admin-page__subheader">
					<div className="admin-page__subheader-info">
						<h1 className="admin-page__title">
							{this.props.title}
						</h1>
						<ul className="admin-page__breadcrumbs">
							{/*this.props.previousPageUrl && this.props.previousPageName &&
							<p>
								<a href={this.props.previousPageUrl}>
									{this.props.previousPageName}
								</a> &gt; <span>
									{this.props.title}
								</span>
							</p>
						*/}

							<li>
								<a href={'/en-admin/'}>
									{`Admin`}
								</a>
							</li>
							<li>
								{this.capitalise(this.props.plural)}
							</li>
						</ul>
					</div>
					{searchFields && <>
						<Search className="admin-page__search" placeholder={`Search ${searchFieldsDesc}`}
							onQuery={(where, query) => {
								this.setState({
									searchText: query
								});
							}} />
					</>}
				</header>
				<div className="admin-page__content">
					<div className="admin-page__internal">
						{this.props.beforeList}
						{/* TODO: split revisions into a separate tab if available */}
						{revisionsSupported && (
							<Loop asTable over={this.props.endpoint + '/revision/list'} filter={{ where: { IsDraft: 1 } }}
								xorNone={() => this.renderEmpty()}
								onFailed={e => {
									// Revisions aren't supported.
									return true;
								}}>
								{[
									this.renderHeader,
									this.renderColgroups,
									this.renderEntry
								]}
							</Loop>
						)}

						<Loop asTable over={this.props.endpoint + "/list"} {...this.props} filter={combinedFilter} paged
							orNone={() => this.renderEmpty()}
							onResults={results => {
								// Either changed page or loaded for first time - clear bulk selects if there is any.
								if (this.state.bulkSelections) {
									this.setState({ bulkSelections: null });
								}

								return results;
							}}>
							{[
								this.renderHeader,
								this.renderColgroups,
								this.renderEntry
							]}
						</Loop>
						{this.state.confirmDelete && this.renderConfirmDelete(selectedCount)}
					</div>
					{/*feedback && <>
						<footer className="admin-page__feedback">
						</footer>
					</>*/}
					<footer className="admin-page__footer">
						{selectedCount > 0 ? this.renderBulkOptions(selectedCount) : null}
						{this.props.create && <>
							<a href={addUrl} className="btn btn-primary">
								{`Create`}
							</a>
						</>}
					</footer>
				</div>
			</div>
		</>;
    }
}