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
			loading: true
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
					this.load(this.props);
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
				var { user } = global.app.state;

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
		if (!this.state.content) {
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
			// Deleted it:
			this.setState({
				content: null,
				loading: false
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
	
	componentWillReceiveProps(props){
		if(this.props && this.props.type == props.type && this.props.id == props.id){
			// Cached object is fine here.
			return;
		}
		this.load(props);
	}
	
	componentWillUnmount() {
		if (this.props.live) {
			webSocket.removeEventListener(this.evtType(), this.onLiveMessage);
		}
		
		document.removeEventListener("contentchange", this.onContentChange);
	}
	
	componentDidMount(){
		this.load(this.props);
		document.addEventListener("contentchange", this.onContentChange);
	}
	
	load(props){
		if (props.live) {
			webSocket.addEventListener(this.evtType(), this.onLiveMessage, {where: {Id: props.id}});
		}
		
		this.setState({loading: true});
		Content.get(props.type, props.id)
			.then(content => this.setState({content, loading: false}))
			.catch(e => {
				// E.g. doesn't exist.
				this.setState({content: null, loading: false});
			});
	}
	
	render(){
		
		return <div className="content">
			{this.props.children && this.props.children(this.state.content, this.state.loading)}
		</div>;
		
	}
	
}

// E.g:
// content.get("blog", 1);
// A convenience wrapper which is shorted serverside for rapid performance.
// Returns a promise which will either resolve directly to the object, 
// or be rejected with a message and statusCode if there was an error.
// 
// Objects may be returned instantly from a client side cache. Due to the way how promise will, by design, always wait, this can lead to a wasted render 
// where a load screen is displayed for a few frames, and will typically result in an apparent white flash.
// To avoid this, the returned promise also has a .value 

Content.get = (contentType, contentId) => {
	var url = contentType + '/' + contentId;
	return webRequest(url).then(response => response.json);
};