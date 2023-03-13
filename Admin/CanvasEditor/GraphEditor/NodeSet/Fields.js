import GraphNode from 'Admin/CanvasEditor/GraphEditor/GraphNode';
import Input from 'UI/Input';
import getAutoForm from 'Admin/Functions/GetAutoForm';

/*
* Defines the admin UI handler for the Fields graph node.
* This is separate from the graph node executor which exists on the frontend.
*/
export default class Fields extends GraphNode {
	
	constructor(props){
		super(props);
	}
	
	async validateState() {
		var { object } = this.state;
		
		if(!object){
			return;
		}
		
		var inputType = this.getInputType(object);
		
		if(inputType.name){
			inputType = inputType.name;
		}
		
		if(inputType == this.iType){
			// Warn: Won't match for array inputs.
			return;
		}
		
		this.iType = inputType;
		var fullContentTypeInfo = await getAutoForm('content', inputType);
		this.contentTypeFields = fullContentTypeInfo.form.fields;
	}
	
	renderFields() {
		var object = this.state.object;
		var inputType = object ? this.getInputType(object) : null;
		
		var fields = [{
				key: 'object',
				name: 'Object',
				type: inputType || 'object',
			}
		];
		
		inputType && this.setType(inputType);
		
		if(this.contentTypeFields){
			
			// For each field in it..
			this.contentTypeFields.forEach(fieldInfo => {
				
				var type = fieldInfo.includable ? fieldInfo.valueType : fieldInfo.data.type;
				
				if(fieldInfo.includable){
					if(fieldInfo.valueType.indexOf('[]') != -1){
						var elementType = fieldInfo.valueType.substring(0, fieldInfo.valueType.length - 2);
						
						type = {name: 'array', elementType};
					}else{
						type = fieldInfo.valueType.toLowerCase();
					}
				}
				
				fields.push({
					key: fieldInfo.data.name,
					type,
					name: fieldInfo.data.label,
					direction: 'out'
				});
				
			});
			
		}
		
		
		return fields;
	}
	
}