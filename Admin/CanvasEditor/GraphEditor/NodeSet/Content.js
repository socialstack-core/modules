import GraphNode from '../GraphNode';
import Input from 'UI/Input';
import ContentTypeSelect from '../ContentTypeSelect';
import getAutoForm, {getAllContentTypes} from 'Admin/Functions/GetAutoForm';


/*
* Defines the admin UI handler for the Content graph node.
* This is separate from the graph node executor which exists on the frontend.
*/
export default class Content extends GraphNode {
	
	constructor(props){
		super(props);
	}
	
	async validateState() {
		var { contentType } = this.state;
		
		if(!contentType || contentType == this.cTypeFieldsFor){
			this.updateIncludes();
			return;
		}
		
		this.cTypeFieldsFor = contentType;
		var fullContentTypeInfo = await getAutoForm('content', contentType == 'primary' ? this.context && this.context.primary : contentType);
		this.contentTypeFields = fullContentTypeInfo.form.fields;
		
		this.updateIncludes();
	}
	
	updateIncludes() {
		delete this.state.includes;
		
		// If any output fields are connected which are includable, include them now.
		// Note: will have to factor in include usage propagating 
		// backwards through Loop, but that's a later issue!
		var incl = null;
		
		this.contentTypeFields && this.contentTypeFields.forEach(fieldInfo => {
				
				if(fieldInfo.includable){
					// Connected?
					var fieldName = fieldInfo.data.name;
					
					if(this.isOutputConnected(fieldName)){
						// Yes, the field is in use.
						if(!incl){
							incl = [];
						}
						
						incl.push(fieldName);
					}
				}
				
				
		});
		
		this.state.includes = incl;
	}
	
	renderFields() {
		
		var primaryType = (this.context && this.context.primary);
		
		var fields = [
			{
				key: 'contentType',
				name: `Content Type`,
				type: 'contentType',
				onRender: (value, onSetValue, label) => {
					return <ContentTypeSelect label={label} value={value} onChange={e => {
						var typeName = e.target.value;
						onSetValue(typeName);
					}} primaryType={primaryType} />
				},
				direction: 'none'
			}
		];
		
		var cType = this.state.contentType;
		
		if(cType != 'primary'){
			fields.push({
				key: 'contentId',
				name: `Content ID`,
				type: 'int',
				onRender: (value, onSetValue, label) => {
					return <Input type={'number'} label={label} value={value !==null && value!==undefined && !(value.link && value.node && value.field) ? value : undefined} onChange={e => {
						onSetValue(e.target.value);
					}} onKeyUp={e => {
						onSetValue(e.target.value);
					}}/>;
				}
			});
		}else{
			// The primary type has been selected - set it to that:
			cType = primaryType;
		}
		
		if(cType){
			// We've got a content type selected. 
			
			this.setType(cType);
			
			fields.push({
				key: 'output',
				type: cType,
				name: 'output',
				direction: 'out'
			});
			
			// For each field in it..
			this.contentTypeFields && this.contentTypeFields.forEach(fieldInfo => {
				
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