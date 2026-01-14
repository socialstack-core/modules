/**
 * A simple debounce helper. It reduces spammed calls to a maximum call frequency.
 * For example, if someone types quickly in a search bar which live searches you would get a lot of requests.
 * Debouncing it reduces that to a max of 4 requests/sec by default (at a delay of 250ms).
 */
export default class Debounce<T> {

	/**
	 * The delay in ms that the callback will run at (at most).
	 */
	delay: number;

	/**
	 * Internal timer.
	 */
	timer: number;

	/**
	 * The callback function.
	 */
	onRun: (item: T) => void;

	/**
	 * Creates a new debounce helper.
	 * @param func The callback to run.
	 * @param delay The max wait time in milliseconds. If not specified then 250ms is used.
	 */
	constructor(func: (item: T) => void, delay? : number) {
		this.onRun = func;
		this.delay = delay || 250;
		this.timer = 0;
	}

	/**
	 * Spam this! You can optionally provide args. The majority of the args will be 
	 * dropped and only the absolute latest will be passed to the callback.
	 * @param args
	 */
	handle(args : T){
		this.timer && clearTimeout(this.timer);

		this.timer = setTimeout(() => {
			 this.onRun(args);
		}, this.delay);
	}
}