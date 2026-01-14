import { PageState } from 'UI/Router';

export interface TokenOptions {
	/**
	 * Optional content object (can be anything).
	 */
	content?: any;
}

/**
 * Resolves a singular token name such as "url.urlTokenName" to its value.
 * @param tokenName
 * @param session
 * @param localContent
 * @param pageState
 */
export function resolveSingular(tokenName: string, session: Session, localContent?: any, pageState?: PageState) {
	if (!tokenName) {
		return undefined;
	}

	const fields = tokenName.split('.');
	const mode = fields.shift()!.toLowerCase();

	return resolveValue(mode, fields, session, localContent, pageState);
}

export function resolveValue(
	mode: string,
	fields: string[],
	session: Session,
	localContent?: any,
	pageState?: PageState,
	opts?: TokenOptions
): string {
	var token;

	if (mode) {
		mode = mode.toLowerCase();
	}

	if (mode == "content" || mode == "context") {
		token = localContent;
	} else if (mode == "url") {
		if (!pageState || !pageState.tokenNames) {
			return '';
		}
		var index = pageState.tokenNames.indexOf(fields.join('.'));
		return (index == null || index == -1) ? '' : (pageState.tokens ? pageState.tokens[index] : '');
	} else if (mode == "theme") {
		return 'var(--' + fields.join('-') + ')';
	} else if (mode == "customdata" || mode == "primary") {
		// Used by emails mostly. Passes through via primary object.
		if (!pageState || !pageState.po) {
			return '';
		}
		token = pageState.po;
	} else {
		token = session;
	}

	if (!token) {
		return '';
	}

	var fields = fields;

	if (Array.isArray(fields) && fields.length) {
		try {
			for (var i = 0; i < fields.length; i++) {
				token = token[fields[i]];
				if (token === undefined || token === null) {
					return '';
				}
			}
		} catch (e) {
			console.log(e);
			token = null;
		}

		return token;
	} else if (typeof fields == 'string') {
		return token[fields];
	}

	return '';
}
