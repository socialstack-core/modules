import Tile from 'Admin/Tile';
import Loop from 'UI/Loop';
import Canvas from 'UI/Canvas';


export default class AutoList extends React.Component {
	
	constructor(props){
		super(props);
		
		this.state={
			// If an id field is specified, that's the default sort
			sort: this.props.fields.find(field => field == 'id') ? {field: 'id', direction: 'desc'} : null
		};
	}
	
	render(){
		var {filter, filterField, filterValue} = this.props;
		
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
		
		var path = '/en-admin/' + this.props.endpoint + '/';
		
		return <Tile className="auto-list">
			{this.props.create && (
				<div style={{marginBottom: '10px'}}>
					<a href={path + 'add'} className="btn btn-primary">
						Create
					</a>
				</div>
			)}
			<Loop asTable over={this.props.endpoint + "/list"} {...this.props} filter={combinedFilter}>
			{[
				allContent => {
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
				},
				entry => {
					// Each row
					return this.props.fields.map(field => <td><a href={path + '' + entry.id}>{
							field.endsWith("Json") ? <Canvas>{entry[field]}</Canvas> : entry[field]
						}</a></td>);
				}
			]}
			</Loop>
		</Tile>;
	}
}