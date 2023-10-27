import GraphNode from 'Admin/CanvasEditor/GraphEditor/GraphNode';
import Input from 'UI/Input';

/*
* Defines the admin UI handler for the If graph node.
* This is separate from the graph node executor which exists on the frontend.
*/
export default class If extends GraphNode {
	
	constructor(props){
		super(props);
	}
	
	renderFields() {
		var input = this.state.input;
		var inputType = input ? this.getInputType(input) : null;
		
		var fields = [
			{
				key: 'operanda',
				name: `Operand A`,
				type: inputType || 'number'
			},
			{
				key: 'operandb',
				name: `Operand B`,
				type: inputType || 'number'
			},
			{
				key: 'operandType',
				name: `Operation`,
				type: 'type',
				direction: 'none',
				onRender: (value, onSetValue, label) => {
					return <Input type='select' label={label} value={value} defaultValue={value} onChange={e => {
						onSetValue(e.target.value);
					}}>
						<option value=''>Select one..</option>
						<option value='lessThan'>A &lt; B</option>
						<option value='moreThan'>A &gt; B</option>
						<option value='lessThanEqual'>A &lt;= B</option>
						<option value='moreThanEqual'>A &gt;= B</option>
						<option value='equalTo'>A = B</option>
					</Input>
				},
			},

		];

		var outputType = 'bool';
		this.setType(outputType);

		fields.push({
			key: 'output',
			type: outputType,
			name: 'output',
			direction: 'out'
		});

		return fields;
	}
	
}