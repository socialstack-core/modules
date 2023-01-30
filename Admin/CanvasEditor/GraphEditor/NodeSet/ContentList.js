import GraphNode from '../GraphNode';
import ContentTypeSelect from '../ContentTypeSelect';

/*
* Defines the admin UI handler for the Content List graph node.
* This is separate from the graph node executor which exists on the frontend.
*/
export default class ContentList extends GraphNode {
	
	constructor(props){
		super(props);
	}
	
	countArgs(query){
		var count = 0;
		for(var i=0; i<query.length;i++){
			if('?' === query[i]){
				count++;
			}
		}
		return count;
	}
	
	renderFields() {
		
		var fields = [{
				key: 'contentType',
				name: `Content Type`,
				type: 'contentType',
				onRender: (value, onSetValue, label) => {
					return <ContentTypeSelect label={label} value={value} onChange={e => {
						
						var typeName = e.target.value;
						onSetValue(typeName);
						
					}} />
				},
				direction: 'none'
			}
		];
		
		var cType = this.state.contentType;
		
		if(cType){
			// We've got a content type selected. 
			
			fields.push({
				key: 'filter',
				type: 'text',
				name: 'Filter'
			});
			
			var outputType = {name: 'array', elementType: cType};
			
			this.setType(outputType);
			
			var filter = this.state.filter;
			
			if(filter && typeof filter == 'string'){
				// Parse the filter string to establish what type the args are.
				// For now though we can just do any type.
				var argCount = this.countArgs(filter);
				
				for(var i=0;i<argCount;i++){
					fields.push({
						key: 'arg' + i,
						type: 'object',
						name: 'Filter Value ' + (i+1)
					});
				}
			}
			
			fields.push({
				key: 'output',
				type: outputType,
				name: `Output`,
				direction: 'out'
			});
		}
		
		return fields;
	}
	
}