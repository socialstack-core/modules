import Executor from 'UI/Functions/GraphRuntime/Executor';
import webRequest from 'UI/Functions/WebRequest';


export default class ContentList extends Executor {

	constructor() {
		super();
	}

	reset() {
		this.content = null;
	}

	countArgs(query) {
		var count = 0;
		for (var i = 0; i < query.length; i++) {
			if ('?' === query[i]) {
				count++;
			}
		}
		return count;
	}

	go() {
		// Make a webRequest and return the results.
		if (!this.content) {
			var props = {};
			
			this.readValue('contentType', props);
			this.readValue('includes', props);
			this.readValue('filter', props);
			
			if(this.state.filter){
				for(var k in this.state){
					if(k.indexOf('arg') === 0){
						this.readValue(k, props);
					}
				}
			}
			
			return this.onValuesReady(props, () => {
				var {includes, filter, contentType} = props;
				
				var filt = null;
				
				if(filter && typeof filter == 'string'){
					// How many args does it have? this is simply the number of question marks.
					var argCount = this.countArgs(query);
					
					var args = [];
					args.length = argCount;
					
					for (var i = 0; i < argCount; i++) {
						args[i] = props['arg' + i];
					}
					
					// Set the filter:
					filt = { query, args };
				}
				
				var context = this.context;
				var cache = context && context.cache;
				
				var p = cache ?
					cache.get(contentType, 'list', filt, includes) :
					webRequest(contentType + '/list', filt, { includes });
				
				return p.then(response => {
					this.content = response.json.results;
					return this.content;
				});
				
			});
		}

		return this.content;
	}

}