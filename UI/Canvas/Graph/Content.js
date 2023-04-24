import Executor from 'UI/Functions/GraphRuntime/Executor';
import webRequest from 'UI/Functions/WebRequest';
import CN from 'UI/Content';

export default class Content extends Executor {

	constructor() {
		super();
	}

	reset() {
		this.comp = null;
	}
	
	go() {
		// Ensure the content is loaded then return it and all its fields.

		if (this.content) {
			return this.content;
		}
		
		var props = {};
		
		this.readValue('contentType', props);
		this.readValue('includes', props);
		this.readValue('contentId', props);
		
		return this.onValuesReady(props, () => {
			var content;
			var context = this.context;
			
			var {contentType, includes, contentId} = props;
			
			var set = (content) => {
				this.content = content;
			
				// Also expose all fields of the object into the outputs.
				if (content) {
					for (var k in content) {
						if (k == 'output') {
							continue;
						}
						this.outputs[k] = content[k];
					}
				}
				
				return content;
			};
			
			var get = (type, id) => {
				var cache = context && context.cache;
				return cache ?
					cache.get(type, id, null, includes) :
					webRequest(type + '/' + id, null, { includes });
			};
			
			if (contentType == 'primary') {
				// Get the primary object for the current context.
				var po = context && context.pageState && context.pageState.po;
				if (!po || !po.type || !po.id) {
					return set(null);
				}
				
				contentType = po.type;
				contentId = po.id;
			}
			
			// Get the content:
			var prom = get(contentType, contentId);
			
			if(prom && prom.then){
				return prom.then(response => set(response.json));
			}else{
				// An instant response from the SSR cache
				return set(prom);
			}
		});
	}

}