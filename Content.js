import webRequest from 'UI/Functions/WebRequest';
import webSocket from 'UI/Functions/WebSocket';

/*
* A convenience mechanism for obtaining 1 piece of content. Outputs no DOM structure.
* Very similar to <Loop> with a where:{Id: x}.
*/
export default class Content extends React.Component {
	
	constructor(props){
		super(props);
		this.state={
			// Get initial content object. This is to avoid very inefficient wasted renders 
			// caused by using a promise here or componentDidUpdate only.
			content: props.id ? Content.getCached(props.type, props.id) : null
		};
		this.onLiveMessage = this.onLiveMessage.bind(this);
		this.onContentChange = this.onContentChange.bind(this);
	}
	
	evtType(){
		var name = this.props.type;
		return name.charAt(0).toUpperCase() + name.slice(1);
	}
	
	onLiveMessage(msg) {
		if (msg.all) {
			if (msg.type == "status") {
				if (msg.connected) {
					// Force a reload:
					var {type, id, primary} = this.props;
					!primary && this.load(type, id);
				}

				this.props.onLiveStatus && this.props.onLiveStatus(msg.connected);
			}
			return;
		}
		
		// Push msg.entity into the results set:
		if (this.state.content && msg.entity) {
			var e = msg.entity;
			if (msg.by && e.viewedAtUtc) {
				// Special views specific functionality here.
				// If we receive an update via the websocket, we must change its viewedAtUtc field (if it has one).
				// That's because its value is user specific, and is set to the value of the person who raised the event.
				// Lots of database traffic just isn't worthwhile given the UI can figure it out for itself.
				
				// If *this user* made the update, set the viewed date as the edited date.
				// Otherwise, clear it. We don't know when this user actually last saw it.
				var { user } = this.context.app.state;

				var userId = user ? user.id : 0;

				if (msg.by == userId) {
					e.viewedAtUtc = e.editedUtc;
				} else {
					e.viewedAtUtc = null;
				}
			}
			
			var entityId = e.id;

			if (msg.method == 'delete') {
				this.onContentChange({deleted: true, entity: e});
			} else if (msg.method == 'update' || msg.method == 'create') {
				this.onContentChange({entity: e});
			}
		}
	}
	
	onContentChange(e) {
		// Content changed! Is it a thing relative to us?
		var { content } = this.state;
		if (!content) {
			// Nothing loaded yet
			return;
		}
		
		var entity = e.entity;
		if(entity && entity.type != this.evtType()){
			return;
		}
		
		if (this.props.onContentChange) {
			entity = this.props.onContentChange(entity);
			if (!entity) {
				// Handler rejected
				return;
			}
		}
		
		if (e.deleted) {
			// Deleted it. _err indicates an object that is known to not exist:
			this.setState({
				content: {_err: true}
			});
		} else {
			// Update or add. id match?
			if(this.props.id == entity.id){
				this.setState({
					content: entity
				});
			}
		}
	}
	
	componentDidUpdate(prevProps){
		var {type, id, primary} = this.props;
		
		if((prevProps.primary && primary) || (prevProps && type == prevProps.type && id == prevProps.id)){
			// Cached object is fine here.
			return;
		}
		this.load(type, id);
	}
	
	load(type, id){
		Content.get(type, id)
			.then(content => this.setState({content}))
			.catch(e => {
				// E.g. doesn't exist.
				this.setState({content: {_err: e}});
			});
	}
	
	componentWillUnmount() {
		if (this.props.live) {
			webSocket.removeEventListener(this.evtType(), this.onLiveMessage);
		}
		document.removeEventListener("contentchange", this.onContentChange);
	}
	
	componentDidMount(){
		var {type, id, live} = this.props;
		if (live) {
			webSocket.addEventListener(this.evtType(), this.onLiveMessage, {where: {Id: id}});
		}
		document.addEventListener("contentchange", this.onContentChange);
		
		if(!this.state.content && id){
			// Content that is intentionally client only. Load now:
			this.load(type, id);
		}
	}
	
	render(){
		var {content} = this.state;
		var {children, primary} = this.props;
		
		if(primary){
			content = this.context.pageRouter.state.po;
		}
		
		var loading = false;
		
		if(!content){
			// Null indicates loading:
			loading = true;
		}else if(content._err){
			// It failed - indicate null but not loading to children:
			content = null;
		}
		return children ? children(content, loading) : null;
	}
	
}

// Gets the current page's primary content.
Content.getPrimary = function(context){
	return context.pageRouter.state.po;
};

// E.g:
// content.get("blog", 1);
// A convenience wrapper which is shorted serverside for rapid performance.
// Returns a promise which will either resolve directly to the object, 
// or be rejected with a message and statusCode if there was an error.
// You should only use this from componentDidMount and componentDidUpdate (or useEffect) for correct React usage.
Content.get = function(type, id) {
	var url = type + '/' + id;
	return webRequest(url).then(response => response.json);
};

// E.g:
// content.list("blog", {where: ...});
// A convenience wrapper which is shorted serverside for rapid performance.
// Returns a promise which will either resolve directly to the object, 
// or be rejected with a message and statusCode if there was an error.
// You should only use this from componentDidMount and componentDidUpdate (or useEffect) for correct React usage.
Content.list = function(type, filter) {
	var url = type + '/list';
	return webRequest(url, filter).then(response => response.json);
};

// E.g:
// content.getCached("blog", 1, context);
// Returns the object immediately if it came from the cache, otherwise null.
// This should be used in your component constructor.
// Using it prevents a wasted render when the data is available immediately.
// 
// When server side, this may return a promise. Any promises in a component's state when it has been 
// constructed will be awaited and swapped with the resolved value before proceeding.
Content.getCached = function(type, id) {
	if(!global.pageRouter || global.cIndex === undefined){
		return null;
	}
	var {data} = global.pageRouter.state;
	
	// Purely by order.
	return data ? data[global.cIndex++] : null;
};

Content.listCached = function(type, filter) {
	return Content.getCached();
};