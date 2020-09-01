/*
* ToastInfo will be passed directly to your ToastList render function.
* toastInfo.duration defines how long it's visible for (don't set it at all if you want it to be explicitly closed by the user).
*/

function pop (toastInfo) {
	
	var {toasts} = global.app.state;
	
	if(!toasts){
		toasts = [toastInfo];
	}else{
		toasts.push(toastInfo);
	}
	
	global.app.setState({toasts});
	
	if(toastInfo.duration){
		setTimeout(() => {
			close(toastInfo);
		}, toastInfo.duration * 1000);
	}
};

function close(toastInfo) {
	var {toasts} = global.app.state;
	toasts = toasts.filter(toast => toast != toastInfo);
	global.app.setState({toasts});
}

module.exports = {
		pop,
		close
}