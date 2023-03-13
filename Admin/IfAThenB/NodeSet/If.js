import GraphNode from 'Admin/CanvasEditor/GraphEditor/GraphNode';

/*
* Defines the admin UI handler for the If graph node.
* This is separate from the graph node executor which exists on the frontend.
*/
export default class If extends GraphNode {
	
	constructor(props){
		super(props);
	}
	
	renderFields() {
		
		var fields = [
		];
		
		fields.push({
			key: 'doThis',
			name: `Do this`,
			type: 'execute',
		});
		
		var inType = this.getInputType(this.state.expression);
		var bool = inType && inType.name == 'boolean';
		
		fields.push({
			key: 'expression',
			name: `Value to check`,
			type: inType || 'anything',
		});
		
		fields.push({
			key: 'ifTrue',
			name: bool ? `If it's yes` : `If it has a value`,
			type: 'execute',
			direction: 'out'
		});
		
		fields.push({
			key: 'ifFalse',
			name: bool ? `If it's no` : `If it doesn't`,
			type: 'execute',
			direction: 'out'
		});
		
		return fields;
	}
	
}