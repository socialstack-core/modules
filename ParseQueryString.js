/**
 * Breaks a querystring into its key/value pairs.
 * @param queryString
 * @param defaultParams
 */
export default function parseQueryString(queryString = null, defaultParams = {}) {
	if (!queryString || !queryString.length) {
		queryString = window.location.search.replace("?", '')
	}

	if (queryString[0] == '?') {
		queryString = queryString.substring(1);
	}

	var pieces = queryString.split('&');
	var result = defaultParams;
	for (var i = 0; i < pieces.length; i++) {
		var entry = pieces[i].split('=');
		result[entry[0]] = decodeURI(entry[1]);
	}

	return result;
}
