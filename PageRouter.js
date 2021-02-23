import webRequest from 'UI/Functions/WebRequest';
import Canvas from 'UI/Canvas';
import getBuildDate from 'UI/Functions/GetBuildDate';

export default class PageRouter extends React.Component{
	
	go(url){
		global.history.pushState({}, "", global.storedToken ? '#' + url : url);
		document.body.parentNode.scrollTop=0;
		this.props.onNavigate && this.props.onNavigate(url);
	}
	
	constructor(props, context){
		super(props, context);
		global.pageRouter = this;
		context.pageRouter = this;
		this.onLinkClick = this.onLinkClick.bind(this);
		this.onPopState = this.onPopState.bind(this);
		this.state = context.pgState || global.pgState;
		this.trigger(this.state.page);
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
	
	componentWillUnmount(){
		global.removeEventListener("popstate", this.onPopState);
		document.removeEventListener("click", this.onLinkClick);
	}
	
	render(){
		var {page} = this.state;
		return page ? <Canvas>
			{page.bodyJson}
		</Canvas> : null;
	}
	
	componentDidUpdate(oldProps){
		if(oldProps.url != this.props.url){
			webRequest("page/state", {url:this.props.url, version: getBuildDate().timestamp}).then(res => this.setState(res.json));
		}
		
		var {page} = this.state;
		if(page && page.title){
			document.title = page.title;
		}
	}
}