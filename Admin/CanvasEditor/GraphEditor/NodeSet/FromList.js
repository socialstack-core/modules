import GraphNode from 'Admin/CanvasEditor/GraphEditor/GraphNode';
import Input from 'UI/Input';

/*
* Defines the admin UI handler for the FromList graph node.
* This is separate from the graph node executor which exists on the frontend.
*/
export default class FromList extends GraphNode {
	
	constructor(props){
		super(props);
	}
	
	renderFields() {
		
		var listOfItems = this.state.listOfItems;
		
		var inputType = listOfItems ? this.getInputType(listOfItems) : null;
		
		var fields = [{
				key: 'listOfItems',
				name: 'List of items',
				type: inputType || 'array',
			},{
				key: 'index',
				name: 'List index (first = 0)',
				type: 'int',
				onRender: (value, onSetValue) => {
					return <Input type={'number'} value={value && !value.link ? value : undefined} onChange={e => {
						onSetValue(e.target.value);
					}} onKeyUp={e => {
						onSetValue(e.target.value);
					}}/>;
				}
			}
		];
		
		if(listOfItems && this.state.index != undefined){
			
			if(inputType && inputType.elementType){
				// We've got a list and an index
				var outputType = inputType.elementType;
				
				this.setType(outputType);
				
				fields.push({
					key: 'output',
					type: outputType,
					name: 'output',
					direction: 'out'
				});
			}
		}
		
		
		return fields;
	}
	
}