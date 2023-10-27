import NodeOutput from './NodeOutput';
import { useRouter, useSession } from 'UI/Session';
import webRequest from 'UI/Functions/WebRequest';
import Content from 'UI/Content';


function includesMatches(wanted, has) {
	// Check if all of the wanted includes are at 
	// least contained in the "has" set.
	if (!wanted || has['*']) {
		// I want nothing or it has everything.
		// Note that an empty wanted array will also 
		// go straight to returning true.
		// Therefore this set will fulfil the want.
		return true;
	}

	for (var i = 0; i < wanted.length; i++) {
		var want = wanted[i];
		if (!has[want]) {
			return false;
		}
	}

	return true;
}

function makeIncludesSet(set) {
	var map = {};
	if (set) {
		for (var i = 0; i < set.length; i++) {
			map[set[i]] = true;
		}
	}
	return map;
}

function findBy(requests, body, wanted) {
	for (var i = 0; i < requests.length; i++) {
		var request = requests[i];
		if (includesMatches(wanted, request.inc)) {
			if (body == request.b) {
				// basic body check for now. Will generally only pass on nulls.
				return request.p;
			}
		}
	}
	return null;
}

// ContentCache:
function CC(ctx) {
	var byType = {};

	this.get = (type, id, body, includes) => {
		if (!id) {
			return {};
		}

		// type cache:
		var tc = byType[type];

		if (!tc) {
			// Create type cache:
			byType[type] = tc = {};
		}

		// Got the content in there?
		var requests = tc[id];
		if (!requests) {
			tc[id] = requests = [];
		}

		// Search the array for a match by body & include set:
		var p = findBy(requests, body, includes);

		if (p) {
			// Found a suitable request with an 
			// includes set that covers the wanted includes.
			// Return it:
			return p;
		}

		// No - create a request now:
		p = webRequest(type + '/' + id, body, { includes });
		
		var request = {
			p,
			inc: makeIncludesSet(includes),
			b: body
		};
		
		requests.push(request);
		return request.p;
	};

}

const CacheCtx = React.createContext();

export const Provider = (props) => {
	// Content cache provider.
	// Stores state for nodes which grab content.
	var [cache, setCache] = React.useState(() => new CC(props.ctx));

	return (
		<CacheCtx.Provider
			value={cache}
		>
			{props.children}
		</CacheCtx.Provider>
	);
};

function useContentCache() {
	// returns CC object
	return React.useContext(CacheCtx);
}

function ReactGraphHelper(props) {
	var { pageState } = useRouter();
	var { session } = useSession();
	var cache = useContentCache();
	var context = { pageState, session, cache };
	return <ReactGraphHelperIntl graph={props.graph}
		context={context} />
}

class ReactGraphHelperIntl extends React.Component {
	constructor(props) {
		super(props);
		
		var output = this.init(props);
		
		if(output && output.then && !window.SERVER){
			// It returned a promise. Wait for it. The server waits for promises in state anyway.
			output.then((result) => {
				this.setState({output: result});
			});
			output = null;
		}
		
		this.state = {
			output
		};
	}
	
	init(props) {
		var graph = props.graph;
		graph.setContext(props.context);
		return graph.run(); // like ValueTask - can return a value or a promise.
	}
	
	componentWillReceiveProps(props){
		if(props.graph != this.props.graph){
			var output = this.init(props);
			
			if(output && output.then){
				// It returned a promise. Wait for it.
				output.then((result) => {
					this.setState({output: result});
				});
			}else{
				this.setState({output});
			}
		}
	}
	
	render() {
		return this.state.output;
	}
}

export default class Graph {

	constructor(structure, config) {
		if (typeof structure === 'string') {
			structure = JSON.parse(structure);
		}
		config = config || {};
		this.structure = structure;
		this.config = config;
		this._output = null; // Cached output if you want to use it.

		if (config.namespace && !config.ns) {
			config.ns = config.namespace;
		}

		if (!config.ns) {
			// Use the default canvas namespace
			config.ns = 'UI/Canvas/Graph/';
		}
	}

	reset() {
		// Clear all caches in nodes if necessary.
		this._output = null;

		if (this.nodes) {
			this.nodes.forEach(node => {
				node.reset();
			});
		}
	}

	loadNodes() {
		var graphData = this.structure;

		if (typeof graphData === 'string') {
			graphData = JSON.parse(graphData);
		}

		if (Array.isArray(graphData)) {
			graphData = { c: graphData };
		}

		if (!graphData || !graphData.c) {
			// Empty node set.
			return [];
		}

		if (!Array.isArray(graphData.c)) {
			graphData.c = [graphData.c];
		}

		var nodes = [];

		// The index in this array is used to establish links.
		// The convention is also that the first entry in the array is the result/ output node.
		for (var i = 0; i < graphData.c.length; i++) {
			var nodeJson = graphData.c[i];

			// Initial node expansion by constructing them.
			var nodeType = nodeJson.t;

			if (nodeType.indexOf('/') == -1) {
				// Unprefixed node - add the common namespace:
				nodeType = (this.config.ns || '') + nodeType;
			}

			var NodeType = require(nodeType).default;

			if (!NodeType) {
				throw new Error("Missing graph node: " + nodeType);
			}

			// instance the node:
			var node = new NodeType();
			node.context = this.context;

			// Construct the data state; links will come later:
			node.loadData(nodeJson.d);

			nodes.push(node);
		}

		// Now the nodes are constructed, we can now load the links.
		for (var i = 0; i < graphData.c.length; i++) {
			var nodeJson = graphData.c[i];
			var node = nodes[i];

			if (nodeJson.r) {
				this.root = node;
			}

			if (nodeJson.l) {
				// It has links - load each one:
				for (var key in nodeJson.l) {
					var link = nodeJson.l[key];

					if (link && link.n >= 0 && link.n < nodes.length) {
						var srcNode = nodes[link.n];

						// Add the link:
						node.state[key] = new NodeOutput({ node: srcNode, field: link.f });
					}
				}
			}
		}

		return nodes;
	}

	setContext(context) {
		// Must call before the first run.
		this.context = context;
	}

	run() {
		// like ValueTask - can return a value or a promise.
		// This is such that graphs using content cached by the serverside renderer (or just simple graphs that don't have content anyway) load instantly.
		
		if (!this.nodes) {
			// Lazy expansion of the nodes. Instance and link up each one now.
			this.nodes = this.loadNodes();
		}
		
		if (!this.root) {
			return null;
		}

		return this.root.run();
	}

	render() {
		// Special react specific convenience function. Use run() for generic graphs.

		// A react render call. Can't use state here, but can in child components.
		return <ReactGraphHelper graph={this} />;
	}

}