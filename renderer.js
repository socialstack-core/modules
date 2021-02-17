var preact = window.Preact || window.preact || window.React;

function _getContentTypeIdFactory(){
	const _hash1 = ((5381 << 16) + 5381)|0;
	const floor = Math.floor;

	/*
		Converts a typeName like "BlogPost" to its numeric content type ID.
		If porting this, instead take a look at the C# version in ContentTypes.cs. 
		Most of the stuff here is for forcing JS to do integer arithmetic.
	*/
	return function(typeName) {
		typeName = typeName.toLowerCase();
		var hash1 = _hash1;
		var hash2 = hash1;
		
		for (var i = 0; i < typeName.length; i += 2)
		{
			var s1 = ~~floor(hash1 << 5);
			hash1 = ~~floor(s1 + hash1);
			hash1 = hash1 ^ typeName.charCodeAt(i);
			if (i == typeName.length - 1)
				break;
			
			s1 = ~~floor(hash2 << 5);
			hash2 = ~~floor(s1 + hash2);
			hash2 = hash2 ^ typeName.charCodeAt(i+1);
		}
		
		var result = ~~floor(Math.imul(hash2, 1566083941));
		result = ~~floor(hash1 + result);
		return result;
	};
}

const getContentTypeId = _getContentTypeIdFactory();

/*
* Originates from preact-render-to-string.
* Custom source version is to make it async friendly such that it can wait on fetch 
* requests without wasting large amounts of effort building strings to just throw them away.
*/

function reactPreRender(){
	
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

	const noop = () => {};

	/** Render Preact JSX + Components to an HTML string.
	 *	@name render
	 *	@function
	 *	@param {VNode} vnode	JSX VNode to render.
	 *	@param {Object} [context={}]	Optionally pass an initial context object through the render path.
	 *	@param {Object} [options={}]	Rendering options
	 *	@param {Boolean} [options.shallow=false]	If `true`, renders nested Components as HTML elements (`<Foo a="b" />`).
	 *	@param {Boolean} [options.xml=false]		If `true`, uses self-closing tags for elements without children.
	 *	@param {Boolean} [options.pretty=false]		If `true`, adds whitespace for readability
	 *	@param {RegEx|undefined} [options.voidElements]       RegeEx that matches elements that are considered void (self-closing)
	 */
	renderToString.render = renderToString;

	/** Only render elements, leaving Components inline as `<ComponentName ... />`.
	 *	This method is just a convenience alias for `render(vnode, context, { shallow:true })`
	 *	@name shallow
	 *	@function
	 *	@param {VNode} vnode	JSX VNode to render.
	 *	@param {Object} [context={}]	Optionally pass an initial context object through the render path.
	 */
	let shallowRender = (vnode, context) => renderToString(vnode, context, SHALLOW);

	const EMPTY_ARR = [];
	async function renderToString(vnode, context, opts) {
		const res = await _renderToString(vnode, context, opts);
		// options._commit, we don't schedule any effects in this library right now,
		// so we can pass an empty queue to this hook.
		if (options.__c) options.__c(vnode, EMPTY_ARR);
		return res;
	}

	/** The default export is an alias of `render()`. */
	async function _renderToString(vnode, context, opts, inner, isSvgMode, selectValue) {
		if (vnode == null || typeof vnode === 'boolean') {
			return '';
		}

		// wrap array nodes in Fragment
		if (Array.isArray(vnode)) {
			vnode = createElement(Fragment, null, vnode);
		}

		let nodeName = vnode.type,
			props = vnode.props,
			isComponent = false;
		context = context || {};
		opts = opts || {};

		let pretty = opts.pretty,
			indentChar = pretty && typeof pretty === 'string' ? pretty : '\t';

		// #text nodes
		if (typeof vnode !== 'object' && !nodeName) {
			return encodeEntities(vnode);
		}

		// components
		if (typeof nodeName === 'function') {
			isComponent = true;
			if (opts.shallow && (inner || opts.renderRootComponent === false)) {
				nodeName = getComponentName(nodeName);
			} else if (nodeName === Fragment) {
				let rendered = '';
				let children = [];
				getChildren(children, vnode.props.children);

				for (let i = 0; i < children.length; i++) {
					rendered +=
						(i > 0 && pretty ? '\n' : '') +
						await _renderToString(
							children[i],
							context,
							opts,
							opts.shallowHighOrder !== false,
							isSvgMode,
							selectValue
						);
				}
				return rendered;
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
					_currentContext = cctx;
					rendered = nodeName.call(vnode.__c, props, cctx);
					_currentContext = null;
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
					_currentContext = cctx;
					c = vnode.__c = new nodeName(props, cctx);
					_currentContext = null;
					c.__v = vnode;
					// turn off stateful re-rendering:
					c._dirty = c.__d = true;
					c.props = props;
					if (c.state == null) {
						c.state = {};
					} else {
						// ------- Socialstack extension -----
						// Does the state contain any thennable objects (promises)?
						// If so, we'll await them before proceeding.
						var promisesToWaitFor = null;
						
						for(var field in c.state){
							var fieldValue = c.state[field];
							if(fieldValue && fieldValue.then){
								// Thennable. Wait for it.
								if(!promisesToWaitFor){
									promisesToWaitFor = [];
								}
								
								promisesToWaitFor.push(fieldValue.then(result => c.state[field] = result));
							}
						}
						
						if(promisesToWaitFor){
							await Promise.all(promisesToWaitFor);
						}
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
				
				return await _renderToString(
					rendered,
					context,
					opts,
					opts.shallowHighOrder !== false,
					isSvgMode,
					selectValue
				);
			}
		}
		
		// render JSX to HTML
		let s = '',
			propChildren,
			html;

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
					s += hooked;
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
						// in non-xml mode, allow boolean attributes
						if (!opts || !opts.xml) {
							s += ' ' + name;
							continue;
						}
					}

					if (name === 'value') {
						if (nodeName === 'select') {
							selectValue = v;
							continue;
						} else if (nodeName === 'option' && selectValue == v) {
							s += ` selected`;
						}
					}
					s += ` ${name}="${encodeEntities(v)}"`;
				}
			}
		}

		// account for >1 multiline attribute
		if (pretty) {
			let sub = s.replace(/^\n\s*/, ' ');
			if (sub !== s && !~sub.indexOf('\n')) s = sub;
			else if (pretty && ~s.indexOf('\n')) s += '\n';
		}

		s = `<${nodeName}${s}>`;
		if (String(nodeName).match(/[\s\n\\/='"\0<>]/))
			throw new Error(`${nodeName} is not a valid HTML tag name in ${s}`);

		let isVoid =
			String(nodeName).match(VOID_ELEMENTS) ||
			(opts.voidElements && String(nodeName).match(opts.voidElements));
		let pieces = [];

		let children;
		if (html) {
			// if multiline, indent.
			if (pretty && isLargeString(html)) {
				html = '\n' + indentChar + indent(html, indentChar);
			}
			s += html;
		} else if (
			propChildren != null &&
			getChildren((children = []), propChildren).length
		) {
			let hasLarge = pretty && ~s.indexOf('\n');
			let lastWasText = false;

			for (let i = 0; i < children.length; i++) {
				let child = children[i];

				if (child != null && child !== false) {
					let childSvgMode =
							nodeName === 'svg'
								? true
								: nodeName === 'foreignObject'
								? false
								: isSvgMode,
						ret = await _renderToString(
							child,
							context,
							opts,
							true,
							childSvgMode,
							selectValue
						);

					if (pretty && !hasLarge && isLargeString(ret)) hasLarge = true;

					// Skip if we received an empty string
					if (ret) {
						if (pretty) {
							let isText = ret.length > 0 && ret[0] != '<';

							// We merge adjacent text nodes, otherwise each piece would be printed
							// on a new line.
							if (lastWasText && isText) {
								pieces[pieces.length - 1] += ret;
							} else {
								pieces.push(ret);
							}

							lastWasText = isText;
						} else {
							pieces.push(ret);
						}
					}
				}
			}
			if (pretty && hasLarge) {
				for (let i = pieces.length; i--; ) {
					pieces[i] = '\n' + indentChar + indent(pieces[i], indentChar);
				}
			}
		}

		if (pieces.length || html) {
			s += pieces.join('');
		} else if (opts && opts.xml) {
			return s.substring(0, s.length - 1) + ' />';
		}

		if (isVoid && !children && !html) {
			s = s.replace(/>$/, ' />');
		} else {
			if (pretty && ~s.indexOf('\n')) s += '\n';
			s += `</${nodeName}>`;
		}

		return s;
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
			for (let i = UNNAMED.length; i--; ) {
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
var _Canvas = getModule('UI/Canvas/Canvas.js').default;
var _contentModule = getModule("UI/Content/Content.js").default;
var _currentContext = null;

// Stub:
pageRouter = {
	state: {}
};

function fetch(url, opts) {
	// Reject all fetch requests.
	return Promise.reject({ error: 'This request is unsupported serverside.', serverside: true, url });
}

// Overwrite content.get, content.getCache etc:
_contentModule.get = (type, id) => {
	return Promise.reject({
		error: 'This request is unsupported serverside. Use getCached in your constructor, and Content.get from componentDidMount/ componentDidUpdate/ useEffect.',
		serverside: true,
		type,
		id
	});
};

_contentModule.list = (type, filter) => {
	return Promise.reject({
		error: 'This request is unsupported serverside. Use listCached in your constructor, and Content.list from componentDidMount/ componentDidUpdate/ useEffect.',
		serverside: true,
		type,
		filter
	});
};

_contentModule.getCached = (type, id) => {
	if (!_currentContext) {
		// Invalid call site - constructors only.
		return null;
	}

	var cctx = _currentContext;

	return new Promise(s => {
		document.getContentById(cctx.app.apiContext, getContentTypeId(type), parseInt(id), jsonResponse => {
			if (cctx.__contextualData) {
				// We're tracking contextual data
				cctx.__contextualData += "sscache._a(\'" + type + "\',\'" + id + "\'," + jsonResponse + ");";
			}
			s(JSON.parse(jsonResponse));
		});
	});
};

_contentModule.listCached = (type, filter) => {
	if (!_currentContext) {
		// Invalid call site - constructors only.
		return null;
	}

	var filterJson = JSON.stringify(filter);
	var cctx = _currentContext;

	return new Promise(s => {
		document.getContentsByFilter(cctx.app.apiContext, getContentTypeId(type), filter, jsonResponse => {
			if (cctx.__contextualData) {
				// We're tracking contextual data
				cctx.__contextualData += "sscache._a(\'" + type + "\',\'" + filterJson + "\'," + jsonResponse + ");";
			}
			s(JSON.parse(jsonResponse));
		});
	});
};

function renderCanvas(bodyJson, apiContext, publicApiContextJson, url, postData, trackContextualData){
	
	// Stub app state:
	app = {
		apiContext,
		state: {
			url,
			...JSON.parse(publicApiContextJson)
		}
	};
	
	var canvas = preact.createElement(_Canvas, { children: bodyJson});

	var context = {
		app,
		postData
	};

	if (trackContextualData)
	{
		// Only construct this string if we actually need it (email rendering does not, for example).
		// The contextual data being pulled in during this render. Must use this to rehydrate the caches when the site is done loading.
		context.__contextualData = 'gsInit=' + publicApiContextJson + ';sscache={_a: function(t,i,a){if(!sscache[t]){sscache[t]={}}sscache[t][i]=a}};';
	};

	// Returns a promise.
	return renderToString(canvas, context).then(body => {

		return {
			body,
			data: trackContextualData ? context.__contextualData : ''
		};

	});
}