import Executor from 'UI/Functions/GraphRuntime/Executor';
import webRequest from 'UI/Functions/WebRequest';


export default class Content extends Executor {
	
	constructor(){
		super();
	}
	
	reset(){
		this.comp = null;
	}
	
	async go() {
		// Ensure the content is loaded then return it and all its fields.
		
		if(!this.content){
			var contentType = await this.state.contentType.run();
			
			var content;
			
			if(contentType == 'primary'){
				// Get the primary object for the current context.
				if(this.context && this.context.pageState && this.context.pageState.po){
					
					// Get the primary object:
					var po = this.context.pageState.po; // type and id.
					
					if(po.type && po.id){
						var response = await webRequest(po.type + '/' + po.id);
						content = response.json;
					}else{
						content = null;
					}
					
				}else{
					content = null;
				}
				
			}else{
				
				// Get the content ID:
				var contentId = await this.state.contentId.run();
				
				// Get the content:
				var response = await webRequest(contentType + '/' + contentId);
				content = response.json;
			}
			
			this.content = content;
			
			// Also expose all fields of the object into the outputs.
			if(content){
				for(var k in content){
					if(k == 'output'){
						continue;
					}
					this.outputs[k] = content[k];
				}
			}
		}
		
		return this.content;
	}
	
}