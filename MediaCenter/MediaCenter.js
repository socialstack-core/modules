import Tile from 'Admin/Tile';
import Loop from 'UI/Loop';
import Canvas from 'UI/Canvas';
import Col from 'UI/Column';
import Row from 'UI/Row';
import Search from 'UI/Search';
import Uploader from 'UI/Uploader';
import Modal from 'UI/Modal';
import Input from 'UI/Input';
import Image from 'UI/Image';
import webRequest from 'UI/Functions/WebRequest';
import getRef from 'UI/Functions/GetRef';
import {useTokens} from 'UI/Token';
import Default from 'Admin/Layouts/Default';

var fields = ['id', 'originalName'];
var searchFields = ['originalName'];


export default class MediaCenter extends React.Component {
	
	constructor(props){
		super(props);
		
		this.state={
			// If an id field is specified, that's the default sort
			sort: {field: 'id', direction: 'desc'}
		};
		
		this.renderHeader = this.renderHeader.bind(this);
		this.renderEntry = this.renderEntry.bind(this);
	}
	
	componentWillReceiveProps(props){
		
		var { sort } = this.state;
		
		if(sort && !fields.find(field => field == sort.field)){
			// Restore to id sort:
			this.setState({
				sort: {field: 'id', direction: 'desc'}
			});
		}
		
	}
	
	showRef(ref){
		// Check if it's an image/ video/ audio file. If yes, a preview is shown. Otherwise it'll be a preview link.
		var canShowImage = getRef.isImage(ref);
		
		return <a href={getRef(ref, {url: true})} alt={ref} target={'_blank'}>
			{
				canShowImage ? (
					<Image fileRef={ref} size={256} alt={ref}/>
				) : 'View selected file'
			}
		</a>;
		
	}
	
	renderHeader(allContent){
		// Header (Optional)
		var heads = fields.map(field => {
			
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
			</th>,
			<th>
				File
			</th>
		].concat(heads);
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
		var path = '/en-admin/upload/';
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
		return [checkbox, <td>{this.showRef(entry.ref)}</td>].concat(fields.map(field => {
			
			var fieldValue = entry[field];
			
			if(field.endsWith("Json")) {
				fieldValue = <Canvas>{fieldValue}</Canvas>;
			}else if(field == "id") {
				if(entry.isDraft){
					fieldValue = <span>{fieldValue} <span className="is-draft">(Draft)</span></span>;
				}
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
		
		return <div className="bulk-actions">
			<p>
				{message}
			</p>
			<button className="btn btn-danger" onClick={() => this.startDelete()}>
				Delete selected
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
			'upload/' + id,
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
	
	renderConfirmDelete(count){
		return <Modal visible onClose={() => this.cancelDelete()}>
				<p>
				{`Are you sure you want to delete ${count} item(s)?`}
				</p>
				<div>
					<Input inline type="button" className="btn btn-danger" onClick={() => this.confirmDelete()}>Yes, delete</Input>
					<Input inline type="button" className="btn btn-secondary" style={{ marginLeft: '10px' }} onClick={() => this.cancelDelete()}>Cancel</Input>
				</div>
		</Modal>;
	}
	
	render(){
		var combinedFilter = {};
		
		if(!combinedFilter.sort && this.state.sort){
			combinedFilter.sort = this.state.sort;
		}
		
		if(this.state.searchText && searchFields){
			var where = [];
			
			for(var i=0;i< searchFields.length;i++){
				var ob = {};
				
				var field = searchFields[i];
				var fieldNameUcFirst = field.charAt(0).toUpperCase() + field.slice(1);
				
				ob[fieldNameUcFirst] = {
					contains: this.state.searchText
				};
				
				where.push(ob);
			}
			
			combinedFilter.where = where;
		}
		
		var path = '/en-admin/upload/';
		
		var addUrl = path + 'add';
		
		var selectedCount = this.selectedCount();

		return  <Default>
			<Tile className="media-center" title={'Uploads'}>
				<Row style={{marginBottom: '10px'}}>
					<Col>
						<Uploader onUploaded={() => window.location.reload(true)}/>
					</Col>
					<Col>
						{searchFields && (
							<Search onQuery={(where, query) => {
								this.setState({
									searchText: query
								});
							}}/>
						)}
					</Col>
				</Row>
				<Loop asTable over={'upload/list'} onResults={results => {
					// Either changed page or loaded for first time - clear bulk selects if there is any.
					if(this.state.bulkSelections){
						this.setState({bulkSelections: null});
					}
					
					return results;
					
				}} filter={combinedFilter} paged>
				{[
					this.renderHeader,
					this.renderEntry
				]}
				</Loop>
				{selectedCount > 0 ? this.renderBulkOptions(selectedCount) : null}
				{this.state.confirmDelete && this.renderConfirmDelete(selectedCount)}

			</Tile>
		</Default>;
    }
}