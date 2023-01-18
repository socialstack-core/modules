import NodeOutput from './NodeOutput';
import {useRouter, useSession} from 'UI/Session';

function ReactGraphHelper(props){
	var [output, setOutput] = React.useState(props.graph._output);
	
	var {pageState} = useRouter();
	var {session} = useSession();
	
	React.useEffect(() => {
		// Run the graph once:
		var graph = props.graph;
		
		if(graph._output){
			// Already ran
			return;
		}
		
		graph.setContext({pageState, session});
		graph.run().then(graphOutput => {
			graph._output = graphOutput;
			setOutput(graphOutput);
		});
	}, []);
	
	return output;
}

export default class Graph {
	
	constructor(structure, config) {
		if(typeof structure === 'string'){
			structure = JSON.parse(structure);
		}
		config = config || {};
		this.structure = structure;
		this.config = config;
		this._output = null; // Cached output if you want to use it.
		
		if(config.namespace && !config.ns){
			config.ns = config.namespace;
		}
		
		if(!config.ns){
			// Use the default canvas namespace
			config.ns = 'UI/Canvas/Graph/';
		}
	}
	
	reset(){
		// Clear all caches in nodes if necessary.
		this._output = null;
		
		if(this.nodes){
			this.nodes.forEach(node => {
				node.reset();
			});
		}
	}
	
	loadNodes(){
		var graphData = this.structure;
		
		if(typeof graphData === 'string'){
			graphData = JSON.parse(graphData);
		}
		
		if(Array.isArray(graphData)){
			graphData = {c: graphData};
		}
		
		if(!graphData || !graphData.c){
			// Empty node set.
			return [];
		}
		
		if(!Array.isArray(graphData.c)){
			graphData.c = [graphData.c];
		}
		
		var nodes = [];
		
		// The index in this array is used to establish links.
		// The convention is also that the first entry in the array is the result/ output node.
		for(var i=0;i<graphData.c.length;i++){
			var nodeJson = graphData.c[i];
			
			// Initial node expansion by constructing them.
			var nodeType = nodeJson.t;
			
			if(nodeType.indexOf('/') == -1){
				// Unprefixed node - add the common namespace:
				nodeType = (this.config.ns || '') + nodeType;
			}
			
			var NodeType = require(nodeType).default;
			
			if(!NodeType){
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
		for(var i=0;i<graphData.c.length;i++){
			var nodeJson = graphData.c[i];
			var node = nodes[i];
			
			if(nodeJson.r){
				this.root = node;
			}
			
			if(nodeJson.l){
				// It has links - load each one:
				for(var key in nodeJson.l){
					var link = nodeJson.l[key];
					
					if(link && link.n >= 0 && link.n < nodes.length){
						var srcNode = nodes[link.n];
						
						// Add the link:
						node.state[key] = new NodeOutput({node: srcNode, field: link.f});
					}
				}
			}
		}
		
		return nodes;
	}
	
	setContext(context){
		// Must call before the first run.
		this.context = context;
	}
	
	async run(){
		if(!this.nodes){
			// Lazy expansion of the nodes. Instance and link up each one now.
			this.nodes = this.loadNodes();
		}
		
		if(!this.root){
			return null;
		}
		
		return this.root.run();
	}
	
	render(){
		// Special react specific convenience function. Use run() for generic graphs.

		// A react render call. Can't use state here, but can in child components.
		return <ReactGraphHelper graph={this}/>;
	}
	
}