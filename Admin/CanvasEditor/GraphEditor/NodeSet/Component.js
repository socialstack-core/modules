import GraphNode from '../GraphNode';
import ComponentSelect from '../ComponentSelect';
import {niceName} from '../Utils';

/*
* Defines the admin UI handler for the Component graph node.
* This is separate from the graph node executor which exists on the frontend.
*/
export default class Component extends GraphNode {
	
	constructor(props){
		super(props);
	}
	
	getTypeColor(){
		return [0.15, 1, 0.5];
	}
	
	renderFields() {
		
		var fields = [
			{
				key: 'componentType',
				name: `Name`,
				type: 'type',
				direction: 'none',
				onRender: (value, onSetValue, label) => {
					return <ComponentSelect value={value} label={label} onChange={e => {
						var typeName = e.target.value;
						onSetValue(typeName);
					}} />
				},
			}
		];
		
		var componentType = this.state.componentType;
		
		if(componentType){
			// We've got an input on the name field.
			var propTypes = require(componentType).default.propTypes;
			
			for(var propKey in propTypes){
				
				var propInfo = propTypes[propKey];
				var type = propInfo;
				
				if(Array.isArray(propInfo) && propInfo.length){
					
					// A dropdown menu to select an entity of one of the specified types.
					var first = propInfo[0];
					
					if(first && first.value !== undefined){
						first = first.value;
					}
					
					if(typeof first === 'string'){
						type = 'string';
					}else if(typeof first === 'number'){
						type = 'decimal';
					}else{
						console.log("Unknown array type value: ", propInfo);
						continue;
					}
					
				}else if(typeof type !== 'string'){
					if(type.type != 'array' && type.type != 'list'){
						continue;
					}
				}
				
				fields.push({
					key: propKey,
					type,
					name: niceName(propKey)
					// direction: in - is implied
				});
				
			}
			
			fields.push({
				type: 'component',
				direction: 'out',
				key: 'output',
				name: `Output`
			});
			
		}
		
		return fields;
	}
	
}