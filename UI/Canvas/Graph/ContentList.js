import Executor from 'UI/Functions/GraphRuntime/Executor';
import webRequest from 'UI/Functions/WebRequest';


export default class ContentList extends Executor {
	
	constructor(){
		super();
	}
	
	reset(){
		this.content = null;
	}
	
	countArgs(query){
		var count = 0;
		for(var i=0; i<query.length;i++){
			if('?' === query[i]){
				count++;
			}
		}
		return count;
	}
	
	async go() {
		// Make a webRequest and return the results.
		if(!this.content){
			
			var filter = null;
			var includes = null;
			
			if(this.state.includes){
				includes = await this.state.includes.run();
			}
			
			if(this.state.filter){
				var query = await this.state.filter.run();
				
				if(query && typeof query === 'string'){
					// How many args does it have? this is simply the number of question marks.
					var argCount = this.countArgs(query);
					
					var args = [];
					args.length = argCount;
					
					for(var i=0;i<argCount;i++){
						var arg = this.state['arg' + i];
						args[i] = arg ? await arg.run() : null;
					}
					
					// Set the filter:
					filter = {query, args};
				}
				
			}
			
			var contentType = await this.state.contentType.run();
			var response = await webRequest(contentType + '/list', filter, {includes});
			this.content = response.json.results;
			
		}
		
		return this.content;
	}
	
}