import GraphNode from 'Admin/CanvasEditor/GraphEditor/GraphNode';
import Input from 'UI/Input';

/*
* Defines the admin UI handler for the Constant graph node.
* This is separate from the graph node executor which exists on the frontend.
*/
export default class Constant extends GraphNode {
	
	constructor(props){
		super(props);
	}
	
	renderFields() {
		
		var fields = [
			{
				key: 'constantType',
				name: `Type`,
				type: 'type',
				direction: 'none',
				onRender: (value, onSetValue, label) => {
					return <Input type='select' label={label} value={value} defaultValue={value} onChange={e => {
						onSetValue(e.target.value);
					}}>
						<option value=''>Select one..</option>
						<option value='text'>Text</option>
						<option value='number'>Number</option>
						<option value='decimal'>Decimal number</option>
						<option value='bool'>Yes/ no</option>
					</Input>
				},
			}
		];
		
		var constantType = this.state.constantType;
		
		if(constantType){
			
			this.setType(constantType);
			
			fields.push({
				type: constantType,
				direction: 'out',
				key: 'output',
				name: `Output`,
				onRender: (value, onSetValue, label) => {
					return <Input type={constantType} label={label} value={value} defaultValue={value} onChange={e => {
						if(constantType == 'bool'){
							onSetValue(e.target.checked);
						}else{
							onSetValue(e.target.value);
						}
					}}/>;
				}
			});
			
		}
		
		return fields;
	}
	
}