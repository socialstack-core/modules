import webRequest from 'UI/Functions/WebRequest';
import Canvas from 'UI/Canvas';
import { Router } from 'UI/Session';
import getBuildDate from 'UI/Functions/GetBuildDate';

try{
	var preloadedPages = global.getModule('UI/PreloadedPages/PreloadedPages.json');
}
catch{
	var preloadedPages = null;
}

const { hashRouter, location } = global;

const initialUrl = hashRouter ? location.hash?.substring(1) ?? "/" : `${location.pathname}${location.search}`;

// Initial event:
const initState = global.pgState || {};
triggerEvent(initState.page);

function triggerEvent(pgInfo) {
	if(pgInfo){
		var e;
		if(typeof(Event) === 'function') {
			e = new Event('xpagechange');
		}else{
			e = document.createEvent('Event');
			e.initEvent('xpagechange', true, true);
		}
		e.pageInfo = pgInfo;
		global.dispatchEvent(e);
	}
}

function replaceTokens(str, res) {
	var {page, po} = res;

	if(str == null)  {
		return str;
	}

	if(po != null) {
		var mode = 0; // 0 = text, 1 = inadie a {token.field}
		var tokens = [];
		var storedIndex = 0;

		for(var i = 0; i < str.length; i++) {
			var currentChar = str[i];
			if(mode == 0) {
				if(currentChar == "{") {
					// now in a token.
					mode = 1;
					storedIndex = i;
				}
			} else if (mode == 1) {
				if (currentChar == "}") {
					// we have the end of the token, let's get it.
					var token = str.substring(storedIndex, i+1);
					tokens.push(token);
					mode = 0;
				}
			}
		}

		tokens.forEach(token => {
			// remove the brackets
			var noBrackets = token.substring(1, token.length - 1);

			// Let's split it - to get content and its field.
			var contentAndField = noBrackets.split(".");

			// Is this valid?
			if(contentAndField.length != 2) {
				// nope, no replacement or further action for this token.
				return;
			}

			// This should have a content and field since its 2 pieces.
			var content = contentAndField[0];
			var field = contentAndField[1];

			// does the content match the primary object type?
			// TODO: we will probably need handling for tokens other than primary objects in the future. 
			if(!po.type || content.toLowerCase() != po.type.toLowerCase()) {
				return;
			}

			// Does the field exist on the primary object?
			if(!po[field] == null) {
				return;
			}

			var value = po[field];

			str = str.replace(token, value.toString());
		});
	}

	return str;
}

export default () => {
	var [pageState, setPage] = React.useState({url: initialUrl, ...initState});
  
	function go(url) {
		global.history.pushState({}, "", global.storedToken ? '#' + url : url);
		document.body.parentNode.scrollTop=0;
		
		webRequest("page/state", {
			url,
			version: getBuildDate().timestamp
		}).then(res => {
			setPage({url, page: res.json});
			triggerEvent(res.json);
		});
	}
	
	const onPopState = (e) => {
		document.body.parentNode.scrollTo(0,0);
		go(document.location.pathname);
	}
	
	const onLinkClick = (e) => {
		if(e.button != 0 || e.defaultPrevented){
			// Browser default action for right/ middle clicks
			return;
		}
		var cur = e.target;
		while(cur && cur != document){
			if(cur.nodeName == 'A'){
				var href = cur.getAttribute('href'); // cur.href is absolute
				if(cur.getAttribute('target') || cur.getAttribute('download')){
					return;
				}
				
				if(href && href.length){
					var pn = document.location.pathname;
					var isOnExternPage = pn.indexOf('/en-admin') == 0 || pn.indexOf('/v1') == 0;
					var targetIsExternPage = href[0] == '/' ? (href.indexOf('/en-admin') == 0 || href.indexOf('/v1') == 0) : isOnExternPage;
					
					if(href.indexOf(':') != -1 || (href[0] == '/' && (href.length>1 && href[1] == '/'))){
						return;
					}
					if(targetIsExternPage == isOnExternPage){
						e.preventDefault();
						go(cur.pathname + cur.search);
					}
					return;
				}
			}
			cur = cur.parentNode;
		}
	}
	
	React.useEffect(() => {
		document.addEventListener("popstate", onPopState);
		document.addEventListener("click", onLinkClick);
		
		return () => {
			document.removeEventListener("popstate", onPopState);
			document.removeEventListener("click", onLinkClick);
		};
	});
	
	var { page } = pageState;
	
	React.useEffect(() => {
		if(page && page.title){
			// Does our title have any tokens in it?
			console.log(pageState);
			document.title = replaceTokens(page.title, pageState);
		}
	});
	
	return <Router.Provider
			value={{
				pageState,
				setPage: go
			}}
		>
		{
			page ? <Canvas>
				{page.bodyJson}
			</Canvas> : null
		}
	</Router.Provider>;
}
