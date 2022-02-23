import {VectorPath, PathFitter, MoveToPoint, StraightLinePoint, QuadLinePoint, CurveLinePoint} from 'UI/Functions/VectorPath';

const SELECTION = 1;
const DRAW = 2;

export default class ShapeDraw extends React.Component {
	
	constructor(props){
		super(props);
		this.state={
			paths: this.loadPaths(props.initialPaths || props.paths),
			zoom: 1,
			mode: DRAW
		};
	}
	
	componentWillReceiveProps(newProps){
		
		if(this.props && newProps.paths != this.props.paths){
			this.setState({paths: this.loadPaths(newProps.paths)}, () => this.redraw());
		}
		
	}
	
	loadPaths(paths){
		var pathState = [];
		
		if(paths && paths.length){
			
			for(var i=0;i<paths.length;i++){
				
				// Path is just an array of nodes
				var data = paths[i];
				
				if(!data || !data.length){
					continue;
				}
				
				var path = new VectorPath();
				
				for(var n=0;n<data.length;n++){
					var dNode = data[n];
					var node;
					
					if(dNode.c2x !== undefined){
						path.curveTo(dNode.c1x,dNode.c1y,dNode.c2x,dNode.c2y,dNode.x,dNode.y);
					}else if(dNode.c1x !== undefined){
						path.quadraticCurveTo(dNode.c1x,dNode.c1y,dNode.x,dNode.y);
					}else if(dNode.m){
						// move to (not a line)
						path.moveTo(dNode.x,dNode.y);
					}else{
						path.lineTo(dNode.x,dNode.y);
					}
					
					if(dNode.c){
						// This node is a close
						path.closePath();
					}
				}
				
				pathState.push(path);
				
			}
		}
		
		return pathState;
	}
	
	getPathData(remapFunc){
		var data = [];
		var paths = this.state.paths;
		
		for(var i=0;i<paths.length;i++){
			var path = paths[i];
			var pathData = [];
			data.push(pathData);
			
			var node = path.firstPathNode;
			
			while(node != null){
				var newNode;
				if(node instanceof MoveToPoint){
					newNode = {x: node.x, y: node.y, m: 1};
				}else if(node instanceof StraightLinePoint){
					newNode = {x: node.x, y: node.y};
				}else if(node instanceof QuadLinePoint){
					newNode = {x: node.x, y: node.y, c1x: node.control1X, c1y: node.control1Y};
				}else if(node instanceof CurveLinePoint){
					newNode = {x: node.x, y: node.y, c1x: node.control1X, c1y: node.control1Y, c2x: node.control2X, c2y: node.control2Y};
				}else{
					console.log(node);
					throw new Error("Unknown path node " + node.toString());
				}
				
				if(node.isClose){
					newNode.c=1;
				}
				
				if(remapFunc){
					newNode = remapFunc(newNode);
				}
				
				pathData.push(newNode);
				node = node.next;
			}
			
		}
		
		return data;
	}
	
	redraw(){
		var ctx = this.canvas_ctx;
		var canvas = this.canvas;
		
		var drawInfo=this.getDrawInfo();
		
		ctx.clearRect(0,0,canvas.width,canvas.height);
		
		ctx.setLineDash([]);
		
		var zoom=this.state.zoom;
		ctx.setTransform(zoom,0,0,zoom,0,0);
		
		var p = this.state.paths;
		
		for(var i=0;i<p.length;i++){
			var path = p[i];
			path.draw(drawInfo);
		}
	}
	
	getDrawInfo(){
		var canvas = this.canvas;
		return {
			context: this.canvas_ctx,
			x: 0,
			y: 0,
			nodeRadius: 4,
			nodeLineWidth: 1,
			lineWidth: 1,
			noNodes: this.props.readonly,
			stroke: this.props.lineColor || '#013e78',
			nodeStroke: this.props.nodeBorderColor || '#6e8cb2',
			nodeFill: this.props.nodeColor || '#6e8cb2',
			fill: this.props.fillColor || '#eaebe3',
			controlPointDrawn: false,
			isReference: false,
			width:canvas.width,
			height:canvas.height
		};
	}
	
	testClosed(path,node, radius){
		
		if(path.firstPathNode==null){
			return false;
		}
		radius = radius || 10;
		var last=path.latestPathNode;
		var first=path.firstPathNode;
		
		var fx=first.x;
		var fy=first.y;
		var lx=last.x;
		var ly=last.y;
		
		// lx within 4px of fx?
		var dx=lx-fx;
		
		if(dx>radius || dx<-radius){
			last.isClose=false;
			return false;
		}
		
		var dy=ly-fy;
		
		if(dy>radius || dy<-radius){
			last.isClose=false;
			return false;
		}
		
		// Move node to match the other one:
		if(node==last){
			node.x=fx;
			node.y=fy;
		}else if(node==first){
			node.x=lx;
			node.y=ly;
		}
		
		last.isClose=true;
		
		return true;
	}
	
	// Gets the nearest node line to the given x/y coords point.
	getNearest(x,y){
		// Test a small box region.
		
		// +- this many pixels to test:
		var boxSize=3;
		
		// Get the box coords:
		var minX=x-boxSize;
		var maxX=x+boxSize;
		var minY=y-boxSize;
		var maxY=y+boxSize;
		
		// Clip:
		if(minX<0){
			minX=0;
		}
		
		if(minY<0){
			minY=0;
		}
		
		// Get sizes:
		var width=maxX-minX;
		var height=maxY-minY;
		
		var result={
			line:null, // Clicked on a line
			node:null, // Clicked on a point (Or a control point)
			control:0, // The control point ID. 1 or 2. (0 if not control).
			anchor:null, // Clicked on a selection anchor
			stagePath:null, // The path of a line/node.
			stagePathID:-1, // The ID of the path.
			openSpace:false // Clicked in open space
		};
		
		// Are we in open space?
		var openSpace=true;
		
		// Read from the canvas:
		var imageData=this.canvas_ctx.getImageData(minX,minY,width,height);
		var data=imageData.data;
		
		for(var i=0;i<data.length;i+=4){
			
			//var r=data[i];
			//var g=data[i+1];
			//var b=data[i+2];
			var a=data[i+3];
			
			if(a!=0){
				openSpace=false;
				break;
			}
			
		}
		
		if(openSpace){
			result.openSpace=true;
			return result;
		}
		
		// Most likely to click on nodes, so test those first:
		var pathCount=this.state.paths.length;
		var canvas = this.canvas;
		
		var drawInfo=this.getDrawInfo();
		var nodeRadius = (drawInfo.nodeRadius || 2) + (drawInfo.nodeLineWidth || 1);
		var clickX=x-drawInfo.x;
		var clickY=y-drawInfo.y;
		
		for(var i=0;i<pathCount;i++){
			
			var path=this.state.paths[i];
			
			// For each node in the path:
			var node=path.firstPathNode;
			
			while(node!=null){
				
				// Get dx/dy:
				if(node.getNear(clickX,clickY,path,result, nodeRadius)){
					result.stagePathID=i;
					return result;
				}
				
				// Hop to next one:
				node=node.next;
				
			}
			
		}
		
		// Test selection anchors (to permit dragging them)
		
		// Finally, test for clicks on lines (Todo - needs a hidden input canvas):
		return result;
		
		var lineNode=this.lineNodeAt(x,y,drawInfo);
		
		if(lineNode!=null){
			result.line=lineNode.node;
			result.stagePath=lineNode.stagePath;
			return result;
		}
		
		for(var tx=minX;tx<=maxX;tx++){
			
			for(var ty=minY;ty<=maxY;ty++){
				
				if(x==tx && y==ty){
					continue;
				}
				
				lineNode=this.lineNodeAt(tx,ty,drawInfo);
				
				if(lineNode!=null){
					result.line=lineNode.node;
					result.stagePath=lineNode.stagePath;
					return result;
				}
				
			}
			
		}
		
		// Clicked on unknown target:
		return result;
	}
	
	// Tries to get the path node at the given x/y coords.
	lineNodeAt(x,y,drawInfo){
		
		// Draw to input canvas too - just repeat:
		var ctx=drawInfo.inputContext;
		
		ctx.lineWidth=1;
		ctx.clearRect(0,0,drawInfo.width,drawInfo.height);
		
		var pathCount=this.state.paths.length;
		
		for(var i=0;i<pathCount;i++){
			
			var path=this.state.paths[i];
			
			// For each node in the path:
			var node=path.firstPathNode;
			
			// Skip initial moveto:
			if(node!=null){
				node=node.next;
			}
			
			while(node!=null){
				
				ctx.beginPath();
				
				// Move to start:
				ctx.moveTo(node.previous.x+drawInfo.x,node.previous.y+drawInfo.y);
				
				// Add:
				node.add(drawInfo);
				
				if(node.isClose){
					ctx.closePath();
				}
				
				ctx.stroke();
				
				if(ctx.isPointInStroke(x,y)){
					return {node,stagePath:path};
				}
				
				// Hop to next one:
				node=node.next;
				
			}
			
		}
		
		return null;
	}
	
	onMouseUp(e){
		
		if(this.state.freeDraw){
			// Complete the shape.
			var fd = this.state.freeDraw;
			
			var f=new PathFitter(fd.path, 10);
			f.straightTolerance = 10;
			f.fit();
			
			this.testClosed(fd.path,fd.path.latestPathNode);
			
			fd.path.noNodes=false;
			this.redraw();
		}
		
		this.props.onChange && this.props.onChange({
			rawVectorPaths: () => this.state.paths,
			getPathData: remapFunc => this.getPathData(remapFunc)
		});
		
		this.setState({
			click: null,
			freeDraw: null
		});
	}
	
	onMouseDown(e){
		var box = this.canvas.getBoundingClientRect();
		var click = {
			box,
			x: e.clientX - box.x,
			y: e.clientY - box.y
		};
		click.nearBy=this.getNearest(click.x,click.y);
		
		this.setState({
			click
		});
	}
	
	onMouseMove(e){
		let { click } = this.state;
		
		if(!click){
			return;
		}
		
		var point = {
			x: e.clientX - click.box.x,
			y: e.clientY - click.box.y
		};
		
		if(click.nearBy.openSpace){
			
			if(this.state.mode == DRAW){
				
				// Dragging the mouse around.
				if(this.state.freeDraw){
					// Continuing free draw.
					this.state.freeDraw.path.lineTo(point.x, point.y);
					this.redraw();
				}else{
					// Started free draw.
					var path = new VectorPath();
					path.moveTo(point.x, point.y);
					path.noNodes = true;
					
					this.setState({freeDraw: {path}});
					this.state.paths.push(path);
					this.redraw();
					
				}
				
			}
			
		}else if(click.nearBy.node){
			// On a point
			var node = click.nearBy.node;
			
			switch(click.nearBy.control){
				case 0:
					node.x = point.x;
					node.y = point.y;
				break;
				case 1:
					node.control1X = point.x;
					node.control1Y = point.y;
				break;
				case 2:
					node.control2X = point.x;
					node.control2Y = point.y;
				break;
			}
			
			this.redraw();
		}
	}
	
	componentWillUnmount(){
		this.disconnectResizer();
	}
	
	disconnectResizer(){
		if(this.resizer && this.resizer.disconnect){
			this.resizer.disconnect();
			this.resizer = null;
			this.resizerE = null;
		}
	}
	
	render(){
		
		var {width, height} = this.state;
		
		return <div className="shape-draw" style={{width: '100%', height: '100%'}} ref={e => {
			
			if(e){
				if(e != this.resizerE){
					this.disconnectResizer();
					this.resizerE = e;
					this.resizer = new ResizeObserver(eles => {
						if(eles[0] && eles[0].contentRect){
							var rect = eles[0].contentRect;
							
							if(rect.width != this.state.width || rect.height != this.state.height){
								this.setState({width: rect.width, height: rect.height}, () => {
									this.redraw();
								});
							}
							
						}
					});
					this.resizer.observe(e);
				}
			}else{
				this.disconnectResizer();
			}
			
		}}>
			<canvas
				style={
					{
						display: 'none'
					}
				}
				ref={r=>{
					this.inputCanvas = r;
				}}
				width={width}
				height={height}
			/>
			<canvas
				ref={r=>{
					if(r && r != this.canvas){
						this.canvas = r;
						this.canvas_ctx = this.canvas.getContext("2d");
						this.redraw();
					}
				}}
				onMouseDown={e=>{
					if(this.props.readonly){
						return;
					}
					this.onMouseDown(e);
				}}
				onMouseUp={e=>{
					if(this.props.readonly){
						return;
					}
					this.onMouseUp(e);
				}}
				onMouseMove={
					e=>{
						if(this.props.readonly){
							return;
						}
						this.onMouseMove(e);
					}
				}
				width={width}
				height={height}
			/>
		</div>;
		
	}
	
}


ShapeDraw.propTypes = {
	width: 'int',
	height: 'int'
};

ShapeDraw.icon='splotch';