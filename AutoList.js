import Tile from 'Admin/Tile';
import Loop from 'UI/Loop';
import Canvas from 'UI/Canvas';
import Col from 'UI/Column';
import Row from 'UI/Row';
import Search from 'UI/Search';


export default class AutoList extends React.Component {
	
	constructor(props){
		super(props);
		
		this.state={
			// If an id field is specified, that's the default sort
			sort: this.props.fields.find(field => field == 'id') ? {field: 'id', direction: 'desc'} : null
		};
		
		this.renderHeader = this.renderHeader.bind(this);
		this.renderEntry = this.renderEntry.bind(this);
	}
	
	renderHeader(allContent){
		// Header (Optional)
		return this.props.fields.map(field => {
			
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
	}
	
	renderEntry(entry) {
		var path = '/en-admin/' + this.props.endpoint + '/';
		
		// Each row
		return this.props.fields.map(field => {
			
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
		});
	}
	
	render(){
		var {filter, filterField, filterValue, searchFields} = this.props;
		
		if(filterField){
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
		
		var path = '/en-admin/' + this.props.endpoint + '/';
		
		var addUrl = path + 'add';
		
		if(filterField){
			var filterFieldLC = filterField.charAt(0).toLowerCase() + filterField.slice(1);
			addUrl += '?' + filterFieldLC + '=' + filterValue;
		}
		
		return <Tile className="auto-list" title={this.props.title}>
			{(this.props.create || searchFields) && (
				<Row style={{marginBottom: '10px'}}>
					<Col>
						{this.props.create && (
								<a href={ addUrl } className="btn btn-primary">
									Create
								</a>
						)}
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
			)}
			<Loop asTable over={this.props.endpoint + '/revision/list'} filter={{where: {IsDraft: 1}}} onFailed={e => {
				// Revisions aren't supported.
				return true;
			}}>
				{[
					this.renderHeader,
					this.renderEntry
				]}
			</Loop>
			<Loop asTable over={this.props.endpoint + "/list"} {...this.props} filter={combinedFilter} paged>
			{[
				this.renderHeader,
				this.renderEntry
			]}
			</Loop>
		</Tile>;
	}
}