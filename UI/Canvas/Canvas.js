import {expand} from 'UI/Functions/CanvasExpand';
import { RouterConsumer } from 'UI/Session';
import {TokenResolver} from 'UI/Token';
import Alert from 'UI/Alert';

var uniqueKey = 1;

/**
 * This component renders canvas JSON. It takes canvas JSON as its child
 */
class Canvas extends React.Component {
	
	constructor(props){
		super(props);

		this.state = {
			content: this.loadJson(props)
		};
	}
	
	componentDidMount(){
		this._loc = global.location.href;
	}
	
	componentWillReceiveProps(props){
		// Only do something if canvas JSON provided has changed, or .
		var dataSource = props.bodyJson || props.children;
		
		if(this.props){
			var prevDataSource = this.props.bodyJson || this.props.children;
			
			if(typeof dataSource == 'string' && prevDataSource == dataSource){
				// Surrounding global state may have changed though. If we're on a different page from before, force a load.
				var cur = global.location.href
				
				if(cur == this._loc){
					return;
				}
				
				this._loc = cur;
			}
		}
		
		this.setState({content: this.loadJson(props)});
	}
	
	loadJson(props, set){
		var content;
		var dataSource = props.bodyJson || props.children;
		
		if(typeof dataSource == 'string'){
			try{
				content = JSON.parse(dataSource);
			}catch(e){
				console.log("Canvas failed to load JSON: ", dataSource);
				console.error(e);
			}
		}else{
			content = dataSource;
		}
		
		if(content){
			content = expand(content, props.onContentNode);
		}
		
		return content;
	}
	
	forceRender() {
		this.setState({content: this.state.content});
	}
	
	onChange(){
		this.forceRender();
		this.props.onCanvasChanged && this.props.onCanvasChanged();
	}
	
	renderNodeC1(contentNode, index, pageRouter) {
		if(!contentNode){
			return null;
		}
		
		if(!contentNode.module && !contentNode.content){
			// E.g. strings, numbers.
			return contentNode;
		}
		
		// note that some elements are just arrays of content, which will be wrapped in a div.
		var Module = contentNode.module || "div";
		
		// Resolve runtime field values now:
		var dataFields = contentNode.data;

		if (contentNode.data && contentNode.data.name && contentNode.data.name.toLowerCase().endsWith('iconref')) {
			dataFields['iconOnly'] = true;
        }
		
		return (
			<Module key={index} {...dataFields}>
			{
				contentNode.useCanvasRender && contentNode.content ? contentNode.content.map((e,i)=>this.renderNodeC1(e,i, pageRouter)) : contentNode.content
			}
			</Module>
		);
	}
	
	renderNode(node){		
		if(!node){
			return null;
		}
		if(Array.isArray(node)){
			return node.map((n,i) => {
				if(n && !n.__key){
					if(n.id){
						n.__key = n.id;
					}else{
						n.__key = "_canvas_" + uniqueKey;
					}
					uniqueKey++;
				}
				return this.renderNode(n);
			});
		}
		
		if(node.graph){
			return <ErrorCatcher node={node}>{node.graph.render()}</ErrorCatcher>;
		}
		
		var NodeType = node.type;
		
		if(NodeType == '#text'){
			if(this.props.context !== undefined){
				// Contextual token canvas.
				return <TokenResolver content={this.props.context} value={node.text}>{resolvedText => resolvedText}</TokenResolver>;
			}else{
				return node.text;
			}
		}else if(typeof NodeType === 'string'){
			if(!node.dom){
				node.dom = React.createRef();
			}
			
			var childContent = null;

			if(node.content && node.content.length){
				childContent = this.renderNode(node.content);
			} else if (!node.isInline && node.type != 'br') {
				// Fake a <br> such that block elements still have some sort of height.
				//childContent = this.renderNode({type:'br', props: {'rte-fake': 1}});
				if (!node.props) {
					node.props = {};
				}
				var className = node.props.className ? node.props.className : "";
				if (!(className && className.length && className.includes("empty-canvas-node"))) {
					className += " empty-canvas-node";
				}
				node.props.className = className;
			}
			
			return <NodeType key={node.__key} ref={node.dom} {...node.props}>{childContent}</NodeType>;
		}else if(NodeType){
			// Custom component
			var props = {...node.props};
			
			if(node.roots){
				var children = null;
				
				for(var k in node.roots){
					var root = node.roots[k];
					
					var isChildren = k == 'children';

					var rendered = this.renderNode(root.content);
					
					if(isChildren){
						children = rendered;
					}else{
						props[k] = rendered;
					}
				}
				
				return <ErrorCatcher node={node}><NodeType key={node.__key} {...props}>{children}</NodeType></ErrorCatcher>;
			}else{
				// It has no content inside it; it's purely config driven.
				// Either wrap it in a span (such that it only has exactly 1 DOM node, always), unless the module tells us it has one node anyway:
				return <ErrorCatcher node={node}><NodeType key={node.__key} {...props} /></ErrorCatcher>;
			}
		}else if(node.content){
			return this.renderNode(node.content);
		}
	}
	
	render() {
		var content = this.state.content;
		
		// Otherwise, render the (preprocessed) child nodes.
		if(!content){
			return null;
		}
		
		return <RouterConsumer>{
			pageRouter => {
				if (content.c2) {
					// Canvas 2
					return this.renderNode(content, 0, pageRouter);
					
				}else{
					return Array.isArray(content) ? 
					content.map((e,i)=>this.renderNodeC1(e,i, pageRouter)) : 
					this.renderNodeC1(content, 0, pageRouter);
				}
				
			}
		}</RouterConsumer>;
	}
}

Canvas.propTypes = {
	bodyJson: 'canvas',
	context: 'object'
};

export default Canvas;

export class ErrorCatcher extends React.Component {
	
	constructor(props) {
		super(props);
		this.state = { hasError: false, errorShown: false };
	}

	static getDerivedStateFromError(error) {
		// Update state so the next render will show the fallback UI.
		var msg = `Unknown`;

		if (error)
		{
			// Main message:
			msg = error.toString();

			if (error.fileName)
			{
				msg += ' (in ' + error.fileName + ' at ' + error.lineNumber + ':' + error.columnNumber + ')';
			}
		}

		return { hasError: true, error: msg, errorShown: false };
	}
	
	componentWillReceiveProps(newProps){
		if(newProps.node != this.props.node){
			// Back to default error state:
			this.setState({ hasError: false, errorShown: false });
		}
	}
	
	componentDidCatch(error, errorInfo) {
		console.error(error, errorInfo);
	}
	
	render() {
		if (this.state.hasError) {
			var { node } = this.props;
			
			var name = node ? (node.graph ? 'Graph runtime' : node.typeName) : 'Unknown';
			
			return <Alert type='error'>
				{`The component "${name}" crashed.`}
				<details>
					<summary>{`Error details`}</summary>
					{
						this.state.error
					}
				</details>
			</Alert>;
		}
		
		return this.props.children; 
	}
	
}

