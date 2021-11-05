function getParam(param) {

	try {
		// example.com/products?myparam=1
		var url = new URL(window.location.href);
		var val = url && url.searchParams ? url.searchParams.get(param) : undefined;

		// check if we're using hash routing (e.g. within a Cordova app)
		// /apps/com.example/asdasdj898u8345/index.html?myparam=1#/products
		if (!val) {
			var hash = window.location.hash && window.location.hash.length ? new URL(window.location.hash, window.location.origin) : undefined;
			val = hash && hash.searchParams ? hash.searchParams.get(param) : undefined;
		}
	}
	catch {
		val = undefined;
	}

	return val;
}

export {
	getParam
};