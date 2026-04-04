// Handles conversion between Socialstack Canvas JSON and TinyMCE HTML

function escapeHtml(str: string): string {
	return str
		.replace(/&/g, '&amp;')
		.replace(/</g, '&lt;')
		.replace(/>/g, '&gt;')
		.replace(/"/g, '&quot;')
		.replace(/'/g, '&#039;');
}

export function canvasJsonToHtml(json: any): string {
	if (!json) {
		return '';
	}

	if (typeof json === 'string') {
		try {
			json = JSON.parse(json);
		} catch (e) {
			return json; // If it's just a regular string, return it
		}
	}

	let html = '';

	// It's a single component/tag
	if (json.t) {
		return renderNodeToHtml(json);
	}

	// It's a root array (content key)
	if (json.c && Array.isArray(json.c)) {
		json.c.forEach((node: any) => {
			html += renderNodeToHtml(node);
		});
		return html;
	}

	// It's a wrapped component (c key) - e.g. {"c": {"t": "Admin/Template", ...}}
	if (json.c && typeof json.c === 'object') {
		return renderNodeToHtml(json.c);
	}

	return '';
}

function escapeJsonForAttribute(jsonString: string) {
	return jsonString.replace(/[&<>"']/g, (m: string) => {
		return ({
			'&': '&amp;',
			'<': '&lt;',
			'>': '&gt;',
			'"': '&quot;',
			"'": '&#39;'
		}[m]) as string;
	});
}

function renderNodeToHtml(node: any): string {
	if (typeof node === 'string') {
		return node;
	}

	if (!node.t && node.s === undefined) {

		if (node.c) {
			// Just a wrapper node, usually for an array.
			if (!Array.isArray(node.c)) {
				node.c = [node.c];
			}

			let result = '';
			node.c.map((child: any) => {
				result += renderNodeToHtml(child);
			});

			return result;
		}

		return '';
	}

	// Handle "s" (string content) - render as <p> for editability
	if (node.s !== undefined) {
		return `<p>${escapeHtml(node.s)}</p>`;
	}

	const isReactComponent = node.t.includes('/') || node.t[0] === node.t[0].toUpperCase();

	// Handle c as alias for r.children
	if (node.c) {
		if (node.r) {
			node.r.children = node.c;
		} else {
			node.r = { children: node.c };
		}
	}

	if (isReactComponent) {
		const propsString = node.d ? escapeJsonForAttribute(JSON.stringify(node.d)) : '';
		let rootsString = '';

		if (node.r) {
			for (var rootKey in node.r) {
				rootsString += '<root-content data-name="' + rootKey + '">' + renderNodeToHtml(node.r[rootKey]) + '</root-content>';
			}
		}

		return '<react-component data-name="' + node.t + '" data-props="' + propsString + '" contenteditable="false">' + rootsString + '</react-component>';
	} else {
		// Standard HTML element
		let attribs = '';
		if (node.d) {
			for (const key in node.d) {
				if (key === 'className') {
					attribs += ` class="${node.d[key]}"`;
				} else {
					attribs += ` ${key}="${node.d[key]}"`;
				}
			}
		}

		let contentHtml = '';
		if (node.c) {
			if (Array.isArray(node.c)) {
				node.c.forEach((child: any) => {
					contentHtml += renderNodeToHtml(child);
				});
			} else {
				contentHtml += renderNodeToHtml(node.c);
			}
		}

		if (node.c) {
			return `<${node.t}${attribs}>${contentHtml}</${node.t}>`;
		} else {
			const selfClosingTags = ['img', 'br', 'hr', 'input', 'meta', 'link'];
			if (selfClosingTags.includes(node.t.toLowerCase())) {
				return `<${node.t}${attribs} />`;
			} else {
				return `<${node.t}${attribs}></${node.t}>`;
			}
		}
	}
}

export function htmlToCanvasJson(html: string): string {
	if (!html) {
		return JSON.stringify({});
	}

	const parser = new DOMParser();
	const doc = parser.parseFromString(html, 'text/html');
	const converted = rootSetToCanvas(doc.body);
	return converted ? JSON.stringify(converted) : '{}';
}

function rootSetToCanvas(el: Element) {
	const rootNodes: any[] = [];

	Array.from(el.childNodes).forEach(node => {
		const parsedNode = parseHtmlNode(node);
		if (parsedNode) {
			rootNodes.push(parsedNode);
		}
	});

	if (!rootNodes.length) {
		return null;
	}

	if (rootNodes.length == 1) {
		return rootNodes[0];
	}

	// It's a set
	return { c: rootNodes };
}

export function searchForContentRoots(el: Element, result: (el:HTMLElement, rootName: string) => void) {

	var children = el.childNodes;

	for (var i = 0; i < children.length; i++) {
		var child = children[i];

		if (child.nodeType != Node.ELEMENT_NODE) {
			continue;
		}

		const childEl = child as HTMLElement;
		const tagName = childEl.tagName.toLowerCase();

		if (tagName == 'root-content') {
			// Found one!
			const rootName = childEl.getAttribute('data-name');

			if (!rootName) {
				continue;
			}

			result(childEl, rootName);

		} else {
			// Search its children
			searchForContentRoots(childEl, result);
		}
	}

}

function parseHtmlNode(node: ChildNode, roots?: Record<string, any>): any {
	if (node.nodeType === Node.TEXT_NODE) {
		const text = node.textContent?.trim();
		// Skip whitespace-only text nodes
		return text ? { s: text } : null;
	}

	if (node.nodeType !== Node.ELEMENT_NODE) {
		return null;
	}

	const el = node as HTMLElement;
	const tagName = el.tagName.toLowerCase();

	if (tagName === 'react-component') {
		const componentName = el.getAttribute('data-name');
		const propsString = el.getAttribute('data-props');

		let data: any = null;

		if (propsString) {
			try {
				data = JSON.parse(propsString);
			} catch (e) {
				console.error("Failed to parse component props", e);
			}
		}

		const result: any = {
			t: componentName,
		};

		if (data && Object.keys(data).length > 0) {
			result.d = data;
		}

		// Next, search for roots which can be in any of its child nodes.
		let rootSet : any = {};
		searchForContentRoots(el, (childEl, rootName) => {
			const convertedContent = rootSetToCanvas(childEl);

			if (convertedContent) {
				// It has something to save.
				rootSet[rootName] = convertedContent;
			}
		});
		const rootCount = Object.keys(rootSet).length;
		if (rootCount > 0) {
			if (rootSet.children) {
				result.c = rootSet.children;

				if (rootCount == 1) {
					rootSet = null;
				} else {
					delete rootSet.children;
				}
			}

			if (rootSet) {
				result.r = rootSet;
			}
		}

		return result;
	} else {
		// Standard HTML element
		const result: any = {
			t: tagName
		};

		const data: any = {};
		let hasData = false;

		Array.from(el.attributes).forEach(attr => {
			if (attr.name === 'class') {
				data['className'] = attr.value;
				hasData = true;
			} else if (attr.name !== 'style' && !attr.name.startsWith('data-mce-')) {
				// ignoring TinyMCE specific attributes
				data[attr.name] = attr.value;
				hasData = true;
			}
		});

		if (hasData) {
			result.d = data;
		}

		if (el.childNodes.length > 0) {
			const children: any[] = [];
			Array.from(el.childNodes).forEach(child => {
				// Pass roots to children so nested root-content can be captured
				const childNode = parseHtmlNode(child, roots);
				if (childNode) {
					children.push(childNode);
				}
			});
			if (children.length > 0) {
				result.c = children.length == 1 ? children[0] : children;
			}
		}

		if (result.t == 'p' && !result.c) {
			// Strip empties
			return null;
		}

		return result;
	}
}
