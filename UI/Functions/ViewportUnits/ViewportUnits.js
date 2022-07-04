function throttle (callback, limit) {
    var waiting = false;
    return function () {
        if (!waiting) {
            callback.apply(this, arguments);
            waiting = true;
            setTimeout(function () {
                waiting = false;
            }, limit);
        }
    }
}

function updateViewportVars() {
	let vh = window.innerHeight * 0.01;
	let vw = window.innerWidth * 0.01;
	let vmin, vmax;
	
	if (window.innerHeight > window.innerWidth) {
		vmin = vw;
		vmax = vh;
	} else {
		vmin = vh;
		vmax = vw;
	}

	var docStyle = document.documentElement.style;

	docStyle.setProperty('--vh', `${vh}px`);
	docStyle.setProperty('--vw', `${vw}px`);
	docStyle.setProperty('--vmin', `${vmin}px`);
	docStyle.setProperty('--vmax', `${vmax}px`);
}

function reoriented() {
	// catches odd window.innerWidth/innerHeight values that can sometimes appear when re-orienting
	// (at least via chrome devtools)
	setTimeout(updateViewportVars, 50);
}

document.addEventListener("DOMContentLoaded", function() {
	updateViewportVars();
	window.addEventListener('resize', throttle(updateViewportVars, 100));

	if (screen.orientation) {
		screen.orientation.addEventListener('change', reoriented);
	}
  
});
