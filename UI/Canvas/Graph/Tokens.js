import Executor from 'UI/Functions/GraphRuntime/Executor';

export default class Tokens extends Executor {
	
	constructor(props){
		super(props);
	}
	
	async run(field) {
		
		var inputs = {};
		
		for(var k in this.state){
			inputs[k] = await this.state[k].run();
		}
		
		var str = inputs.text;
		
		var res = (str || '').toString().replace(/\$\{(\w|\.)+\}/g, function (textToken) {
			var inputName = textToken.substring(2, textToken.length - 1);
			return inputs[inputName];
		});
		
		this.outputs[field] = res;
		return res;
	}
	
}