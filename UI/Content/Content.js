import webRequest, {expandIncludes} from 'UI/Functions/WebRequest';
import webSocket from 'UI/Functions/WebSocket';
import { SessionConsumer, RouterConsumer } from 'UI/Session';

const ContentContext = React.createContext();

class ContentIntl extends React.Component {
	
	constructor(props){
		super(props);
		this.state={
			content: null
		};
		this.onLiveMessage = this.onLiveMessage.bind(this);
		this.onContentChange = this.onContentChange.bind(this);
	}
	
	evtType(){
		var content = this.state.content;
		
		if(!content || content._err){
			return null;
		}
		
		var name = content.type;
		return name.charAt(0).toUpperCase() + name.slice(1);
	}
	
	onLiveMessage(msg) {
		if (msg.all) {
			if (msg.type == "status") {
				if (msg.connected) {
					// Force a reload:
					var {type, id, includes} = this.props;
					this.load(type, id, includes);
				}

				this.props.onLiveStatus && this.props.onLiveStatus(msg.connected);
			}
			return;
		}
		
		// Push msg.entity into the results set:
		if (this.state.content && msg.entity) {
			var e = msg.entity;
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
		var content = this.state.content;
		
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
			if(content.id == entity.id){
				this.setState({
					content: entity
				});
			}
		}
	}
	
	componentDidUpdate(prevProps){
		var {type, id, includes} = this.props;
		
		this.updateWS(prevProps && id != prevProps.id);
		
		if(prevProps && type == prevProps.type && id == prevProps.id){
			// Cached object is fine here.
			return;
		}
		
		if (type !== '' && id !== 0) {
			this.load(type, id, includes);
		}
	}
	
	load(type, id, includes){
		Content.get(type, id, includes)
			.then(content => this.setState({content}))
			.catch(e => {
				// E.g. doesn't exist.
				this.setState({content: {_err: e}});
			});
	}
	
	componentWillUnmount() {
		if (this.mountedType) {
			webSocket.removeEventListener(this.mountedType, this.onLiveMessage);
			this.mountedType = null;
		}
		document.removeEventListener("contentchange", this.onContentChange);
	}
	
	updateWS(idChange){
		var {live, id} = this.props;
		if (live) {
			var idealType    = this.evtType();
			var typeChange   = idealType && idealType != this.mountedType;
			this.mountedType = idealType ?? this.mountedType;

			if(this.mountedType && (typeChange || idChange)) {
				webSocket.addEventListener(this.mountedType, this.onLiveMessage, id);
			}
		}
	}
	
	componentDidMount(){
		var {type, id, includes} = this.props;
		this.updateWS();
		document.addEventListener("contentchange", this.onContentChange);
		
		if(!this.state.content && id){
			// Content that is intentionally client only. Load now:
			this.load(type, id, includes);
		}
	}
	
	render(){
		var {content} = this.state;
		var {children} = this.props;
		
		var loading = false;
		
		if(!content){
			// Null indicates loading:
			loading = true;
		}else if(content._err){
			// It failed - indicate null but not loading to children:
			content = null;
		}
		
		return <ContentContext.Provider
			value={{content}}
		>
			{children ? children(content, loading) : null}
		</ContentContext.Provider>;
	}
	
}

/*
* A convenience mechanism for obtaining 1 piece of content. Outputs no DOM structure.
* Very similar to <Loop> with a where:{Id: x}.
*/
export default function Content(props) {
	
	return (props.primary) ? <RouterConsumer>{
		pgState => {
			if(!pgState.po){
				return null;
			}
			
			if(pgState.po.type && pgState.po.id){
				return <ContentIntl type={pgState.po.type} id={pgState.po.id} {...props}/>;
			}
			
			// Occurs when po is a customData object:
			return props.children ? props.children(pgState.po, false) : null;
		}
	}</RouterConsumer> : <ContentIntl {...props}/>;
	
}

export function useContent(){
	return React.useContext(ContentContext);
}

// Use this to use <Content> via context. Used primarily by tokens.
export const ContentConsumer = ContentContext.Consumer;

// E.g:
// content.get("blog", 1);
// A convenience wrapper which is shorted serverside for rapid performance.
// Returns a promise which will either resolve directly to the object, 
// or be rejected with a message and statusCode if there was an error.
// You should only use this from componentDidMount and componentDidUpdate (or useEffect) for correct React usage.
Content.get = function(type, id, includes) {
	var url = type + '/' + id;
	return webRequest(url, null, includes ? {includes} : null).then(response => response.json);
};

// E.g:
// content.list("blog", {where: ...});
// A convenience wrapper which is shorted serverside for rapid performance.
// Returns a promise which will either resolve directly to the object, 
// or be rejected with a message and statusCode if there was an error.
// You should only use this from componentDidMount and componentDidUpdate (or useEffect) for correct React usage.
Content.list = function(type, filter, includes) {
	var url = type + '/list';
	return webRequest(url, filter, includes ? {includes} : null).then(response => response.json);
};
