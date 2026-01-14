import GraphNode from '../GraphNode';
import ComponentSelect from '../ComponentSelect';
import {niceName} from '../Utils';
import {
	isJsxType, getConstantUnionType, getAsContentType,
	getArrayElementType,
	isNumericType, isBooleanType, isRefType
} from 'Admin/Functions/GetPropTypes';

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
	
	getNodeType(codeModuleType, codeModuleMeta, fieldName){
		// Is it an array?
		var arrayEle = getArrayElementType(codeModuleType);
		
		if(arrayEle){
			return {elementType: this.getNodeType(arrayEle, codeModuleMeta, fieldName)};
		}
		
		var constantUnion = getConstantUnionType(codeModuleType, codeModuleMeta);
		var contentTypeName = getAsContentType(codeModuleType, codeModuleMeta);
		var type = '';
		
		if(isJsxType(codeModuleType)){
			type = 'jsx';
		}else if (fieldName?.endsWith("Ref") || isRefType(codeModuleType)){
			type = 'file';
		} else if (constantUnion){
			var first = constantUnion[0];
			
			// Are they numbers?
			if(!isNaN(parseFloat(first))){
				type='decimal';
			}else{
				type='string';
			}
			
		} else if (contentTypeName){
			type = contentTypeName;
		}else if(isNumericType(codeModuleType)){
			type = 'number';
		} else if (isBooleanType(codeModuleType)) {
			type = 'boolean';
		}else{
			type = 'string';
		}
		
		return type;
	}
	
	collectPropTypesInto(propTypes, codeModuleMeta, fields){
		
		for(var propKey in propTypes){
			var propType = propTypes[propKey];
			var type = this.getNodeType(propType.type, codeModuleMeta, propKey);
			/*
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
			*/
			
			var label = propKey;
			
			if(type == 'file' && propKey.endsWith('Ref')){
				label = label.substring(0, label.length-3);
			}
			
			fields.push({
				key: label,
				type,
				name: niceName(propKey)
				// direction: in - is implied
			});
			
		}
		
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
			var fullPtSet = this.graph?.metadata?.propTypes?.codeModules;
			
			if(fullPtSet){
				const codeModule = fullPtSet[componentType];
				const pt = codeModule?.propTypes;
				if(pt){
					this.collectPropTypesInto(pt, codeModule, fields);
				}
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