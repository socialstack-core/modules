var preact = React;
var render = preact.render;


export default class Portal extends React.Component {
	
	componentDidUpdate(props) {
		for (let i in props) {
			if (props[i]!==this.props[i]) {
				return setTimeout(this.renderLayer);
			}
		}
	}
	
	componentDidMount() {
		this.isMounted=true;
		this.renderLayer = this.renderLayer.bind(this);
		this.renderLayer();
	}
	
	componentWillUnmount() {
		this.renderLayer(false);
		this.isMounted=false;
		if (this.remote && this.remote.parentNode) this.remote.parentNode.removeChild(this.remote);
	}
	
	findNode(node) {
		return typeof node==='string' ? document.querySelector(node) : node;
	}
	
	renderLayer(show=true) {
		if (!this.isMounted) return;

		// clean up old node if moving bases:
		if (this.props.into!==this.intoPointer) {
			this.intoPointer = this.props.into;
			if (this.into && this.remote) {
				this.remote = render(<PortalProxy />, this.into, this.remote);
			}
			this.into = this.findNode(this.props.into);
		}
		
		if(!this.into){
			return;
		}
		
		this.remote = render((
			<PortalProxy context={this.context}>
				{ show ? this.props.children : null }
			</PortalProxy>
		), this.into, this.remote);
	}
	
	render() {
		if(!this.into && this.isMounted && this.props.into){
			this.into = this.findNode(this.props.into);
			setTimeout(this.renderLayer);
		}
		
		return null;
	}
}

// high-order component that renders its first child if it exists.
// used as a conditional rendering proxy.
class PortalProxy extends React.Component {
	getChildContext() {
		return this.props.context;
	}
	render({ children }) {
		return children;
	}
}
