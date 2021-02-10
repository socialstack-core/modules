import webRequest from 'UI/Functions/WebRequest';
import Canvas from 'UI/Canvas';

try{
	var preloadedPages = global.getModule('UI/PreloadedPages/PreloadedPages.json');
}
catch{
	var preloadedPages = null;
}

export default class PageRouter extends React.Component{
	
	go(url){
		if(global.storedToken){
			global.history.pushState({}, "", '#' + url);
		}else{
			global.history.pushState({}, "", url);
		}
		document.body.parentNode.scrollTop=0;
		this.props.onNavigate && this.props.onNavigate(url);
	}
	
	constructor(props){
		super(props);
		this.state = {
			tokens: {} // Stores e.g. :id. Never null.
		};
		global.pageRouter = this;
		this.onLinkClick = this.onLinkClick.bind(this);
		this.onPopState = this.onPopState.bind(this);
		
		var {loadingUser} = global.app.state;
		
		if(loadingUser){
			loadingUser.then(res => {
				this.role = res && res.json.role ? res.json.role.id : undefined;
			})
		}
	}
	
	makeRequest(){
		return webRequest("page/list").then(response => {
			var pages = response.json.results;
			this.pages(pages, false);
		}).catch(err => {
			// No additional pages are available.
			console.log(err);
		})
	}

	pages(pages, isCached){
		// Build route map and ID maps next.
				
		var idMap = {};
		var contentMap = {};
		var adminContentMap = {};
				
		pages.forEach(page => {
			idMap[''+page.id] = page;
		});
		
		var rootPage = {children: {}, tokenNames: []};
		
		pages.forEach(page => {
			
			var url = page.url;
			
			if(!url){
				return;
			}

			// Does our url contain '{' ?
			if(url.indexOf("{") != -1) {
				// We need to handle it as a content type. Let's get the type and piecesAfter value. 
				var urlExploded = url.split("/");

				var contentType = null;
				var parameter = null;
				var piecesAfter = 0;

				// Let's loop backwards through our exploded url
				for (var i = urlExploded.length -1; i >= 0; i--) {
					var piece = urlExploded[i];

					// Is our piece the content?
					if(piece.charAt(0) == "{" && piece.charAt(piece.length-1) == "}") {
						// yes, shed the curlies.
						contentAndParam = piece.slice(1, -1);

						// Let's split on the "." now.
						contentAndParamExplode = contentAndParam.split(".");
						
						// do we have two pieces? If not, this is not right.
						if(contentAndParamExplode.length == 2) {
							// good to go. 
							// what is the content type and param?
							contentType = contentAndParamExplode[0].toLowerCase();
							parameter = contentAndParamExplode[1].toLowerCase();
							
							//Nothing else needed in this loop now.
							break;
						}
					}

					piecesAfter++;
				}

				// Did we get a content type and param out of this?
				if(contentType && parameter) {
					
					// What's it's scope?
					if(url.includes("/en-admin/")) {
						// The scope is admin - do we have an existing entry for this content?
						if(!adminContentMap[contentType] || adminContentMap[contentType].piecesAfter > piecesAfter) {
							adminContentMap[contentType] = {
								parameter: parameter,
								piecesAfter: piecesAfter,
								page: page,
								url: url
							};
						}
					} else {
						// For now, we will assume the scope is the UI otherwise.
						if(!contentMap[contentType] || contentMap[contentType].piecesAfter > piecesAfter) {
							contentMap[contentType] = {
								parameter: parameter,
								piecesAfter: piecesAfter,
								page: page,
								url: url
							};
						} 
					}
				}
			}

			
			if(url.length && url[0] == '/'){
				url = url.substring(1);
			}
			
			if(url.length && url[url.length-1] == '/'){
				url = url.substring(0, url.length-1);
			}
			
			page.url = url;
			
			// URL parts:
			
			var pg = rootPage;
			
			if(url.length){
				var parts = url.split('/');
				
				for(var i=0;i<parts.length;i++){
					
					var part = parts[i];
					var token;
					if (part.length)
					{
						if (part[0] == ':')
						{
							token = part.substring(1);
						}
						else if (part[0] == '{')
						{
							token = (part[part.length - 1] == '}') ? part.substring(1, part.length - 1) : part.substring(1);
						}
					}

					if(token){
						// Anything. Treat these tokens as *:
						part = '*';
					}

					var next = pg.children[part];
					
					if(!next){
						pg.children[part] = next = {children: {}, tokenNames: []};
						if(token){
							next.tokenNames.push(token);
						}
					}
					
					pg = next;
				}
			}
			
			pg.page = page;
			
		});
		
		this.getPageRedirected(rootPage, idMap, this.props.url).then(pageInfo => {
			this.trigger(pageInfo);
			if(isCached && pageInfo.page.url == "" && this.props.url != "/"){
				this.makeRequest();
			}
			else{
				this.setState({
					pages,
					rootPage,
					idMap,
					contentMap,
					adminContentMap,
					...pageInfo
				});
			}
		})
	}
	
	onLinkClick(e){
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
						this.go(cur.pathname + cur.search);
					}
					return;
				}
			}
			cur = cur.parentNode;
		}
	}
	
	onPopState(e){
		document.body.parentNode.scrollTo(0,0);
		this.props.onNavigate && this.props.onNavigate(document.location.pathname);
	}
	
	componentDidMount(){
		global.addEventListener("popstate", this.onPopState);
		
		document.addEventListener("click", this.onLinkClick);
		
		if(global.pageRouterData){
			this.pages(global.pageRouterData, true);
		}else if(!preloadedPages){
			//console.log(this.props.url);
			this.makeRequest();
		}
		else {
			this.pages(preloadedPages.results || preloadedPages, true);
		}
	}
	
	trigger(pgInfo){
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
	
	getRole(){
		var { role } = global.app.state;
		return role ? role.id : undefined;
	}
	
	componentWillReceiveProps(props){
		var role = this.getRole();
		if(!preloadedPages && this.role!==undefined && role!==undefined && this.role != role){
			this.role = role;
			// User role changed (they logged in) - get page list again, then change page:
			this.makeRequest().then(() => this.setupPage(props));
		}else{
			// Only setup if page actually changed.
			if(props.url != this.props.url){
				this.setupPage(props);
			}
		}
	}
	
	setupPage(props){
		// page and tokens
		this.getPageRedirected(this.state.rootPage, this.state.idMap, props.url).then((pageInfo) => {
			this.trigger(pageInfo);
			this.setState(pageInfo);
		});
	}
	
	componentWillUnmount(){
		global.removeEventListener("popstate", this.onPopState);
		document.removeEventListener("click", this.onLinkClick);
	}
	
	resolveRedirect(pageId, idMap, redirectCount){
		if(redirectCount > 20){
			// Probably a loop - kill it here.
			throw new Error("Redirect loop detected involving page #" + pageId);
		}
		
		var pageInfo = idMap[pageId];
		if(pageInfo == null || !pageInfo.bodyJson){
			return null;
		}
		
		// Is this also a redirect?
		var json;
		
		if(typeof pageInfo.bodyJson == "string"){
			try{
				json = JSON.parse(pageInfo.bodyJson);
			}catch(e){
				console.log('Failed to load page JSON: ', pageInfo.bodyJson);
				console.error(e);
			}
		}else{
			json = pageInfo.bodyJson;
		}
		
		if(!json){
			json = {};
		}
		
		if(json.redirect){
			// Redirecting to another page. It's always identified by ID.
			return this.resolveRedirect(json.redirect, idMap, redirectCount+1);
		}
		
		return pageInfo;
	}
	
	getPageRedirected(rootPage, idMap, url){
		// The same as getPage only this one also resolves redirect: PageId.
		return new Promise((success, reject) => {
			var pageAndState = this.getPage(rootPage, url);
			
			if(!pageAndState || !pageAndState.page){
				// Redirect to 404:
				pageAndState = this.getPage(rootPage, this.props.notFound || '/404');
			}
			
			var {page} = pageAndState;
			
			if(!page || !page.id){
				return success(pageAndState);
			}
			
			var pgLoad = null;
			
			if(page.createdUtc == undefined){
				// Page not loaded yet - get it now:
				pgLoad = webRequest("page/" + page.id).then(response => {
					for(var field in response.json){
						page[field] = response.json[field];
					}
					
					return page;
				});
			}else{
				pgLoad = Promise.resolve(page);
			}
			
			pgLoad.then(page => {
				var json;
			
				if(typeof page.bodyJson == "string"){
					try{
						json = JSON.parse(page.bodyJson);
					}catch(e){
						console.log('Failed to load page JSON: ', page.bodyJson);
						console.error(e);
					}
				}else{
					json = page.bodyJson;
				}
				
				if(!json){
					json = {};
				}
				
				if(json.redirect){
					// Redirecting to another page. It's always identified by ID.
					pageAndState = {
						page: this.resolveRedirect(json.redirect, idMap, 0),
						tokens: pageAndState.tokens
					};
				}
				
				success(pageAndState);
			});
		});
	}
	
	getPage(rootPage, url){
		url = url.split('?')[0].trim();
		
		if(url[0] == '/'){
			url=url.substring(1);
		}

		if(url[url.length-1] == '/'){
			url=url.substring(0,url.length-1);
		}
		
		var prefix = global.urlPrefix;
		
		if(prefix){
			if(prefix[0] == '/'){
				prefix = prefix.substring(1);
			}
			
			if(url.substring(0,prefix.length).toLowerCase() == prefix.toLowerCase()){
				url = url.substring(prefix.length);
				
				if(url[0] == '/'){
					url=url.substring(1);
				}
			}
		}
		
		if(global.mapUrl){
			url = global.mapUrl(url);
		}
		
		var curNode = rootPage;
		
		if(!curNode){
			return null;
		}
		
		var tokens = {};
		
		if(url.length){
			var parts = url.split('/');

			for(var i=0;i<parts.length;i++){
				var nextNode = curNode.children[parts[i]];

				if(!nextNode){
					nextNode = curNode.children['*'];
					
					if(nextNode && nextNode.tokenNames.length){
						// handles e.g. :token or {token} in the URL - sets the named token into the URL ctx.
						// Note that one URL segment can be known by multiple token names.
						var tNames= nextNode.tokenNames;
						
						for(var e=0;e<tNames.length;e++){
							tokens[tNames[e]] = parts[i];
						}
					}
				}
				
				if(!nextNode){
					return null;
				}
				curNode = nextNode;
			}
		}

		return {
			page: curNode.page,
			tokens
		};
	}
	
	render(){
		var {page} = this.state;
		
		if(!page){
			return null;
		}
		
		if(page.title && page.title.length){
			return [
				<title>
					{page.title}
				</title>,
				<Canvas>
					{page.bodyJson}
				</Canvas>
			];
		}
		
		return <Canvas>
			{page.bodyJson}
		</Canvas>;
		
	}
	
}