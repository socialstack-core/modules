import Executor from 'UI/Functions/GraphRuntime/Executor';

export default class ToList extends Executor {
	
	constructor(){
		super();
		this.state = [];
	}
	
	go() {
		var output = [];
		output.length = this.state.length;
		var proms = null;
		
		for(var i=0;i<this.state.length;i++){
			var prop = this.state[i];
			
			if(prop){
				var val = prop.run();
				
				if(val && val.then){
					if(!proms){
						proms = [];
					}
					
					((arrInd) => {
						proms.push(val.then((v) => {
							output[arrInd] = v;
						}));
					})(i);
					
				}else{
					output[i] = val;
				}
			}
		}
		
		return proms ? Promise.all(proms).then(() => output) : output;
	}
	
}