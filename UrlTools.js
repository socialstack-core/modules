function getParam(param) {
	// hitachivantara.virtualrack.com/products?category=1
	var url = new URL(window.location.href);
	var val = url && url.searchParams ? url.searchParams.get(param) : undefined;

	// check if we're using hash routing (e.g. within a Cordova app)
	// /apps/com.hitachivantara.virtualrack/asdasdj898u8345/index.html?category=1#/products
	if (!val) {
		var hash = window.location.hash && window.location.hash.length ? new URL(window.location.hash) : undefined;
		val = hash && hash.searchParams ? hash.searchParams.get(param) : undefined;
	}

	return val;
}

export {
	getParam
};