import GraphNode from 'Admin/CanvasEditor/GraphEditor/GraphNode';

/*
* Defines the admin UI handler for the CurrentContext graph node.
* This is separate from the graph node executor which exists on the frontend.
*/
export default class CurrentContext extends GraphNode {
	
	constructor(props){
		super(props);
	}
	
	renderFields() {
		
		var fields = [{
				key: 'UserId',
				type: 'uint',
				name: 'UserId',
				direction: 'out'
			},
			{
				key: 'RoleId',
				type: 'uint',
				name: 'RoleId',
				direction: 'out'
			},
			{
				key: 'LocaleId',
				type: 'uint',
				name: 'LocaleId',
				direction: 'out'
			}
		];
		
		return fields;
	}
	
}