import GraphNode from 'Admin/CanvasEditor/GraphEditor/GraphNode';

/*
* Defines the admin UI handler for the Loop graph node.
* This is separate from the graph node executor which exists on the frontend.
*/
export default class Loop extends GraphNode {
	
	constructor(props){
		super(props);
	}
	
	renderFields() {
		
		var listOfItems = this.state.listOfItems;
		var eleType = listOfItems && listOfItems.type && listOfItems.type.elementType;
		
		
		var fields = [
			{
				key: 'listOfItems',
				name: `List of items`,
				type: eleType ? listOfItems.type : 'array'
				// direction:in is implied
			}
		];
		
		// If list of items is connected to anything 
		// then we have some additional fields.
		
		if(eleType){
			// We've got an input on the listOfItems field.
			
			fields.push({
				key: 'loopItem',
				name: `Loop item`,
				type: eleType,
				direction: 'out'
			},
			{
				key: 'loopIndex',
				name: `Loop index`,
				type: 'int',
				direction: 'out'
			},
			{
				key: 'loopResult',
				name: `Iteration result`,
				type: 'object'
				// direction:in is implied
			});
			
			if(this.state.loopResult){
				
				var type = this.getInputType(this.state.loopResult);
				
				fields.push({
					key: 'output',
					name: `Output`,
					type: {name: 'array', elementType: type}
				});
				
			}
		}
		
		return fields;
	}
	
}