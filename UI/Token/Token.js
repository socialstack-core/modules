import Content from 'UI/Content';
import { useSession, useRouter } from 'UI/Session';
import { useContent } from 'UI/Content';

var modes = { 'content': 1, 'session': 1, 'url': 1, 'customdata': 1, 'primary': 1, 'theme': 1 };

export function TokenResolver(props) {
	return props.children(useTokens(props.value));
}

export function useTokens(str) {
	var { session } = useSession();
	var localContent = useContent();
	var { pageState } = useRouter();

	return handleString(str, session, localContent, pageState);
}

function handleString(str, session, localContent, pageState) {
	return (str || '').toString().replace(/\$\{(\w|\.)+\}/g, function (textToken) {
		var fields = textToken.substring(2, textToken.length - 1).split('.');

		var mode = '';
		var first = fields[0].toLowerCase();
		if (modes[first]) {
			fields.shift();
			mode = first;
		}

		return resolveValue(mode, fields, session, localContent, pageState);
	});
}

export function resolveValue(mode, fields, session, localContent, pageState) {
	var token;

	if (mode) {
		mode = mode.toLowerCase();
	}

	if (mode == "content") {
		token = localContent ? localContent.content : null;
	} else if (mode == "url") {
		if (!pageState || !pageState.tokenNames) {
			return '';
		}
		var index = pageState.tokenNames.indexOf(fields.join('.'));
		return (index == null || index == -1) ? '' : pageState.tokens[index];
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
}

/*
* Contextual token. 
* Available values either come from the primary type on the page, or the global state. The RTE establishes the options though.
*/

export default function Token(props) {
	// If editor, display the thing and its children:
	var { session } = useSession();
	var localContent = useContent();
	var { pageState } = useRouter();

	if (props._rte) {
		return <span className="context-token" ref={props.rootRef}>
			{props.children}
		</span>;
	}
	
	if (!props.mode) {
		// Resolve from child string if there is one.
		var str = props.s || props.children;
	
		if(Array.isArray(str)){
			str = str.length ? str[0] : null;
		}
		
		if (typeof str == 'string') {
			return handleString(str.indexOf('$') == -1 ? '${' + str + '}' : str, session, localContent, pageState);
		}

		return '{Incorrect token, see wiki}';
	}

	// Resolved value. No wrapper - just plain value.
	return resolveValue(props.mode, props.fields, session, localContent, pageState);
}

Token.editable = {
	inline: true,
	onLoad: nodeInfo => {
		// Convert mode and fields to children root
		const data = nodeInfo.d || {};

		if (data && data.mode) {
			let str = data.mode || '';
			let fieldStr = data.fields ? data.fields.join('.') : '';

			if (str && fieldStr) {
				str += '.' + fieldStr;
			} else {
				str += fieldStr;
			}

			if (!str) {
				str = 'unnamed token';
			}

			nodeInfo.r = {
				children: { s: str }
			};
		}
	},
	onSave: nodeInfo => {
		// Ensure children root is pure text:
		var childRoot;

		if (nodeInfo.r && nodeInfo.r.children) {
			childRoot = nodeInfo.r.children;
		} else if (nodeInfo.c) {
			childRoot = nodeInfo.c;
		}

		if (childRoot) {
			if (Array.isArray(childRoot)) {
				childRoot = childRoot.length ? childRoot[0] : null;
			}

			if (childRoot.s) {
				childRoot = childRoot.s;
			}
		}

		if (childRoot && typeof childRoot === 'string') {
			// good! The root is a pure text node.
			var pieces = childRoot.split('.');
			var first = pieces[0].trim();

			nodeInfo.d = {};

			if (modes[first]) {
				nodeInfo.d.mode = first;
				pieces.shift();
			}

			for (var i = 0; i < pieces.length; i++) {
				pieces[i] = pieces[i].trim();
			}

			nodeInfo.d.fields = pieces;
		}
	}
};

Token.propTypes = {
	children: 'jsx'
};
