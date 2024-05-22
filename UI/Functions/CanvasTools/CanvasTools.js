/**
 * getPlainText
 * returns plain text representation of a canvas object
 * 
 * @param {any} content - expects a canvas JSON object or string representation thereof
 * @returns {string}
 */
function getPlainText(content) {
	let validJson = getJson(content);

	if (!validJson) {
		return null;
	}

	let json = getJson(content);

	if (!json) {
		return null;
	}

	return getNodeText(json, true);
}

/**
 * getJson
 * @param {any} content
 * @returns {JSON}  (or null if invalid)
 */
function getJson(content) {

	if (!content) {
		return null;
	}

	// already an object - check for valid JSON
	if (typeof content === 'object') {
		let str, parsed;

		try {
			str = JSON.stringify(content);
			parsed = JSON.parse(str);
			return parsed === content ? content : null;
		} catch {
			return defaultValue;
		}

	}

	if (typeof content !== 'string') {
		return null;
	}

	let json;
	let input = content.trim();

	if (!input.startsWith("{") || !input.endsWith("}")) {
		return defaultValue;
	}

	try {
		json = JSON.parse(content);
	} catch {
		return null;
	}

	return json;
}

/**
 * isJson
 * @param {any} content
 * @returns {bool} true if content represents valid JSON
 */
function isJson(content) {

	if (!content) {
		return false;
	}

	// already an object - check for valid JSON
	if (typeof content === 'object') {
		let str, parsed;

		try {
			str = JSON.stringify(content);
			parsed = JSON.parse(str);
			return parsed === content;
		} catch {
			return false;
		}

	}

	if (typeof content !== 'string') {
		return false;
	}

	let input = content.trim();

	if (!input.startsWith("{") || !input.endsWith("}")) {
		return false;
	}

	try {
		JSON.parse(content);
	} catch {
		return false;
	}

	return true;
}

/**
 * getNodeText
 * @param {any} node canvas node
 * @param {bool} isLast if node is the last item in the current group (e.g. last li within an unordered list)
 * @returns {string}
 */
function getNodeText(node, isLast) {

	if (!node) {
		return '';
	}

	let target = typeof node == 'object' ? node.c : node;

	switch (typeof target) {

		case 'object':

			if (Array.isArray(node.c)) {
				let arrayText = (node.t == 'ul' || node.t == 'ol' ? ' ' : '');

				node.c.forEach((subNode, i) => {
					let isLast = i == node.c.length - 1;
					arrayText += getNodeText(subNode, isLast);
				});

				return arrayText;
			} else {
				return getNodeText(node.c, true);
			}

		default:

			switch (node.t) {
				case 'li':
					if (target.trim().length && !isLast) {
						let last = target.trim().slice(-1);
						let validChars = ['.', ',', ';', '?', '!'];

						if (!validChars.includes(last)) {
							return target.trimEnd() + "; ";
						}

					}
					break;
			}

			return target;
	}

}

export {
	getPlainText,
	isJson
}
