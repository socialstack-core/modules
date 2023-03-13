import GraphNode from 'Admin/CanvasEditor/GraphEditor/GraphNode';

/*
* Defines the admin UI handler for the SendEmail graph node.
* This is separate from the graph node executor which exists on the frontend.
*/
export default class SendEmail extends GraphNode {
	
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
		}, {
			key: 'thenDo',
			name: `Then`,
			direction: 'out',
			type: 'execute',
		});
		return fields;
	}
	
}