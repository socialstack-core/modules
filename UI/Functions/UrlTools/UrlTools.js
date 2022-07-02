function getParam(param) {
	// example.com/products?myparam=1
	var url = new URL(window.location.href);
	var val = url && url.searchParams ? url.searchParams.get(param) : undefined;

	// check if we're using hash routing (e.g. within a Cordova app)
	// e.g. file:///android_asset/www/index.en.html#/products?myparam=1
	if (!val) {
		var urlParts = window.location.href.split("?");

		if (urlParts.length > 1) {
			var searchParams = urlParts[urlParts.length - 1];

			// strip any appended hash
			// e.g. myparam=1#/products
			var hashCheck = searchParams.split("#");

			if (hashCheck.length > 1) {
				searchParams = hashCheck[0];
			}

			// split into individual params
			searchParams = searchParams.split("&");

			// split each param into key/value pair
			for (var i = 0; i < searchParams.length; i++) {
				var kv = searchParams[i].split("=");

				if (kv.length == 2 && kv[0] == param) {
					val = kv[1];
				}

			}

		}

	}

	return val;
}

export {
	getParam
};