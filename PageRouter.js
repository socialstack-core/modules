import webRequest from 'UI/Functions/WebRequest';
import Canvas from 'UI/Canvas';

try{
	var preloadedPages = require('UI/PreloadedPages/PreloadedPages.json');
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
				
		pages.forEach(page => {
			idMap[''+page.id] = page;
		});
		
		var rootPage = {children: {}, tokenNames: []};
		
		pages.forEach(page => {
			
			var url = page.url;
			
			if(!url){
				return;
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
					var token = (part.length && part[0] == ':') && part.substring(1);
					
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
		
		var pageInfo = this.getPageRedirected(rootPage, idMap, this.props.url);
		this.trigger(pageInfo);
		if(isCached && pageInfo.page.url == "" && !(this.props.url == "/" || this.props.url.substring(this.props.url.length - 11, this.props.url.length) === "mobile.html")){
			this.makeRequest();
		}
		else{
			this.setState({
				pages,
				rootPage,
				idMap,
				...pageInfo
			});
		}
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
		
		if(!preloadedPages){
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
			this.setupPage(props);
		}
	}
	
	setupPage(props){
		// page and tokens
		var pageInfo = this.getPageRedirected(this.state.rootPage, this.state.idMap, props.url);
		this.trigger(pageInfo);
		this.setState(pageInfo);
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
		
		var pageAndState = this.getPage(rootPage, url);
		
		if(!pageAndState || !pageAndState.page){
			// Redirect to 404:
			return this.getPage(this.state.rootPage, this.props.notFound || '/404');
		}
		
		var {page} = pageAndState;
		
		if(!page.bodyJson){
			return pageAndState;
		}
		
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
			return {
				page: this.resolveRedirect(json.redirect, idMap, 0),
				tokens: pageAndState.tokens
			};
		}
		
		return pageAndState;
	}
	
	getPage(rootPage, url){
		url = url.split('?')[0].trim();
		if(url[0] == '/'){
			url=url.substring(1);
		}

		if(url[url.length-1] == '/'){
			url=url.substring(0,url.length-1);
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
						// handles e.g. :token in the URL - sets the named token into the URL ctx.
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