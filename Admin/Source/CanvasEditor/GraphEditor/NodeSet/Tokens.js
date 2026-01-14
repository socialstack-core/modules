import GraphNode from '../GraphNode';
import ContentTypeSelect from '../ContentTypeSelect';

/*
* Defines the admin UI handler for the Tokens graph node.
* This is separate from the graph node executor which exists on the frontend.
*/
export default class Tokens extends GraphNode {
	
	constructor(props){
		super(props);
	}
	
	getTokens(str){
		return (str || '').toString().match(/\$\{(\w|\.)+\}/g);
	}
	
	renderFields() {
		
		var content = this.state.content;
		var contentType = content ? this.getInputType(content) : null;
		
		var fields = [{
			key: 'text',
			type: 'text',
			name: `Text`
		},
		{
			key: 'content',
			type: contentType || 'object',
			name: `Content`
		}];
		
		var outputType = 'string';
		
		this.setType(outputType);
		
		var text = this.resolveValue(this.state.text);
		
		if(text && typeof text == 'string' && !content){
			var argSet = this.getTokens(text);
			
			if(argSet && argSet.length){
				for(var i=0;i<argSet.length;i++){
					var n = argSet[i];
					var name = n.substring(2, n.length - 1);
					fields.push({
						key: name,
						type: 'object',
						name: name
					});
				}
			}
		}
		
		fields.push({
			key: 'output',
			type: outputType,
			name: `Output`,
			direction: 'out'
		});
		
		return fields;
	}
	
}