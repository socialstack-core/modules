export default class Debounce{
	constructor(func, delay){
		this.onRun = func;
		this.delay = delay || 250;
	}
	
	handle(args){
		this.timer && clearTimeout(this.timer);
		this.timer = setTimeout(() => {
			 this.onRun(args);
		}, this.delay);
	}
}