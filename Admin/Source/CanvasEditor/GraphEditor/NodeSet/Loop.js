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
		var eleType = listOfItems ? this.getInputType(listOfItems) : null;
		
		var fields = [
			{
				key: 'listOfItems',
				name: `List of items`,
				type: eleType || 'array'
				// direction:in is implied
			}
		];
		
		// If list of items is connected to anything 
		// then we have some additional fields.
		
		if(eleType){
			// We've got an input on the listOfItems field.
			var loopResType = this.state.loopResult ? this.getInputType(this.state.loopResult) : null;
			
			fields.push({
				key: 'loopItem',
				name: `Loop item`,
				type: eleType.elementType,
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
				type: loopResType || 'object'
				// direction:in is implied
			});
			
			if(this.state.loopResult){
				
				fields.push({
					key: 'output',
					name: `Output`,
					direction: 'out',
					type: {name: 'array', elementType: loopResType}
				});
				
			}
		}
		
		return fields;
	}
	
}