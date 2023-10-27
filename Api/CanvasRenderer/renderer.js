var preact = window.Preact || window.preact || window.React;

function _getContentTypeIdFactory() {
	const _hash1 = ((5381 << 16) + 5381) | 0;
	const floor = Math.floor;

	/*
		Converts a typeName like "BlogPost" to its numeric content type ID.
		If porting this, instead take a look at the C# version in ContentTypes.cs. 
		Most of the stuff here is for forcing JS to do integer arithmetic.
	*/
	return function (typeName) {
		typeName = typeName.toLowerCase();
		var hash1 = _hash1;
		var hash2 = hash1;

		for (var i = 0; i < typeName.length; i += 2) {
			var s1 = ~~floor(hash1 << 5);
			hash1 = ~~floor(s1 + hash1);
			hash1 = hash1 ^ typeName.charCodeAt(i);
			if (i == typeName.length - 1)
				break;

			s1 = ~~floor(hash2 << 5);
			hash2 = ~~floor(s1 + hash2);
			hash2 = hash2 ^ typeName.charCodeAt(i + 1);
		}

		var result = ~~floor(Math.imul(hash2, 1566083941));
		result = ~~floor(hash1 + result);
		return result;
	};
}

const getContentTypeId = _getContentTypeIdFactory();

// E.g. https://site.com - never ends with a /
const siteOrigin = location.origin;

/*
* Originates from preact-render-to-string.
 * Was modified to make it async friendly, however, it caused massive bottlenecks 
 * between C# and JS due to the way how the link is implemented by MS. So instead, 
 * requires all data to be passed in. It knows what data is required via the graphs in the canvas.
*/

function reactPreRender() {

	const IS_NON_DIMENSIONAL = /acit|ex(?:s|g|n|p|$)|rph|grid|ows|mnc|ntw|ine[ch]|zoo|^ord|^--/i;

	function encodeEntities(s) {
		if (typeof s !== 'string') s = String(s);
		let out = '';
		for (let i = 0; i < s.length; i++) {
			let ch = s[i];
			// prettier-ignore
			switch (ch) {
				case '<': out += '&lt;'; break;
				case '>': out += '&gt;'; break;
				case '"': out += '&quot;'; break;
				case '&': out += '&amp;'; break;
				default: out += ch;
			}
		}
		return out;
	}

	let indent = (s, char) =>
		String(s).replace(/(\n+)/g, '$1' + (char || '\t'));

	let isLargeString = (s, length, ignoreLines) =>
		String(s).length > (length || 40) ||
		(!ignoreLines && String(s).indexOf('\n') !== -1) ||
		String(s).indexOf('<') !== -1;

	const JS_TO_CSS = {};

	// Convert an Object style to a CSSText string
	function styleObjToCss(s) {
		let str = '';
		for (let prop in s) {
			let val = s[prop];
			if (val != null) {
				if (str) str += ' ';
				// str += jsToCss(prop);
				str +=
					prop[0] == '-'
						? prop
						: JS_TO_CSS[prop] ||
						(JS_TO_CSS[prop] = prop.replace(/([A-Z])/g, '-$1').toLowerCase());
				str += ': ';
				str += val;
				if (typeof val === 'number' && IS_NON_DIMENSIONAL.test(prop) === false) {
					str += 'px';
				}
				str += ';';
			}
		}
		return str || undefined;
	}

	/**
	 * Copy all properties from `props` onto `obj`.
	 * @param {object} obj Object onto which properties should be copied.
	 * @param {object} props Object from which to copy properties.
	 * @returns {object}
	 * @private
	 */
	function assign(obj, props) {
		for (let i in props) obj[i] = props[i];
		return obj;
	}

	/**
	 * Get flattened children from the children prop
	 * @param {Array} accumulator
	 * @param {any} children A `props.children` opaque object.
	 * @returns {Array} accumulator
	 * @private
	 */
	function getChildren(accumulator, children) {
		if (Array.isArray(children)) {
			children.reduce(getChildren, accumulator);
		} else if (children != null && children !== false) {
			accumulator.push(children);
		}
		return accumulator;
	}

	var { options, Fragment, createElement } = preact;

	const SHALLOW = { shallow: true };

	// components without names, kept as a hash for later comparison to return consistent UnnamedComponentXX names.
	const UNNAMED = [];

	const VOID_ELEMENTS = /^(area|base|br|col|embed|hr|img|input|link|meta|param|source|track|wbr)$/;

	const noop = () => { };

	/** Render Preact JSX + Components to an HTML string.
	 *	@name render
	 *	@function
	 *	@param {VNode} vnode	JSX VNode to render.
	 *	@param {Object} [context={}]	Optionally pass an initial context object through the render path.
	 *	@param {Object} [options={}]	Rendering options
	 *	@param {Boolean} [options.shallow=false]	If `true`, renders nested Components as HTML elements (`<Foo a="b" />`).
	 *	@param {Boolean} [options.mode=1|2|3]	If 1, renders only html. 2 for text only and 3 for both.
	 *	@param {RegEx|undefined} [options.voidElements]       RegeEx that matches elements that are considered void (self-closing)
	 */
	renderToString.render = renderToString;
	
	const EMPTY_ARR = [];
	function renderToString(vnode, context, opts) {
		opts = opts || {};
		var res = { body: '', text: '' };
		opts.result = res;
		_renderToString(vnode, context, opts);
		// options._commit, we don't schedule any effects in this library right now,
		// so we can pass an empty queue to this hook.
		if (options.__c) options.__c(vnode, EMPTY_ARR);
		return res;
	}

	/** The default export is an alias of `render()`. */
	function _renderToString(vnode, context, opts, inner, isSvgMode, selectValue) {
		if (vnode == null || typeof vnode === 'boolean') {
			return;
		}

		// wrap array nodes in Fragment
		if (Array.isArray(vnode)) {
			vnode = createElement(Fragment, null, vnode);
		}

		let nodeName = vnode.type,
			props = vnode.props,
			isComponent = false;
		context = context || {};

		let indentChar = '\t';

		// #text nodes
		if (typeof vnode !== 'object' && !nodeName) {
			if (opts.mode & 1) {
				opts.result.body += encodeEntities(vnode);
			}
			if (opts.mode & 2) {
				opts.result.text += vnode;
			}
			return;
		}

		// components
		if (typeof nodeName === 'function') {
			isComponent = true;
			if (opts.shallow && (inner || opts.renderRootComponent === false)) {
				nodeName = getComponentName(nodeName);
			} else if (nodeName === Fragment) {
				let children = [];
				getChildren(children, vnode.props.children);

				for (let i = 0; i < children.length; i++) {
					_renderToString(
						children[i],
						context,
						opts,
						opts.shallowHighOrder !== false,
						isSvgMode,
						selectValue
					);
				}
				return;
			} else {
				let rendered;

				let c = (vnode.__c = {
					__v: vnode,
					context,
					props: vnode.props,
					// silently drop state updates
					setState: noop,
					forceUpdate: noop,
					// hooks
					__h: []
				});

				// options._diff
				if (options.__b) options.__b(vnode);

				// options._render
				if (options.__r) options.__r(vnode);

				if (
					!nodeName.prototype ||
					typeof nodeName.prototype.render !== 'function'
				) {
					// Necessary for createContext api. Setting this property will pass
					// the context value as `this.context` just for this component.
					let cxType = nodeName.contextType;
					let provider = cxType && context[cxType.__c];
					let cctx =
						cxType != null
							? provider
								? provider.props.value
								: cxType.__
							: context;

					// stateless functional components
					rendered = nodeName.call(vnode.__c, props, cctx);
				} else {
					// class-based components
					let cxType = nodeName.contextType;
					let provider = cxType && context[cxType.__c];
					let cctx =
						cxType != null
							? provider
								? provider.props.value
								: cxType.__
							: context;

					// c = new nodeName(props, context);
					c = vnode.__c = new nodeName(props, cctx);
					c.__v = vnode;
					// turn off stateful re-rendering:
					c._dirty = c.__d = true;
					c.props = props;
					if (c.state == null) {
						c.state = {};
					}

					if (c._nextState == null && c.__s == null) {
						c._nextState = c.__s = c.state;
					}

					c.context = cctx;
					if (nodeName.getDerivedStateFromProps)
						c.state = assign(
							assign({}, c.state),
							nodeName.getDerivedStateFromProps(c.props, c.state)
						);
					else if (c.componentWillMount) {
						c.componentWillMount();

						// If the user called setState in cWM we need to flush pending,
						// state updates. This is the same behaviour in React.
						c.state =
							c._nextState !== c.state
								? c._nextState
								: c.__s !== c.state
									? c.__s
									: c.state;
					}

					rendered = c.render(c.props, c.state, c.context);
				}

				if (c.getChildContext) {
					context = assign(assign({}, context), c.getChildContext());
				}

				if (options.diffed) options.diffed(vnode);

				_renderToString(
					rendered,
					context,
					opts,
					opts.shallowHighOrder !== false,
					isSvgMode,
					selectValue
				);
				return;
			}
		}

		// render JSX to HTML
		let propChildren,
			html;

		if (opts.mode & 1) {
			opts.result.body += '<' + nodeName;
		}

		if (props) {
			let attrs = Object.keys(props);

			// allow sorting lexicographically for more determinism (useful for tests, such as via preact-jsx-chai)
			if (opts && opts.sortAttributes === true) attrs.sort();

			for (let i = 0; i < attrs.length; i++) {
				let name = attrs[i],
					v = props[name];
				if (name === 'children') {
					propChildren = v;
					continue;
				}

				if (name.match(/[\s\n\\/='"\0<>]/)) continue;

				if (
					!(opts && opts.allAttributes) &&
					(name === 'key' ||
						name === 'ref' ||
						name === '__self' ||
						name === '__source' ||
						name === 'defaultValue')
				)
					continue;

				if (name === 'className') {
					if (props.class) continue;
					name = 'class';
				} else if (isSvgMode && name.match(/^xlink:?./)) {
					name = name.toLowerCase().replace(/^xlink:?/, 'xlink:');
				}

				if (name === 'htmlFor') {
					if (props.for) continue;
					name = 'for';
				}

				if (name === 'style' && v && typeof v === 'object') {
					v = styleObjToCss(v);
				}

				// always use string values instead of booleans for aria attributes
				// also see https://github.com/preactjs/preact/pull/2347/files
				if (name[0] === 'a' && name['1'] === 'r' && typeof v === 'boolean') {
					v = String(v);
				}

				let hooked =
					opts.attributeHook &&
					opts.attributeHook(name, v, context, opts, isComponent);
				if (hooked || hooked === '') {
					if (opts.mode & 1) {
						opts.result.body += hooked;
					}
					continue;
				}

				if (name === 'dangerouslySetInnerHTML') {
					html = v && v.__html;
				} else if (nodeName === 'textarea' && name === 'value') {
					// <textarea value="a&b"> --> <textarea>a&amp;b</textarea>
					propChildren = v;
				} else if ((v || v === 0 || v === '') && typeof v !== 'function') {
					if (v === true || v === '') {
						v = name;
						// boolean attributes
						if (opts.mode & 1) {
							opts.result.body += ' ' + name;
						}
						continue;
					}

					if (name === 'value') {
						if (nodeName === 'select') {
							selectValue = v;
							continue;
						} else if (nodeName === 'option' && selectValue == v) {
							if (opts.mode & 1) {
								opts.result.body += ' selected';
							}
						}
					}
					if (opts.mode & 1) {
						if (opts.absoluteUrls && (name == 'src' || name == 'href')) {
							// Make absolute
							var url = encodeEntities(v);

							if (!url.startsWith('http:') && !url.startsWith('https:') && !url.startsWith('//')) {
								if (url.length && url[0] == '/') {
									url = siteOrigin + url;
								} else {
									url = siteOrigin + '/' + url;
								}
							}

							opts.result.body += ` ${name}="${url}"`;
						} else {
							opts.result.body += ` ${name}="${encodeEntities(v)}"`;
						}
					}
				}
			}
		}

		let isVoid =
			String(nodeName).match(VOID_ELEMENTS) ||
			(opts.voidElements && String(nodeName).match(opts.voidElements));

		let children;
		if (html) {
			// if multiline, indent.
			if (opts.mode & 1) {
				opts.result.body += '>' + html;
			}

			if (opts.mode & 2) {
				opts.result.text += htmlToText(html);
			}

		} else if (
			propChildren != null &&
			getChildren((children = []), propChildren).length
		) {
			if (opts.mode & 1) {
				opts.result.body += '>';
			}

			for (let i = 0; i < children.length; i++) {
				let child = children[i];

				if (child != null && child !== false) {
					let childSvgMode =
						nodeName === 'svg'
							? true
							: nodeName === 'foreignObject'
								? false
								: isSvgMode;

					_renderToString(
						child,
						context,
						opts,
						true,
						childSvgMode,
						selectValue
					);
				}
			}
		}
		else {
			if (opts.mode & 1) {
				opts.result.body += isVoid ? '/>' : '>';
			}
		}

		if ((opts.mode & 1) && (!isVoid || children || html)) {
			opts.result.body += '</' + nodeName + '>';
		}
	}

	function getComponentName(component) {
		return (
			component.displayName ||
			(component !== Function && component.name) ||
			getFallbackComponentName(component)
		);
	}

	function getFallbackComponentName(component) {
		let str = Function.prototype.toString.call(component),
			name = (str.match(/^\s*function\s+([^( ]+)/) || '')[1];
		if (!name) {
			// search for an existing indexed name for the given component:
			let index = -1;
			for (let i = UNNAMED.length; i--;) {
				if (UNNAMED[i] === component) {
					index = i;
					break;
				}
			}
			// not found, create a new indexed name:
			if (index < 0) {
				index = UNNAMED.push(component) - 1;
			}
			name = `UnnamedComponent${index}`;
		}
		return name;
	}

	return renderToString;
}

var renderToString = reactPreRender();
module = undefined;
exports = undefined;

// Get canvas and UI/Content:
var _Canvas = require('UI/Canvas').default;
var _Session = require("UI/Session");
var _Graph = require("UI/Functions/GraphRuntime/Graph");
var _webRequestModule = require("UI/Functions/WebRequest");

// Stub:
pageRouter = {
	state: {}
};

function fetch(url, opts) {
	// Reject all fetch requests.
	return Promise.reject({ error: 'This request is not supported server side.', serverside: true, url });
}

function _noOp() { }

function renderCanvas(bodyJson, publicApiContextJson, pageState, mode, absoluteUrls) {

	// Session state:
	var session = {
		...JSON.parse(publicApiContextJson)
	};

	// Page state:
	pageState = JSON.parse(pageState);

	// Expand includes on the session.
	for (var k in session) {
		session[k] = _webRequestModule.expandIncludes(session[k]);
	}

	var canvas = preact.createElement(_Canvas, { children: bodyJson });

	// Pass in the 2 main pieces of global state via context providers - the session and the pageRouter state:
	var pageProvider = preact.createElement(_Session.Router.Provider, {
		value: {
			pageState,
			setPage: _noOp
		}, children: canvas
	});

	var sessionProvider = preact.createElement(_Session.Session.Provider, {
		value: {
			session,
			setSession: _noOp
		}, children: pageProvider
	});

	var ctx = {
	};
	
	var opts = { mode, absoluteUrls, ctx, result: null };

	var graphCacheProvider = preact.createElement(_Graph.Provider, { ctx: opts, children: sessionProvider });

	// Returns the string.
	var result = renderToString(graphCacheProvider, {}, opts);

	if (mode == 3) {
		// Both
		return result;
	}
	else if (mode == 2) {
		// Text only
		return result.text;
	}

	// html only
	return result.body;
}

var htmlToText = (function () {

	function htmlToText(html) {
		if (!html || !html.length) {
			return '';
		}
		var result = '';
		var mode = 0; // 0=text, 1=inside a <tag>
		var storedIndex = 0;
		var htmlEntity = '';
		var htmlEntityValue;

		for (var i = 0; i < html.length; i++) {
			var currentChar = html[i];
			if (mode == 0) {
				if (currentChar == '<') {
					// We have a couple extra checks to be safe in case this is a comment by checking the next two chars
					if (peek(i + 1, html) == '!' && peek(i + 2, html) == '-' && peek(i + 3, html) == '-') {
						mode = 3;
						i += 3;
					} else {
						mode = 1;
					}
				} else if (currentChar == '&') {
					// inside a html encoded entity like &amp;
					// read chars until ;, convert entity to its string literal value, add that to result. Let's store the current index. 
					mode = 2;
					storedIndex = i;
				} else {
					result += currentChar;
				}
			} else if (mode == 1) {
				if (currentChar == '>') {
					// exiting the tag
					mode = 0;
				} else if (currentChar == '"') {
					mode = 4;
				} else if (currentChar == '\'') {
					mode = 5;
				}
			} else if (mode == 2) {
				// a html encoded entity.
				if (currentChar == ';') {
					// let's construct the entity.
					htmlEntity = html.substring(storedIndex + 1, i);
					// Grab the entity from the map and toss it into the result.
					htmlEntityValue = htmlEntityToLiteral(htmlEntity);

					if (htmlEntityValue) {
						// Append the entity
						result += htmlEntityValue;
					} else {
						// We are appending the alegged entity since it was invalid
						result += '&' + htmlEntity + ';';
					}

					// lookup the entity that was just read
					mode = 0;
				} else {
					// If current char is not alphanum, exit this mode.
					var currentCode = html.charCodeAt(i);
					if (!(currentCode >= 65 && currentCode <= 90) && !(currentCode >= 48 && currentCode <= 57) && !(currentCode >= 97 && currentCode <= 122)) {
						// it's not A-Z, 0-9 or a-z. quit, but add what we just skipped.
						result += '&' + html.substring(storedIndex + 1, i);
						mode = 0;
						// Back it up in case the non alpha char was relevant to the parser.
						i--;
					}
				}
			} else if (mode == 3) {
				// Inside a comment.
				if (currentChar == '-' && peek(i + 1, html) == '-' && peek(i + 2, html) == '>') {
					mode = 0;
					i += 2
				}
			} else if (mode == 4) {
				// "attrib"
				if (currentChar == '"') {
					// exiting the attrib
					mode = 1;
				}
			} else if (mode == 5) {
				// 'attrib'
				if (currentChar == '\'') {
					// exiting the attrib
					mode = 1;
				}
			}
		}
		return result;
	}

	function peek(index, str) {
		if (index >= str.length) {
			return '';
		}
		return str[index];
	}


	function htmlEntityToLiteral(entity) {

		if (entity.length > 2 && entity[0] == '0' && entity[1] == 'x') {
			// hex charcode
			return String.fromCharCode(parseInt(Number(entity), 10));

		} else if (entity.length > 1 && entity[0] == '#') {
			// dec charcode
			return String.fromCharCode(parseInt(entity.substring(1)));

		} else {
			// Undefined if doesn't exist
			return entityMapping[entity.toLowerCase()];
		}
	}

	var entityMapping = {
		'aacute': '000c1',
		'aacute': '000e1',
		'abreve': '00102',
		'abreve': '00103',
		'ac': '0223e',
		'acd': '0223f',
		'ace': ['0223e', '00333'],
		'acirc': '000c2',
		'acirc': '000e2',
		'acute': '000b4',
		'acy': '00410',
		'acy': '00430',
		'aelig': '000c6',
		'aelig': '000e6',
		'af': '02061',
		'afr': '1d504',
		'afr': '1d51e',
		'agrave': '000c0',
		'agrave': '000e0',
		'alefsym': '02135',
		'aleph': '02135',
		'alpha': '00391',
		'alpha': '003b1',
		'amacr': '00100',
		'amacr': '00101',
		'amalg': '02a3f',
		'amp': '00026',
		'amp': '00026',
		'and': '02a53',
		'and': '02227',
		'andand': '02a55',
		'andd': '02a5c',
		'andslope': '02a58',
		'andv': '02a5a',
		'ang': '02220',
		'ange': '029a4',
		'angle': '02220',
		'angmsd': '02221',
		'angmsdaa': '029a8',
		'angmsdab': '029a9',
		'angmsdac': '029aa',
		'angmsdad': '029ab',
		'angmsdae': '029ac',
		'angmsdaf': '029ad',
		'angmsdag': '029ae',
		'angmsdah': '029af',
		'angrt': '0221f',
		'angrtvb': '022be',
		'angrtvbd': '0299d',
		'angsph': '02222',
		'angst': '000c5',
		'angzarr': '0237c',
		'aogon': '00104',
		'aogon': '00105',
		'aopf': '1d538',
		'aopf': '1d552',
		'ap': '02248',
		'apacir': '02a6f',
		'ape': '02a70',
		'ape': '0224a',
		'apid': '0224b',
		'apos': '00027',
		'applyfunction': '02061',
		'approx': '02248',
		'approxeq': '0224a',
		'aring': '000c5',
		'aring': '000e5',
		'ascr': '1d49c',
		'ascr': '1d4b6',
		'assign': '02254',
		'ast': '0002a',
		'asymp': '02248',
		'asympeq': '0224d',
		'atilde': '000c3',
		'atilde': '000e3',
		'auml': '000c4',
		'auml': '000e4',
		'awconint': '02233',
		'awint': '02a11',
		'backcong': '0224c',
		'backepsilon': '003f6',
		'backprime': '02035',
		'backsim': '0223d',
		'backsimeq': '022cd',
		'backslash': '02216',
		'barv': '02ae7',
		'barvee': '022bd',
		'barwed': '02306',
		'barwed': '02305',
		'barwedge': '02305',
		'bbrk': '023b5',
		'bbrktbrk': '023b6',
		'bcong': '0224c',
		'bcy': '00411',
		'bcy': '00431',
		'bdquo': '0201e',
		'becaus': '02235',
		'because': '02235',
		'because': '02235',
		'bemptyv': '029b0',
		'bepsi': '003f6',
		'bernou': '0212c',
		'bernoullis': '0212c',
		'beta': '00392',
		'beta': '003b2',
		'beth': '02136',
		'between': '0226c',
		'bfr': '1d505',
		'bfr': '1d51f',
		'bigcap': '022c2',
		'bigcirc': '025ef',
		'bigcup': '022c3',
		'bigodot': '02a00',
		'bigoplus': '02a01',
		'bigotimes': '02a02',
		'bigsqcup': '02a06',
		'bigstar': '02605',
		'bigtriangledown': '025bd',
		'bigtriangleup': '025b3',
		'biguplus': '02a04',
		'bigvee': '022c1',
		'bigwedge': '022c0',
		'bkarow': '0290d',
		'blacklozenge': '029eb',
		'blacksquare': '025aa',
		'blacktriangle': '025b4',
		'blacktriangledown': '025be',
		'blacktriangleleft': '025c2',
		'blacktriangleright': '025b8',
		'blank': '02423',
		'blk12': '02592',
		'blk14': '02591',
		'blk34': '02593',
		'block': '02588',
		'bne': ['0003d', '020e5'],
		'bnequiv': ['02261', '020e5'],
		'bnot': '02aed',
		'bnot': '02310',
		'bopf': '1d539',
		'bopf': '1d553',
		'bot': '022a5',
		'bottom': '022a5',
		'bowtie': '022c8',
		'boxbox': '029c9',
		'boxdl': '02557',
		'boxdl': '02556',
		'boxdl': '02555',
		'boxdl': '02510',
		'boxdr': '02554',
		'boxdr': '02553',
		'boxdr': '02552',
		'boxdr': '0250c',
		'boxh': '02550',
		'boxh': '02500',
		'boxhd': '02566',
		'boxhd': '02564',
		'boxhd': '02565',
		'boxhd': '0252c',
		'boxhu': '02569',
		'boxhu': '02567',
		'boxhu': '02568',
		'boxhu': '02534',
		'boxminus': '0229f',
		'boxplus': '0229e',
		'boxtimes': '022a0',
		'boxul': '0255d',
		'boxul': '0255c',
		'boxul': '0255b',
		'boxul': '02518',
		'boxur': '0255a',
		'boxur': '02559',
		'boxur': '02558',
		'boxur': '02514',
		'boxv': '02551',
		'boxv': '02502',
		'boxvh': '0256c',
		'boxvh': '0256b',
		'boxvh': '0256a',
		'boxvh': '0253c',
		'boxvl': '02563',
		'boxvl': '02562',
		'boxvl': '02561',
		'boxvl': '02524',
		'boxvr': '02560',
		'boxvr': '0255f',
		'boxvr': '0255e',
		'boxvr': '0251c',
		'bprime': '02035',
		'breve': '002d8',
		'breve': '002d8',
		'brvbar': '000a6',
		'bscr': '0212c',
		'bscr': '1d4b7',
		'bsemi': '0204f',
		'bsim': '0223d',
		'bsime': '022cd',
		'bsol': '0005c',
		'bsolb': '029c5',
		'bsolhsub': '027c8',
		'bull': '02022',
		'bullet': '02022',
		'bump': '0224e',
		'bumpe': '02aae',
		'bumpe': '0224f',
		'bumpeq': '0224e',
		'bumpeq': '0224f',
		'cacute': '00106',
		'cacute': '00107',
		'cap': '022d2',
		'cap': '02229',
		'capand': '02a44',
		'capbrcup': '02a49',
		'capcap': '02a4b',
		'capcup': '02a47',
		'capdot': '02a40',
		'capitaldifferentiald': '02145',
		'caps': ['02229', '0fe00'],
		'caret': '02041',
		'caron': '002c7',
		'cayleys': '0212d',
		'ccaps': '02a4d',
		'ccaron': '0010c',
		'ccaron': '0010d',
		'ccedil': '000c7',
		'ccedil': '000e7',
		'ccirc': '00108',
		'ccirc': '00109',
		'cconint': '02230',
		'ccups': '02a4c',
		'ccupssm': '02a50',
		'cdot': '0010a',
		'cdot': '0010b',
		'cedil': '000b8',
		'cedilla': '000b8',
		'cemptyv': '029b2',
		'cent': '000a2',
		'centerdot': '000b7',
		'centerdot': '000b7',
		'cfr': '0212d',
		'cfr': '1d520',
		'chcy': '00427',
		'chcy': '00447',
		'check': '02713',
		'checkmark': '02713',
		'chi': '003a7',
		'chi': '003c7',
		'cir': '025cb',
		'circ': '002c6',
		'circeq': '02257',
		'circlearrowleft': '021ba',
		'circlearrowright': '021bb',
		'circledast': '0229b',
		'circledcirc': '0229a',
		'circleddash': '0229d',
		'circledot': '02299',
		'circledr': '000ae',
		'circleds': '024c8',
		'circleminus': '02296',
		'circleplus': '02295',
		'circletimes': '02297',
		'cire': '029c3',
		'cire': '02257',
		'cirfnint': '02a10',
		'cirmid': '02aef',
		'cirscir': '029c2',
		'clockwisecontourintegral': '02232',
		'closecurlydoublequote': '0201d',
		'closecurlyquote': '02019',
		'clubs': '02663',
		'clubsuit': '02663',
		'colon': '02237',
		'colon': '0003a',
		'colone': '02a74',
		'colone': '02254',
		'coloneq': '02254',
		'comma': '0002c',
		'commat': '00040',
		'comp': '02201',
		'compfn': '02218',
		'complement': '02201',
		'complexes': '02102',
		'cong': '02245',
		'congdot': '02a6d',
		'congruent': '02261',
		'conint': '0222f',
		'conint': '0222e',
		'contourintegral': '0222e',
		'copf': '02102',
		'copf': '1d554',
		'coprod': '02210',
		'coproduct': '02210',
		'copy': '000a9',
		'copy': '000a9',
		'copysr': '02117',
		'counterclockwisecontourintegral': '02233',
		'crarr': '021b5',
		'cross': '02a2f',
		'cross': '02717',
		'cscr': '1d49e',
		'cscr': '1d4b8',
		'csub': '02acf',
		'csube': '02ad1',
		'csup': '02ad0',
		'csupe': '02ad2',
		'ctdot': '022ef',
		'cudarrl': '02938',
		'cudarrr': '02935',
		'cuepr': '022de',
		'cuesc': '022df',
		'cularr': '021b6',
		'cularrp': '0293d',
		'cup': '022d3',
		'cup': '0222a',
		'cupbrcap': '02a48',
		'cupcap': '0224d',
		'cupcap': '02a46',
		'cupcup': '02a4a',
		'cupdot': '0228d',
		'cupor': '02a45',
		'cups': ['0222a', '0fe00'],
		'curarr': '021b7',
		'curarrm': '0293c',
		'curlyeqprec': '022de',
		'curlyeqsucc': '022df',
		'curlyvee': '022ce',
		'curlywedge': '022cf',
		'curren': '000a4',
		'curvearrowleft': '021b6',
		'curvearrowright': '021b7',
		'cuvee': '022ce',
		'cuwed': '022cf',
		'cwconint': '02232',
		'cwint': '02231',
		'cylcty': '0232d',
		'dagger': '02021',
		'dagger': '02020',
		'daleth': '02138',
		'darr': '021a1',
		'darr': '021d3',
		'darr': '02193',
		'dash': '02010',
		'dashv': '02ae4',
		'dashv': '022a3',
		'dbkarow': '0290f',
		'dblac': '002dd',
		'dcaron': '0010e',
		'dcaron': '0010f',
		'dcy': '00414',
		'dcy': '00434',
		'dd': '02145',
		'dd': '02146',
		'ddagger': '02021',
		'ddarr': '021ca',
		'ddotrahd': '02911',
		'ddotseq': '02a77',
		'deg': '000b0',
		'del': '02207',
		'delta': '00394',
		'delta': '003b4',
		'demptyv': '029b1',
		'dfisht': '0297f',
		'dfr': '1d507',
		'dfr': '1d521',
		'dhar': '02965',
		'dharl': '021c3',
		'dharr': '021c2',
		'diacriticalacute': '000b4',
		'diacriticaldot': '002d9',
		'diacriticaldoubleacute': '002dd',
		'diacriticalgrave': '00060',
		'diacriticaltilde': '002dc',
		'diam': '022c4',
		'diamond': '022c4',
		'diamond': '022c4',
		'diamondsuit': '02666',
		'diams': '02666',
		'die': '000a8',
		'differentiald': '02146',
		'digamma': '003dd',
		'disin': '022f2',
		'div': '000f7',
		'divide': '000f7',
		'divideontimes': '022c7',
		'divonx': '022c7',
		'djcy': '00402',
		'djcy': '00452',
		'dlcorn': '0231e',
		'dlcrop': '0230d',
		'dollar': '00024',
		'dopf': '1d53b',
		'dopf': '1d555',
		'dot': '000a8',
		'dot': '002d9',
		'dotdot': '020dc',
		'doteq': '02250',
		'doteqdot': '02251',
		'dotequal': '02250',
		'dotminus': '02238',
		'dotplus': '02214',
		'dotsquare': '022a1',
		'doublebarwedge': '02306',
		'doublecontourintegral': '0222f',
		'doubledot': '000a8',
		'doubledownarrow': '021d3',
		'doubleleftarrow': '021d0',
		'doubleleftrightarrow': '021d4',
		'doublelefttee': '02ae4',
		'doublelongleftarrow': '027f8',
		'doublelongleftrightarrow': '027fa',
		'doublelongrightarrow': '027f9',
		'doublerightarrow': '021d2',
		'doublerighttee': '022a8',
		'doubleuparrow': '021d1',
		'doubleupdownarrow': '021d5',
		'doubleverticalbar': '02225',
		'downarrow': '02193',
		'downarrow': '021d3',
		'downarrow': '02193',
		'downarrowbar': '02913',
		'downarrowuparrow': '021f5',
		'downbreve': '00311',
		'downdownarrows': '021ca',
		'downharpoonleft': '021c3',
		'downharpoonright': '021c2',
		'downleftrightvector': '02950',
		'downleftteevector': '0295e',
		'downleftvector': '021bd',
		'downleftvectorbar': '02956',
		'downrightteevector': '0295f',
		'downrightvector': '021c1',
		'downrightvectorbar': '02957',
		'downtee': '022a4',
		'downteearrow': '021a7',
		'drbkarow': '02910',
		'drcorn': '0231f',
		'drcrop': '0230c',
		'dscr': '1d49f',
		'dscr': '1d4b9',
		'dscy': '00405',
		'dscy': '00455',
		'dsol': '029f6',
		'dstrok': '00110',
		'dstrok': '00111',
		'dtdot': '022f1',
		'dtri': '025bf',
		'dtrif': '025be',
		'duarr': '021f5',
		'duhar': '0296f',
		'dwangle': '029a6',
		'dzcy': '0040f',
		'dzcy': '0045f',
		'dzigrarr': '027ff',
		'eacute': '000c9',
		'eacute': '000e9',
		'easter': '02a6e',
		'ecaron': '0011a',
		'ecaron': '0011b',
		'ecir': '02256',
		'ecirc': '000ca',
		'ecirc': '000ea',
		'ecolon': '02255',
		'ecy': '0042d',
		'ecy': '0044d',
		'eddot': '02a77',
		'edot': '00116',
		'edot': '02251',
		'edot': '00117',
		'ee': '02147',
		'efdot': '02252',
		'efr': '1d508',
		'efr': '1d522',
		'eg': '02a9a',
		'egrave': '000c8',
		'egrave': '000e8',
		'egs': '02a96',
		'egsdot': '02a98',
		'el': '02a99',
		'element': '02208',
		'elinters': '023e7',
		'ell': '02113',
		'els': '02a95',
		'elsdot': '02a97',
		'emacr': '00112',
		'emacr': '00113',
		'empty': '02205',
		'emptyset': '02205',
		'emptysmallsquare': '025fb',
		'emptyv': '02205',
		'emptyverysmallsquare': '025ab',
		'emsp': '02003',
		'emsp13': '02004',
		'emsp14': '02005',
		'eng': '0014a',
		'eng': '0014b',
		'ensp': '02002',
		'eogon': '00118',
		'eogon': '00119',
		'eopf': '1d53c',
		'eopf': '1d556',
		'epar': '022d5',
		'eparsl': '029e3',
		'eplus': '02a71',
		'epsi': '003b5',
		'epsilon': '00395',
		'epsilon': '003b5',
		'epsiv': '003f5',
		'eqcirc': '02256',
		'eqcolon': '02255',
		'eqsim': '02242',
		'eqslantgtr': '02a96',
		'eqslantless': '02a95',
		'equal': '02a75',
		'equals': '0003d',
		'equaltilde': '02242',
		'equest': '0225f',
		'equilibrium': '021cc',
		'equiv': '02261',
		'equivdd': '02a78',
		'eqvparsl': '029e5',
		'erarr': '02971',
		'erdot': '02253',
		'escr': '02130',
		'escr': '0212f',
		'esdot': '02250',
		'esim': '02a73',
		'esim': '02242',
		'eta': '00397',
		'eta': '003b7',
		'eth': '000d0',
		'eth': '000f0',
		'euml': '000cb',
		'euml': '000eb',
		'euro': '020ac',
		'excl': '00021',
		'exist': '02203',
		'exists': '02203',
		'expectation': '02130',
		'exponentiale': '02147',
		'exponentiale': '02147',
		'fallingdotseq': '02252',
		'fcy': '00424',
		'fcy': '00444',
		'female': '02640',
		'ffilig': '0fb03',
		'fflig': '0fb00',
		'ffllig': '0fb04',
		'ffr': '1d509',
		'ffr': '1d523',
		'filig': '0fb01',
		'filledsmallsquare': '025fc',
		'filledverysmallsquare': '025aa',
		'fjlig': ['00066', '0006a'],
		'flat': '0266d',
		'fllig': '0fb02',
		'fltns': '025b1',
		'fnof': '00192',
		'fopf': '1d53d',
		'fopf': '1d557',
		'forall': '02200',
		'forall': '02200',
		'fork': '022d4',
		'forkv': '02ad9',
		'fouriertrf': '02131',
		'fpartint': '02a0d',
		'frac12': '000bd',
		'frac13': '02153',
		'frac14': '000bc',
		'frac15': '02155',
		'frac16': '02159',
		'frac18': '0215b',
		'frac23': '02154',
		'frac25': '02156',
		'frac34': '000be',
		'frac35': '02157',
		'frac38': '0215c',
		'frac45': '02158',
		'frac56': '0215a',
		'frac58': '0215d',
		'frac78': '0215e',
		'frasl': '02044',
		'frown': '02322',
		'fscr': '02131',
		'fscr': '1d4bb',
		'gacute': '001f5',
		'gamma': '00393',
		'gamma': '003b3',
		'gammad': '003dc',
		'gammad': '003dd',
		'gap': '02a86',
		'gbreve': '0011e',
		'gbreve': '0011f',
		'gcedil': '00122',
		'gcirc': '0011c',
		'gcirc': '0011d',
		'gcy': '00413',
		'gcy': '00433',
		'gdot': '00120',
		'gdot': '00121',
		'ge': '02267',
		'ge': '02265',
		'gel': '02a8c',
		'gel': '022db',
		'geq': '02265',
		'geqq': '02267',
		'geqslant': '02a7e',
		'ges': '02a7e',
		'gescc': '02aa9',
		'gesdot': '02a80',
		'gesdoto': '02a82',
		'gesdotol': '02a84',
		'gesl': ['022db', '0fe00'],
		'gesles': '02a94',
		'gfr': '1d50a',
		'gfr': '1d524',
		'gg': '022d9',
		'gg': '0226b',
		'ggg': '022d9',
		'gimel': '02137',
		'gjcy': '00403',
		'gjcy': '00453',
		'gl': '02277',
		'gla': '02aa5',
		'gle': '02a92',
		'glj': '02aa4',
		'gnap': '02a8a',
		'gnapprox': '02a8a',
		'gne': '02269',
		'gne': '02a88',
		'gneq': '02a88',
		'gneqq': '02269',
		'gnsim': '022e7',
		'gopf': '1d53e',
		'gopf': '1d558',
		'grave': '00060',
		'greaterequal': '02265',
		'greaterequalless': '022db',
		'greaterfullequal': '02267',
		'greatergreater': '02aa2',
		'greaterless': '02277',
		'greaterslantequal': '02a7e',
		'greatertilde': '02273',
		'gscr': '1d4a2',
		'gscr': '0210a',
		'gsim': '02273',
		'gsime': '02a8e',
		'gsiml': '02a90',
		'gt': '0003e',
		'gt': '0226b',
		'gt': '0003e',
		'gtcc': '02aa7',
		'gtcir': '02a7a',
		'gtdot': '022d7',
		'gtlpar': '02995',
		'gtquest': '02a7c',
		'gtrapprox': '02a86',
		'gtrarr': '02978',
		'gtrdot': '022d7',
		'gtreqless': '022db',
		'gtreqqless': '02a8c',
		'gtrless': '02277',
		'gtrsim': '02273',
		'gvertneqq': ['02269', '0fe00'],
		'gvne': ['02269', '0fe00'],
		'hacek': '002c7',
		'hairsp': '0200a',
		'half': '000bd',
		'hamilt': '0210b',
		'hardcy': '0042a',
		'hardcy': '0044a',
		'harr': '021d4',
		'harr': '02194',
		'harrcir': '02948',
		'harrw': '021ad',
		'hat': '0005e',
		'hbar': '0210f',
		'hcirc': '00124',
		'hcirc': '00125',
		'hearts': '02665',
		'heartsuit': '02665',
		'hellip': '02026',
		'hercon': '022b9',
		'hfr': '0210c',
		'hfr': '1d525',
		'hilbertspace': '0210b',
		'hksearow': '02925',
		'hkswarow': '02926',
		'hoarr': '021ff',
		'homtht': '0223b',
		'hookleftarrow': '021a9',
		'hookrightarrow': '021aa',
		'hopf': '0210d',
		'hopf': '1d559',
		'horbar': '02015',
		'horizontalline': '02500',
		'hscr': '0210b',
		'hscr': '1d4bd',
		'hslash': '0210f',
		'hstrok': '00126',
		'hstrok': '00127',
		'humpdownhump': '0224e',
		'humpequal': '0224f',
		'hybull': '02043',
		'hyphen': '02010',
		'iacute': '000cd',
		'iacute': '000ed',
		'ic': '02063',
		'icirc': '000ce',
		'icirc': '000ee',
		'icy': '00418',
		'icy': '00438',
		'idot': '00130',
		'iecy': '00415',
		'iecy': '00435',
		'iexcl': '000a1',
		'iff': '021d4',
		'ifr': '02111',
		'ifr': '1d526',
		'igrave': '000cc',
		'igrave': '000ec',
		'ii': '02148',
		'iiiint': '02a0c',
		'iiint': '0222d',
		'iinfin': '029dc',
		'iiota': '02129',
		'ijlig': '00132',
		'ijlig': '00133',
		'im': '02111',
		'imacr': '0012a',
		'imacr': '0012b',
		'image': '02111',
		'imaginaryi': '02148',
		'imagline': '02110',
		'imagpart': '02111',
		'imath': '00131',
		'imof': '022b7',
		'imped': '001b5',
		'implies': '021d2',
		'in': '02208',
		'incare': '02105',
		'infin': '0221e',
		'infintie': '029dd',
		'inodot': '00131',
		'int': '0222c',
		'int': '0222b',
		'intcal': '022ba',
		'integers': '02124',
		'integral': '0222b',
		'intercal': '022ba',
		'intersection': '022c2',
		'intlarhk': '02a17',
		'intprod': '02a3c',
		'invisiblecomma': '02063',
		'invisibletimes': '02062',
		'iocy': '00401',
		'iocy': '00451',
		'iogon': '0012e',
		'iogon': '0012f',
		'iopf': '1d540',
		'iopf': '1d55a',
		'iota': '00399',
		'iota': '003b9',
		'iprod': '02a3c',
		'iquest': '000bf',
		'iscr': '02110',
		'iscr': '1d4be',
		'isin': '02208',
		'isindot': '022f5',
		'isine': '022f9',
		'isins': '022f4',
		'isinsv': '022f3',
		'isinv': '02208',
		'it': '02062',
		'itilde': '00128',
		'itilde': '00129',
		'iukcy': '00406',
		'iukcy': '00456',
		'iuml': '000cf',
		'iuml': '000ef',
		'jcirc': '00134',
		'jcirc': '00135',
		'jcy': '00419',
		'jcy': '00439',
		'jfr': '1d50d',
		'jfr': '1d527',
		'jmath': '00237',
		'jopf': '1d541',
		'jopf': '1d55b',
		'jscr': '1d4a5',
		'jscr': '1d4bf',
		'jsercy': '00408',
		'jsercy': '00458',
		'jukcy': '00404',
		'jukcy': '00454',
		'kappa': '0039a',
		'kappa': '003ba',
		'kappav': '003f0',
		'kcedil': '00136',
		'kcedil': '00137',
		'kcy': '0041a',
		'kcy': '0043a',
		'kfr': '1d50e',
		'kfr': '1d528',
		'kgreen': '00138',
		'khcy': '00425',
		'khcy': '00445',
		'kjcy': '0040c',
		'kjcy': '0045c',
		'kopf': '1d542',
		'kopf': '1d55c',
		'kscr': '1d4a6',
		'kscr': '1d4c0',
		'laarr': '021da',
		'lacute': '00139',
		'lacute': '0013a',
		'laemptyv': '029b4',
		'lagran': '02112',
		'lambda': '0039b',
		'lambda': '003bb',
		'lang': '027ea',
		'lang': '027e8',
		'langd': '02991',
		'langle': '027e8',
		'lap': '02a85',
		'laplacetrf': '02112',
		'laquo': '000ab',
		'larr': '0219e',
		'larr': '021d0',
		'larr': '02190',
		'larrb': '021e4',
		'larrbfs': '0291f',
		'larrfs': '0291d',
		'larrhk': '021a9',
		'larrlp': '021ab',
		'larrpl': '02939',
		'larrsim': '02973',
		'larrtl': '021a2',
		'lat': '02aab',
		'latail': '0291b',
		'latail': '02919',
		'late': '02aad',
		'lates': ['02aad', '0fe00'],
		'lbarr': '0290e',
		'lbarr': '0290c',
		'lbbrk': '02772',
		'lbrace': '0007b',
		'lbrack': '0005b',
		'lbrke': '0298b',
		'lbrksld': '0298f',
		'lbrkslu': '0298d',
		'lcaron': '0013d',
		'lcaron': '0013e',
		'lcedil': '0013b',
		'lcedil': '0013c',
		'lceil': '02308',
		'lcub': '0007b',
		'lcy': '0041b',
		'lcy': '0043b',
		'ldca': '02936',
		'ldquo': '0201c',
		'ldquor': '0201e',
		'ldrdhar': '02967',
		'ldrushar': '0294b',
		'ldsh': '021b2',
		'le': '02266',
		'le': '02264',
		'leftanglebracket': '027e8',
		'leftarrow': '02190',
		'leftarrow': '021d0',
		'leftarrow': '02190',
		'leftarrowbar': '021e4',
		'leftarrowrightarrow': '021c6',
		'leftarrowtail': '021a2',
		'leftceiling': '02308',
		'leftdoublebracket': '027e6',
		'leftdownteevector': '02961',
		'leftdownvector': '021c3',
		'leftdownvectorbar': '02959',
		'leftfloor': '0230a',
		'leftharpoondown': '021bd',
		'leftharpoonup': '021bc',
		'leftleftarrows': '021c7',
		'leftrightarrow': '02194',
		'leftrightarrow': '021d4',
		'leftrightarrow': '02194',
		'leftrightarrows': '021c6',
		'leftrightharpoons': '021cb',
		'leftrightsquigarrow': '021ad',
		'leftrightvector': '0294e',
		'lefttee': '022a3',
		'leftteearrow': '021a4',
		'leftteevector': '0295a',
		'leftthreetimes': '022cb',
		'lefttriangle': '022b2',
		'lefttrianglebar': '029cf',
		'lefttriangleequal': '022b4',
		'leftupdownvector': '02951',
		'leftupteevector': '02960',
		'leftupvector': '021bf',
		'leftupvectorbar': '02958',
		'leftvector': '021bc',
		'leftvectorbar': '02952',
		'leg': '02a8b',
		'leg': '022da',
		'leq': '02264',
		'leqq': '02266',
		'leqslant': '02a7d',
		'les': '02a7d',
		'lescc': '02aa8',
		'lesdot': '02a7f',
		'lesdoto': '02a81',
		'lesdotor': '02a83',
		'lesg': ['022da', '0fe00'],
		'lesges': '02a93',
		'lessapprox': '02a85',
		'lessdot': '022d6',
		'lesseqgtr': '022da',
		'lesseqqgtr': '02a8b',
		'lessequalgreater': '022da',
		'lessfullequal': '02266',
		'lessgreater': '02276',
		'lessgtr': '02276',
		'lessless': '02aa1',
		'lesssim': '02272',
		'lessslantequal': '02a7d',
		'lesstilde': '02272',
		'lfisht': '0297c',
		'lfloor': '0230a',
		'lfr': '1d50f',
		'lfr': '1d529',
		'lg': '02276',
		'lge': '02a91',
		'lhar': '02962',
		'lhard': '021bd',
		'lharu': '021bc',
		'lharul': '0296a',
		'lhblk': '02584',
		'ljcy': '00409',
		'ljcy': '00459',
		'll': '022d8',
		'll': '0226a',
		'llarr': '021c7',
		'llcorner': '0231e',
		'lleftarrow': '021da',
		'llhard': '0296b',
		'lltri': '025fa',
		'lmidot': '0013f',
		'lmidot': '00140',
		'lmoust': '023b0',
		'lmoustache': '023b0',
		'lnap': '02a89',
		'lnapprox': '02a89',
		'lne': '02268',
		'lne': '02a87',
		'lneq': '02a87',
		'lneqq': '02268',
		'lnsim': '022e6',
		'loang': '027ec',
		'loarr': '021fd',
		'lobrk': '027e6',
		'longleftarrow': '027f5',
		'longleftarrow': '027f8',
		'longleftarrow': '027f5',
		'longleftrightarrow': '027f7',
		'longleftrightarrow': '027fa',
		'longleftrightarrow': '027f7',
		'longmapsto': '027fc',
		'longrightarrow': '027f6',
		'longrightarrow': '027f9',
		'longrightarrow': '027f6',
		'looparrowleft': '021ab',
		'looparrowright': '021ac',
		'lopar': '02985',
		'lopf': '1d543',
		'lopf': '1d55d',
		'loplus': '02a2d',
		'lotimes': '02a34',
		'lowast': '02217',
		'lowbar': '0005f',
		'lowerleftarrow': '02199',
		'lowerrightarrow': '02198',
		'loz': '025ca',
		'lozenge': '025ca',
		'lozf': '029eb',
		'lpar': '00028',
		'lparlt': '02993',
		'lrarr': '021c6',
		'lrcorner': '0231f',
		'lrhar': '021cb',
		'lrhard': '0296d',
		'lrm': '0200e',
		'lrtri': '022bf',
		'lsaquo': '02039',
		'lscr': '02112',
		'lscr': '1d4c1',
		'lsh': '021b0',
		'lsh': '021b0',
		'lsim': '02272',
		'lsime': '02a8d',
		'lsimg': '02a8f',
		'lsqb': '0005b',
		'lsquo': '02018',
		'lsquor': '0201a',
		'lstrok': '00141',
		'lstrok': '00142',
		'lt': '0003c',
		'lt': '0226a',
		'lt': '0003c',
		'ltcc': '02aa6',
		'ltcir': '02a79',
		'ltdot': '022d6',
		'lthree': '022cb',
		'ltimes': '022c9',
		'ltlarr': '02976',
		'ltquest': '02a7b',
		'ltri': '025c3',
		'ltrie': '022b4',
		'ltrif': '025c2',
		'ltrpar': '02996',
		'lurdshar': '0294a',
		'luruhar': '02966',
		'lvertneqq': ['02268', '0fe00'],
		'lvne': ['02268', '0fe00'],
		'macr': '000af',
		'male': '02642',
		'malt': '02720',
		'maltese': '02720',
		'map': '02905',
		'map': '021a6',
		'mapsto': '021a6',
		'mapstodown': '021a7',
		'mapstoleft': '021a4',
		'mapstoup': '021a5',
		'marker': '025ae',
		'mcomma': '02a29',
		'mcy': '0041c',
		'mcy': '0043c',
		'mdash': '02014',
		'mddot': '0223a',
		'measuredangle': '02221',
		'mediumspace': '0205f',
		'mellintrf': '02133',
		'mfr': '1d510',
		'mfr': '1d52a',
		'mho': '02127',
		'micro': '000b5',
		'mid': '02223',
		'midast': '0002a',
		'midcir': '02af0',
		'middot': '000b7',
		'minus': '02212',
		'minusb': '0229f',
		'minusd': '02238',
		'minusdu': '02a2a',
		'minusplus': '02213',
		'mlcp': '02adb',
		'mldr': '02026',
		'mnplus': '02213',
		'models': '022a7',
		'mopf': '1d544',
		'mopf': '1d55e',
		'mp': '02213',
		'mscr': '02133',
		'mscr': '1d4c2',
		'mstpos': '0223e',
		'mu': '0039c',
		'mu': '003bc',
		'multimap': '022b8',
		'mumap': '022b8',
		'nabla': '02207',
		'nacute': '00143',
		'nacute': '00144',
		'nang': ['02220', '020d2'],
		'nap': '02249',
		'nape': ['02a70', '00338'],
		'napid': ['0224b', '00338'],
		'napos': '00149',
		'napprox': '02249',
		'natur': '0266e',
		'natural': '0266e',
		'naturals': '02115',
		'nbsp': '000a0',
		'nbump': ['0224e', '00338'],
		'nbumpe': ['0224f', '00338'],
		'ncap': '02a43',
		'ncaron': '00147',
		'ncaron': '00148',
		'ncedil': '00145',
		'ncedil': '00146',
		'ncong': '02247',
		'ncongdot': ['02a6d', '00338'],
		'ncup': '02a42',
		'ncy': '0041d',
		'ncy': '0043d',
		'ndash': '02013',
		'ne': '02260',
		'nearhk': '02924',
		'nearr': '021d7',
		'nearr': '02197',
		'nearrow': '02197',
		'nedot': ['02250', '00338'],
		'negativemediumspace': '0200b',
		'negativethickspace': '0200b',
		'negativethinspace': '0200b',
		'negativeverythinspace': '0200b',
		'nequiv': '02262',
		'nesear': '02928',
		'nesim': ['02242', '00338'],
		'nestedgreatergreater': '0226b',
		'nestedlessless': '0226a',
		'newline': '0000a',
		'nexist': '02204',
		'nexists': '02204',
		'nfr': '1d511',
		'nfr': '1d52b',
		'nge': ['02267', '00338'],
		'nge': '02271',
		'ngeq': '02271',
		'ngeqq': ['02267', '00338'],
		'ngeqslant': ['02a7e', '00338'],
		'nges': ['02a7e', '00338'],
		'ngg': ['022d9', '00338'],
		'ngsim': '02275',
		'ngt': ['0226b', '020d2'],
		'ngt': '0226f',
		'ngtr': '0226f',
		'ngtv': ['0226b', '00338'],
		'nharr': '021ce',
		'nharr': '021ae',
		'nhpar': '02af2',
		'ni': '0220b',
		'nis': '022fc',
		'nisd': '022fa',
		'niv': '0220b',
		'njcy': '0040a',
		'njcy': '0045a',
		'nlarr': '021cd',
		'nlarr': '0219a',
		'nldr': '02025',
		'nle': ['02266', '00338'],
		'nle': '02270',
		'nleftarrow': '021cd',
		'nleftarrow': '0219a',
		'nleftrightarrow': '021ce',
		'nleftrightarrow': '021ae',
		'nleq': '02270',
		'nleqq': ['02266', '00338'],
		'nleqslant': ['02a7d', '00338'],
		'nles': ['02a7d', '00338'],
		'nless': '0226e',
		'nll': ['022d8', '00338'],
		'nlsim': '02274',
		'nlt': ['0226a', '020d2'],
		'nlt': '0226e',
		'nltri': '022ea',
		'nltrie': '022ec',
		'nltv': ['0226a', '00338'],
		'nmid': '02224',
		'nobreak': '02060',
		'nonbreakingspace': '000a0',
		'nopf': '02115',
		'nopf': '1d55f',
		'not': '02aec',
		'not': '000ac',
		'notcongruent': '02262',
		'notcupcap': '0226d',
		'notdoubleverticalbar': '02226',
		'notelement': '02209',
		'notequal': '02260',
		'notequaltilde': ['02242', '00338'],
		'notexists': '02204',
		'notgreater': '0226f',
		'notgreaterequal': '02271',
		'notgreaterfullequal': ['02267', '00338'],
		'notgreatergreater': ['0226b', '00338'],
		'notgreaterless': '02279',
		'notgreaterslantequal': ['02a7e', '00338'],
		'notgreatertilde': '02275',
		'nothumpdownhump': ['0224e', '00338'],
		'nothumpequal': ['0224f', '00338'],
		'notin': '02209',
		'notindot': ['022f5', '00338'],
		'notine': ['022f9', '00338'],
		'notinva': '02209',
		'notinvb': '022f7',
		'notinvc': '022f6',
		'notlefttriangle': '022ea',
		'notlefttrianglebar': ['029cf', '00338'],
		'notlefttriangleequal': '022ec',
		'notless': '0226e',
		'notlessequal': '02270',
		'notlessgreater': '02278',
		'notlessless': ['0226a', '00338'],
		'notlessslantequal': ['02a7d', '00338'],
		'notlesstilde': '02274',
		'notnestedgreatergreater': ['02aa2', '00338'],
		'notnestedlessless': ['02aa1', '00338'],
		'notni': '0220c',
		'notniva': '0220c',
		'notnivb': '022fe',
		'notnivc': '022fd',
		'notprecedes': '02280',
		'notprecedesequal': ['02aaf', '00338'],
		'notprecedesslantequal': '022e0',
		'notreverseelement': '0220c',
		'notrighttriangle': '022eb',
		'notrighttrianglebar': ['029d0', '00338'],
		'notrighttriangleequal': '022ed',
		'notsquaresubset': ['0228f', '00338'],
		'notsquaresubsetequal': '022e2',
		'notsquaresuperset': ['02290', '00338'],
		'notsquaresupersetequal': '022e3',
		'notsubset': ['02282', '020d2'],
		'notsubsetequal': '02288',
		'notsucceeds': '02281',
		'notsucceedsequal': ['02ab0', '00338'],
		'notsucceedsslantequal': '022e1',
		'notsucceedstilde': ['0227f', '00338'],
		'notsuperset': ['02283', '020d2'],
		'notsupersetequal': '02289',
		'nottilde': '02241',
		'nottildeequal': '02244',
		'nottildefullequal': '02247',
		'nottildetilde': '02249',
		'notverticalbar': '02224',
		'npar': '02226',
		'nparallel': '02226',
		'nparsl': ['02afd', '020e5'],
		'npart': ['02202', '00338'],
		'npolint': '02a14',
		'npr': '02280',
		'nprcue': '022e0',
		'npre': ['02aaf', '00338'],
		'nprec': '02280',
		'npreceq': ['02aaf', '00338'],
		'nrarr': '021cf',
		'nrarr': '0219b',
		'nrarrc': ['02933', '00338'],
		'nrarrw': ['0219d', '00338'],
		'nrightarrow': '021cf',
		'nrightarrow': '0219b',
		'nrtri': '022eb',
		'nrtrie': '022ed',
		'nsc': '02281',
		'nsccue': '022e1',
		'nsce': ['02ab0', '00338'],
		'nscr': '1d4a9',
		'nscr': '1d4c3',
		'nshortmid': '02224',
		'nshortparallel': '02226',
		'nsim': '02241',
		'nsime': '02244',
		'nsimeq': '02244',
		'nsmid': '02224',
		'nspar': '02226',
		'nsqsube': '022e2',
		'nsqsupe': '022e3',
		'nsub': '02284',
		'nsube': ['02ac5', '00338'],
		'nsube': '02288',
		'nsubset': ['02282', '020d2'],
		'nsubseteq': '02288',
		'nsubseteqq': ['02ac5', '00338'],
		'nsucc': '02281',
		'nsucceq': ['02ab0', '00338'],
		'nsup': '02285',
		'nsupe': ['02ac6', '00338'],
		'nsupe': '02289',
		'nsupset': ['02283', '020d2'],
		'nsupseteq': '02289',
		'nsupseteqq': ['02ac6', '00338'],
		'ntgl': '02279',
		'ntilde': '000d1',
		'ntilde': '000f1',
		'ntlg': '02278',
		'ntriangleleft': '022ea',
		'ntrianglelefteq': '022ec',
		'ntriangleright': '022eb',
		'ntrianglerighteq': '022ed',
		'nu': '0039d',
		'nu': '003bd',
		'num': '00023',
		'numero': '02116',
		'numsp': '02007',
		'nvap': ['0224d', '020d2'],
		'nvdash': '022af',
		'nvdash': '022ae',
		'nvdash': '022ad',
		'nvdash': '022ac',
		'nvge': ['02265', '020d2'],
		'nvgt': ['0003e', '020d2'],
		'nvharr': '02904',
		'nvinfin': '029de',
		'nvlarr': '02902',
		'nvle': ['02264', '020d2'],
		'nvlt': ['0003c', '020d2'],
		'nvltrie': ['022b4', '020d2'],
		'nvrarr': '02903',
		'nvrtrie': ['022b5', '020d2'],
		'nvsim': ['0223c', '020d2'],
		'nwarhk': '02923',
		'nwarr': '021d6',
		'nwarr': '02196',
		'nwarrow': '02196',
		'nwnear': '02927',
		'oacute': '000d3',
		'oacute': '000f3',
		'oast': '0229b',
		'ocir': '0229a',
		'ocirc': '000d4',
		'ocirc': '000f4',
		'ocy': '0041e',
		'ocy': '0043e',
		'odash': '0229d',
		'odblac': '00150',
		'odblac': '00151',
		'odiv': '02a38',
		'odot': '02299',
		'odsold': '029bc',
		'oelig': '00152',
		'oelig': '00153',
		'ofcir': '029bf',
		'ofr': '1d512',
		'ofr': '1d52c',
		'ogon': '002db',
		'ograve': '000d2',
		'ograve': '000f2',
		'ogt': '029c1',
		'ohbar': '029b5',
		'ohm': '003a9',
		'oint': '0222e',
		'olarr': '021ba',
		'olcir': '029be',
		'olcross': '029bb',
		'oline': '0203e',
		'olt': '029c0',
		'omacr': '0014c',
		'omacr': '0014d',
		'omega': '003a9',
		'omega': '003c9',
		'omicron': '0039f',
		'omicron': '003bf',
		'omid': '029b6',
		'ominus': '02296',
		'oopf': '1d546',
		'oopf': '1d560',
		'opar': '029b7',
		'opencurlydoublequote': '0201c',
		'opencurlyquote': '02018',
		'operp': '029b9',
		'oplus': '02295',
		'or': '02a54',
		'or': '02228',
		'orarr': '021bb',
		'ord': '02a5d',
		'order': '02134',
		'orderof': '02134',
		'ordf': '000aa',
		'ordm': '000ba',
		'origof': '022b6',
		'oror': '02a56',
		'orslope': '02a57',
		'orv': '02a5b',
		'os': '024c8',
		'oscr': '1d4aa',
		'oscr': '02134',
		'oslash': '000d8',
		'oslash': '000f8',
		'osol': '02298',
		'otilde': '000d5',
		'otilde': '000f5',
		'otimes': '02a37',
		'otimes': '02297',
		'otimesas': '02a36',
		'ouml': '000d6',
		'ouml': '000f6',
		'ovbar': '0233d',
		'overbar': '0203e',
		'overbrace': '023de',
		'overbracket': '023b4',
		'overparenthesis': '023dc',
		'par': '02225',
		'para': '000b6',
		'parallel': '02225',
		'parsim': '02af3',
		'parsl': '02afd',
		'part': '02202',
		'partiald': '02202',
		'pcy': '0041f',
		'pcy': '0043f',
		'percnt': '00025',
		'period': '0002e',
		'permil': '02030',
		'perp': '022a5',
		'pertenk': '02031',
		'pfr': '1d513',
		'pfr': '1d52d',
		'phi': '003a6',
		'phi': '003c6',
		'phiv': '003d5',
		'phmmat': '02133',
		'phone': '0260e',
		'pi': '003a0',
		'pi': '003c0',
		'pitchfork': '022d4',
		'piv': '003d6',
		'planck': '0210f',
		'planckh': '0210e',
		'plankv': '0210f',
		'plus': '0002b',
		'plusacir': '02a23',
		'plusb': '0229e',
		'pluscir': '02a22',
		'plusdo': '02214',
		'plusdu': '02a25',
		'pluse': '02a72',
		'plusminus': '000b1',
		'plusmn': '000b1',
		'plussim': '02a26',
		'plustwo': '02a27',
		'pm': '000b1',
		'poincareplane': '0210c',
		'pointint': '02a15',
		'popf': '02119',
		'popf': '1d561',
		'pound': '000a3',
		'pr': '02abb',
		'pr': '0227a',
		'prap': '02ab7',
		'prcue': '0227c',
		'pre': '02ab3',
		'pre': '02aaf',
		'prec': '0227a',
		'precapprox': '02ab7',
		'preccurlyeq': '0227c',
		'precedes': '0227a',
		'precedesequal': '02aaf',
		'precedesslantequal': '0227c',
		'precedestilde': '0227e',
		'preceq': '02aaf',
		'precnapprox': '02ab9',
		'precneqq': '02ab5',
		'precnsim': '022e8',
		'precsim': '0227e',
		'prime': '02033',
		'prime': '02032',
		'primes': '02119',
		'prnap': '02ab9',
		'prne': '02ab5',
		'prnsim': '022e8',
		'prod': '0220f',
		'product': '0220f',
		'profalar': '0232e',
		'profline': '02312',
		'profsurf': '02313',
		'prop': '0221d',
		'proportion': '02237',
		'proportional': '0221d',
		'propto': '0221d',
		'prsim': '0227e',
		'prurel': '022b0',
		'pscr': '1d4ab',
		'pscr': '1d4c5',
		'psi': '003a8',
		'psi': '003c8',
		'puncsp': '02008',
		'qfr': '1d514',
		'qfr': '1d52e',
		'qint': '02a0c',
		'qopf': '0211a',
		'qopf': '1d562',
		'qprime': '02057',
		'qscr': '1d4ac',
		'qscr': '1d4c6',
		'quaternions': '0210d',
		'quatint': '02a16',
		'quest': '0003f',
		'questeq': '0225f',
		'quot': '00022',
		'quot': '00022',
		'raarr': '021db',
		'race': ['0223d', '00331'],
		'racute': '00154',
		'racute': '00155',
		'radic': '0221a',
		'raemptyv': '029b3',
		'rang': '027eb',
		'rang': '027e9',
		'rangd': '02992',
		'range': '029a5',
		'rangle': '027e9',
		'raquo': '000bb',
		'rarr': '021a0',
		'rarr': '021d2',
		'rarr': '02192',
		'rarrap': '02975',
		'rarrb': '021e5',
		'rarrbfs': '02920',
		'rarrc': '02933',
		'rarrfs': '0291e',
		'rarrhk': '021aa',
		'rarrlp': '021ac',
		'rarrpl': '02945',
		'rarrsim': '02974',
		'rarrtl': '02916',
		'rarrtl': '021a3',
		'rarrw': '0219d',
		'ratail': '0291c',
		'ratail': '0291a',
		'ratio': '02236',
		'rationals': '0211a',
		'rbarr': '02910',
		'rbarr': '0290f',
		'rbarr': '0290d',
		'rbbrk': '02773',
		'rbrace': '0007d',
		'rbrack': '0005d',
		'rbrke': '0298c',
		'rbrksld': '0298e',
		'rbrkslu': '02990',
		'rcaron': '00158',
		'rcaron': '00159',
		'rcedil': '00156',
		'rcedil': '00157',
		'rceil': '02309',
		'rcub': '0007d',
		'rcy': '00420',
		'rcy': '00440',
		'rdca': '02937',
		'rdldhar': '02969',
		'rdquo': '0201d',
		'rdquor': '0201d',
		'rdsh': '021b3',
		're': '0211c',
		'real': '0211c',
		'realine': '0211b',
		'realpart': '0211c',
		'reals': '0211d',
		'rect': '025ad',
		'reg': '000ae',
		'reg': '000ae',
		'reverseelement': '0220b',
		'reverseequilibrium': '021cb',
		'reverseupequilibrium': '0296f',
		'rfisht': '0297d',
		'rfloor': '0230b',
		'rfr': '0211c',
		'rfr': '1d52f',
		'rhar': '02964',
		'rhard': '021c1',
		'rharu': '021c0',
		'rharul': '0296c',
		'rho': '003a1',
		'rho': '003c1',
		'rhov': '003f1',
		'rightanglebracket': '027e9',
		'rightarrow': '02192',
		'rightarrow': '021d2',
		'rightarrow': '02192',
		'rightarrowbar': '021e5',
		'rightarrowleftarrow': '021c4',
		'rightarrowtail': '021a3',
		'rightceiling': '02309',
		'rightdoublebracket': '027e7',
		'rightdownteevector': '0295d',
		'rightdownvector': '021c2',
		'rightdownvectorbar': '02955',
		'rightfloor': '0230b',
		'rightharpoondown': '021c1',
		'rightharpoonup': '021c0',
		'rightleftarrows': '021c4',
		'rightleftharpoons': '021cc',
		'rightrightarrows': '021c9',
		'rightsquigarrow': '0219d',
		'righttee': '022a2',
		'rightteearrow': '021a6',
		'rightteevector': '0295b',
		'rightthreetimes': '022cc',
		'righttriangle': '022b3',
		'righttrianglebar': '029d0',
		'righttriangleequal': '022b5',
		'rightupdownvector': '0294f',
		'rightupteevector': '0295c',
		'rightupvector': '021be',
		'rightupvectorbar': '02954',
		'rightvector': '021c0',
		'rightvectorbar': '02953',
		'ring': '002da',
		'risingdotseq': '02253',
		'rlarr': '021c4',
		'rlhar': '021cc',
		'rlm': '0200f',
		'rmoust': '023b1',
		'rmoustache': '023b1',
		'rnmid': '02aee',
		'roang': '027ed',
		'roarr': '021fe',
		'robrk': '027e7',
		'ropar': '02986',
		'ropf': '0211d',
		'ropf': '1d563',
		'roplus': '02a2e',
		'rotimes': '02a35',
		'roundimplies': '02970',
		'rpar': '00029',
		'rpargt': '02994',
		'rppolint': '02a12',
		'rrarr': '021c9',
		'rrightarrow': '021db',
		'rsaquo': '0203a',
		'rscr': '0211b',
		'rscr': '1d4c7',
		'rsh': '021b1',
		'rsh': '021b1',
		'rsqb': '0005d',
		'rsquo': '02019',
		'rsquor': '02019',
		'rthree': '022cc',
		'rtimes': '022ca',
		'rtri': '025b9',
		'rtrie': '022b5',
		'rtrif': '025b8',
		'rtriltri': '029ce',
		'ruledelayed': '029f4',
		'ruluhar': '02968',
		'rx': '0211e',
		'sacute': '0015a',
		'sacute': '0015b',
		'sbquo': '0201a',
		'sc': '02abc',
		'sc': '0227b',
		'scap': '02ab8',
		'scaron': '00160',
		'scaron': '00161',
		'sccue': '0227d',
		'sce': '02ab4',
		'sce': '02ab0',
		'scedil': '0015e',
		'scedil': '0015f',
		'scirc': '0015c',
		'scirc': '0015d',
		'scnap': '02aba',
		'scne': '02ab6',
		'scnsim': '022e9',
		'scpolint': '02a13',
		'scsim': '0227f',
		'scy': '00421',
		'scy': '00441',
		'sdot': '022c5',
		'sdotb': '022a1',
		'sdote': '02a66',
		'searhk': '02925',
		'searr': '021d8',
		'searr': '02198',
		'searrow': '02198',
		'sect': '000a7',
		'semi': '0003b',
		'seswar': '02929',
		'setminus': '02216',
		'setmn': '02216',
		'sext': '02736',
		'sfr': '1d516',
		'sfr': '1d530',
		'sfrown': '02322',
		'sharp': '0266f',
		'shchcy': '00429',
		'shchcy': '00449',
		'shcy': '00428',
		'shcy': '00448',
		'shortdownarrow': '02193',
		'shortleftarrow': '02190',
		'shortmid': '02223',
		'shortparallel': '02225',
		'shortrightarrow': '02192',
		'shortuparrow': '02191',
		'shy': '000ad',
		'sigma': '003a3',
		'sigma': '003c3',
		'sigmaf': '003c2',
		'sigmav': '003c2',
		'sim': '0223c',
		'simdot': '02a6a',
		'sime': '02243',
		'simeq': '02243',
		'simg': '02a9e',
		'simge': '02aa0',
		'siml': '02a9d',
		'simle': '02a9f',
		'simne': '02246',
		'simplus': '02a24',
		'simrarr': '02972',
		'slarr': '02190',
		'smallcircle': '02218',
		'smallsetminus': '02216',
		'smashp': '02a33',
		'smeparsl': '029e4',
		'smid': '02223',
		'smile': '02323',
		'smt': '02aaa',
		'smte': '02aac',
		'smtes': ['02aac', '0fe00'],
		'softcy': '0042c',
		'softcy': '0044c',
		'sol': '0002f',
		'solb': '029c4',
		'solbar': '0233f',
		'sopf': '1d54a',
		'sopf': '1d564',
		'spades': '02660',
		'spadesuit': '02660',
		'spar': '02225',
		'sqcap': '02293',
		'sqcaps': ['02293', '0fe00'],
		'sqcup': '02294',
		'sqcups': ['02294', '0fe00'],
		'sqrt': '0221a',
		'sqsub': '0228f',
		'sqsube': '02291',
		'sqsubset': '0228f',
		'sqsubseteq': '02291',
		'sqsup': '02290',
		'sqsupe': '02292',
		'sqsupset': '02290',
		'sqsupseteq': '02292',
		'squ': '025a1',
		'square': '025a1',
		'square': '025a1',
		'squareintersection': '02293',
		'squaresubset': '0228f',
		'squaresubsetequal': '02291',
		'squaresuperset': '02290',
		'squaresupersetequal': '02292',
		'squareunion': '02294',
		'squarf': '025aa',
		'squf': '025aa',
		'srarr': '02192',
		'sscr': '1d4ae',
		'sscr': '1d4c8',
		'ssetmn': '02216',
		'ssmile': '02323',
		'sstarf': '022c6',
		'star': '022c6',
		'star': '02606',
		'starf': '02605',
		'straightepsilon': '003f5',
		'straightphi': '003d5',
		'strns': '000af',
		'sub': '022d0',
		'sub': '02282',
		'subdot': '02abd',
		'sube': '02ac5',
		'sube': '02286',
		'subedot': '02ac3',
		'submult': '02ac1',
		'subne': '02acb',
		'subne': '0228a',
		'subplus': '02abf',
		'subrarr': '02979',
		'subset': '022d0',
		'subset': '02282',
		'subseteq': '02286',
		'subseteqq': '02ac5',
		'subsetequal': '02286',
		'subsetneq': '0228a',
		'subsetneqq': '02acb',
		'subsim': '02ac7',
		'subsub': '02ad5',
		'subsup': '02ad3',
		'succ': '0227b',
		'succapprox': '02ab8',
		'succcurlyeq': '0227d',
		'succeeds': '0227b',
		'succeedsequal': '02ab0',
		'succeedsslantequal': '0227d',
		'succeedstilde': '0227f',
		'succeq': '02ab0',
		'succnapprox': '02aba',
		'succneqq': '02ab6',
		'succnsim': '022e9',
		'succsim': '0227f',
		'suchthat': '0220b',
		'sum': '02211',
		'sum': '02211',
		'sung': '0266a',
		'sup': '022d1',
		'sup': '02283',
		'sup1': '000b9',
		'sup2': '000b2',
		'sup3': '000b3',
		'supdot': '02abe',
		'supdsub': '02ad8',
		'supe': '02ac6',
		'supe': '02287',
		'supedot': '02ac4',
		'superset': '02283',
		'supersetequal': '02287',
		'suphsol': '027c9',
		'suphsub': '02ad7',
		'suplarr': '0297b',
		'supmult': '02ac2',
		'supne': '02acc',
		'supne': '0228b',
		'supplus': '02ac0',
		'supset': '022d1',
		'supset': '02283',
		'supseteq': '02287',
		'supseteqq': '02ac6',
		'supsetneq': '0228b',
		'supsetneqq': '02acc',
		'supsim': '02ac8',
		'supsub': '02ad4',
		'supsup': '02ad6',
		'swarhk': '02926',
		'swarr': '021d9',
		'swarr': '02199',
		'swarrow': '02199',
		'swnwar': '0292a',
		'szlig': '000df',
		'tab': '00009',
		'target': '02316',
		'tau': '003a4',
		'tau': '003c4',
		'tbrk': '023b4',
		'tcaron': '00164',
		'tcaron': '00165',
		'tcedil': '00162',
		'tcedil': '00163',
		'tcy': '00422',
		'tcy': '00442',
		'tdot': '020db',
		'telrec': '02315',
		'tfr': '1d517',
		'tfr': '1d531',
		'there4': '02234',
		'therefore': '02234',
		'therefore': '02234',
		'theta': '00398',
		'theta': '003b8',
		'thetasym': '003d1',
		'thetav': '003d1',
		'thickapprox': '02248',
		'thicksim': '0223c',
		'thickspace': ['0205f', '0200a'],
		'thinsp': '02009',
		'thinspace': '02009',
		'thkap': '02248',
		'thksim': '0223c',
		'thorn': '000de',
		'thorn': '000fe',
		'tilde': '0223c',
		'tilde': '002dc',
		'tildeequal': '02243',
		'tildefullequal': '02245',
		'tildetilde': '02248',
		'times': '000d7',
		'timesb': '022a0',
		'timesbar': '02a31',
		'timesd': '02a30',
		'tint': '0222d',
		'toea': '02928',
		'top': '022a4',
		'topbot': '02336',
		'topcir': '02af1',
		'topf': '1d54b',
		'topf': '1d565',
		'topfork': '02ada',
		'tosa': '02929',
		'tprime': '02034',
		'trade': '02122',
		'trade': '02122',
		'triangle': '025b5',
		'triangledown': '025bf',
		'triangleleft': '025c3',
		'trianglelefteq': '022b4',
		'triangleq': '0225c',
		'triangleright': '025b9',
		'trianglerighteq': '022b5',
		'tridot': '025ec',
		'trie': '0225c',
		'triminus': '02a3a',
		'tripledot': '020db',
		'triplus': '02a39',
		'trisb': '029cd',
		'tritime': '02a3b',
		'trpezium': '023e2',
		'tscr': '1d4af',
		'tscr': '1d4c9',
		'tscy': '00426',
		'tscy': '00446',
		'tshcy': '0040b',
		'tshcy': '0045b',
		'tstrok': '00166',
		'tstrok': '00167',
		'twixt': '0226c',
		'twoheadleftarrow': '0219e',
		'twoheadrightarrow': '021a0',
		'uacute': '000da',
		'uacute': '000fa',
		'uarr': '0219f',
		'uarr': '021d1',
		'uarr': '02191',
		'uarrocir': '02949',
		'ubrcy': '0040e',
		'ubrcy': '0045e',
		'ubreve': '0016c',
		'ubreve': '0016d',
		'ucirc': '000db',
		'ucirc': '000fb',
		'ucy': '00423',
		'ucy': '00443',
		'udarr': '021c5',
		'udblac': '00170',
		'udblac': '00171',
		'udhar': '0296e',
		'ufisht': '0297e',
		'ufr': '1d518',
		'ufr': '1d532',
		'ugrave': '000d9',
		'ugrave': '000f9',
		'uhar': '02963',
		'uharl': '021bf',
		'uharr': '021be',
		'uhblk': '02580',
		'ulcorn': '0231c',
		'ulcorner': '0231c',
		'ulcrop': '0230f',
		'ultri': '025f8',
		'umacr': '0016a',
		'umacr': '0016b',
		'uml': '000a8',
		'underbar': '0005f',
		'underbrace': '023df',
		'underbracket': '023b5',
		'underparenthesis': '023dd',
		'union': '022c3',
		'unionplus': '0228e',
		'uogon': '00172',
		'uogon': '00173',
		'uopf': '1d54c',
		'uopf': '1d566',
		'uparrow': '02191',
		'uparrow': '021d1',
		'uparrow': '02191',
		'uparrowbar': '02912',
		'uparrowdownarrow': '021c5',
		'updownarrow': '02195',
		'updownarrow': '021d5',
		'updownarrow': '02195',
		'upequilibrium': '0296e',
		'upharpoonleft': '021bf',
		'upharpoonright': '021be',
		'uplus': '0228e',
		'upperleftarrow': '02196',
		'upperrightarrow': '02197',
		'upsi': '003d2',
		'upsi': '003c5',
		'upsih': '003d2',
		'upsilon': '003a5',
		'upsilon': '003c5',
		'uptee': '022a5',
		'upteearrow': '021a5',
		'upuparrows': '021c8',
		'urcorn': '0231d',
		'urcorner': '0231d',
		'urcrop': '0230e',
		'uring': '0016e',
		'uring': '0016f',
		'urtri': '025f9',
		'uscr': '1d4b0',
		'uscr': '1d4ca',
		'utdot': '022f0',
		'utilde': '00168',
		'utilde': '00169',
		'utri': '025b5',
		'utrif': '025b4',
		'uuarr': '021c8',
		'uuml': '000dc',
		'uuml': '000fc',
		'uwangle': '029a7',
		'vangrt': '0299c',
		'varepsilon': '003f5',
		'varkappa': '003f0',
		'varnothing': '02205',
		'varphi': '003d5',
		'varpi': '003d6',
		'varpropto': '0221d',
		'varr': '021d5',
		'varr': '02195',
		'varrho': '003f1',
		'varsigma': '003c2',
		'varsubsetneq': ['0228a', '0fe00'],
		'varsubsetneqq': ['02acb', '0fe00'],
		'varsupsetneq': ['0228b', '0fe00'],
		'varsupsetneqq': ['02acc', '0fe00'],
		'vartheta': '003d1',
		'vartriangleleft': '022b2',
		'vartriangleright': '022b3',
		'vbar': '02aeb',
		'vbar': '02ae8',
		'vbarv': '02ae9',
		'vcy': '00412',
		'vcy': '00432',
		'vdash': '022ab',
		'vdash': '022a9',
		'vdash': '022a8',
		'vdash': '022a2',
		'vdashl': '02ae6',
		'vee': '022c1',
		'vee': '02228',
		'veebar': '022bb',
		'veeeq': '0225a',
		'vellip': '022ee',
		'verbar': '02016',
		'verbar': '0007c',
		'vert': '02016',
		'vert': '0007c',
		'verticalbar': '02223',
		'verticalline': '0007c',
		'verticalseparator': '02758',
		'verticaltilde': '02240',
		'verythinspace': '0200a',
		'vfr': '1d519',
		'vfr': '1d533',
		'vltri': '022b2',
		'vnsub': ['02282', '020d2'],
		'vnsup': ['02283', '020d2'],
		'vopf': '1d54d',
		'vopf': '1d567',
		'vprop': '0221d',
		'vrtri': '022b3',
		'vscr': '1d4b1',
		'vscr': '1d4cb',
		'vsubne': ['02acb', '0fe00'],
		'vsubne': ['0228a', '0fe00'],
		'vsupne': ['02acc', '0fe00'],
		'vsupne': ['0228b', '0fe00'],
		'vvdash': '022aa',
		'vzigzag': '0299a',
		'wcirc': '00174',
		'wcirc': '00175',
		'wedbar': '02a5f',
		'wedge': '022c0',
		'wedge': '02227',
		'wedgeq': '02259',
		'weierp': '02118',
		'wfr': '1d51a',
		'wfr': '1d534',
		'wopf': '1d54e',
		'wopf': '1d568',
		'wp': '02118',
		'wr': '02240',
		'wreath': '02240',
		'wscr': '1d4b2',
		'wscr': '1d4cc',
		'xcap': '022c2',
		'xcirc': '025ef',
		'xcup': '022c3',
		'xdtri': '025bd',
		'xfr': '1d51b',
		'xfr': '1d535',
		'xharr': '027fa',
		'xharr': '027f7',
		'xi': '0039e',
		'xi': '003be',
		'xlarr': '027f8',
		'xlarr': '027f5',
		'xmap': '027fc',
		'xnis': '022fb',
		'xodot': '02a00',
		'xopf': '1d54f',
		'xopf': '1d569',
		'xoplus': '02a01',
		'xotime': '02a02',
		'xrarr': '027f9',
		'xrarr': '027f6',
		'xscr': '1d4b3',
		'xscr': '1d4cd',
		'xsqcup': '02a06',
		'xuplus': '02a04',
		'xutri': '025b3',
		'xvee': '022c1',
		'xwedge': '022c0',
		'yacute': '000dd',
		'yacute': '000fd',
		'yacy': '0042f',
		'yacy': '0044f',
		'ycirc': '00176',
		'ycirc': '00177',
		'ycy': '0042b',
		'ycy': '0044b',
		'yen': '000a5',
		'yfr': '1d51c',
		'yfr': '1d536',
		'yicy': '00407',
		'yicy': '00457',
		'yopf': '1d550',
		'yopf': '1d56a',
		'yscr': '1d4b4',
		'yscr': '1d4ce',
		'yucy': '0042e',
		'yucy': '0044e',
		'yuml': '00178',
		'yuml': '000ff',
		'zacute': '00179',
		'zacute': '0017a',
		'zcaron': '0017d',
		'zcaron': '0017e',
		'zcy': '00417',
		'zcy': '00437',
		'zdot': '0017b',
		'zdot': '0017c',
		'zeetrf': '02128',
		'zerowidthspace': '0200b',
		'zeta': '00396',
		'zeta': '003b6',
		'zfr': '02128',
		'zfr': '1d537',
		'zhcy': '00416',
		'zhcy': '00436',
		'zigrarr': '021dd',
		'zopf': '02124',
		'zopf': '1d56b',
		'zscr': '1d4b5',
		'zscr': '1d4cf',
		'zwj': '0200d',
		'zwnj': '0200c'
	};

	// Load map:
	for (var entityName in entityMapping) {
		var codePoints = entityMapping[entityName];

		if (Array.isArray(codePoints)) {
			entityMapping[entityName] = String.fromCharCode(parseInt(Number('0x' + codePoints[0]), 10), parseInt(Number('0x' + codePoints[1]), 10));
		} else {
			entityMapping[entityName] = String.fromCharCode(parseInt(Number('0x' + codePoints), 10));
		}
	}

	// End of scope.
	return htmlToText;
})();
