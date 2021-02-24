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

	replaceTokens(str, res) {
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


	// Used to update the pages meta's such as title and description.
	updateMetas(res) {
		var {page} = res;

		if(page && page.title){
			// Does our title have any tokens in it?
			document.title = this.replaceTokens(page.title, res);
		}
	}
	
	componentDidUpdate(oldProps){
		if(oldProps.url != this.props.url){
			webRequest("page/state", {url:this.props.url, version: getBuildDate().timestamp}).then(res => {this.setState(res.json); this.updateMetas(res.json);});
		}
	}
}