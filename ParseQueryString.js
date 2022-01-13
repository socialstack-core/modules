/**
 * Breaks a querystring into its key/value pairs.
 * @param str
 */
export default function queryString(str) {
	if (!str || !str.length) {
		return {};
	}

	if (str[0] == '?') {
		str = str.substring(1);
	}

	var pieces = str.split('&');
	var result = {};
	for (var i = 0; i < pieces.length; i++) {
		var entry = pieces[i].split('=');
		result[entry[0]] = decodeURI(entry[1]);
	}

	return result;
}
