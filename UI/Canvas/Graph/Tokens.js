import Executor from 'UI/Functions/GraphRuntime/Executor';
import {handleString} from 'UI/Token';


export default class Tokens extends Executor {
	
	constructor(props){
		super(props);
	}
	
	run(field) {
		var props = {};
		
		for(var k in this.state){
			this.readValue(k, props);
		}
		
		return this.onValuesReady(props, () => {
			var content = props.content;
			var str = props.text;
			var res;
			
			if(content){
				// Classic useTokens technique here. Session ones - the empty object - are not available however (for now!)
				// content is available via the content resolver, i.e. that's ${content.X}
				res = handleString(str, {}, content);
			}else{
				res = (str || '').toString().replace(/\$\{(\w|\.)+\}/g, function (textToken) {
					var inputName = textToken.substring(2, textToken.length - 1);
					return props[inputName];
				});
			}
			
			this.outputs[field] = res;
			return res;
		});
	}
	
}