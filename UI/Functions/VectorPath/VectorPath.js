// Vector path functionality.


class VectorPoint{
	
	constructor(x,y){
		this.x=x;
		this.y=y;
		this.next=null;
		this.previous=null;
		this.unloaded=false;
		this.isCurve=false;
		this.hasLine=false;
		this.isClose=false;
	}
	
	move(x,y){
		this.x+=x;
		this.y+=y;
	}
	
	multiply(x,y){
		this.x*=x;
		this.y*=y;
	}
	
	recalculateBounds(path){
		if(this.x<path.minX){
			path.minX=this.x;
		}
		
		if(this.y<path.minY){
			path.minY=this.y;
		}
		
		// Width/height are used as max to save some memory:
		if(this.x>path.width){
			path.width=this.x;
		}
		
		if(this.y>path.height){
			path.height=this.y;
		}
	}
	
	toJson(){
		return {
			type: 'VectorPoint',
			isClose: this.isClose,
			x: this.x,
			y: this.y
		};
	}
	
	getNear(clickX,clickY,path,result, radius){
		return this.getNearPoint(clickX,clickY,path,result, radius);
	}
	
	getNearPoint(clickX,clickY,path,result, radius){
		var dx=this.x - clickX;
		var dy=this.y - clickY;
		radius = radius || 3;
		
		// How close?
		if(dx<radius && dx>-radius){
			if(dy<radius && dy>-radius){
				// Got it!
				result.node=this;
				result.stagePath=path;
				return true;
			}
		}
		
		return false;
	}
	
	draw(drawInfo){
		// Draw:
		this.drawPoint(this.x,this.y,drawInfo);
	}
	
	replaceWith(replacement,path){
		
		if(replacement==null){
			return;
		}
		
		// Update next/prev:
		replacement.next=this.next;
		replacement.previous=this.previous;
		replacement.isClose=this.isClose;
		
		if(this.next==null){
			path.latestPathNode=replacement;
		}else{
			this.next.previous=replacement;
		}
		
		if(this.previous==null){
			path.firstPathNode=replacement;
		}else{
			this.previous.next=replacement;
		}
		
	}
	
	progressAlongFast(x,y,C,D,len_sq){
		var x1=this.previous.x;
		var y1=this.previous.y;
		
		var A = x - x1;
		var B = y - y1;

		var dot = A * C + B * D;
		var param = 0;
		if (len_sq != 0){
			param = dot / len_sq;
		}
		
		return param;
	}
	
	trySelect(selections,region){
		// Contains?
		if(region.contains(this.x,this.y)){
			// Add to selections set:
			selections.push(this);
		}
	}
	
	drawControlPoint(x,y,drawInfo){
		
		var midX=x+drawInfo.x;
		var midY=y+drawInfo.y;
		var ctx=drawInfo.context;
		
		if(!drawInfo.controlPointDrawn){
			// Swap to control:
			drawInfo.controlPointDrawn=true;
			
			if(drawInfo.isReference){
				ctx.strokeStyle=drawInfo.controlRefStroke || "#333333";
				ctx.fillStyle=drawInfo.controlRefFill || "#861dbb";
			}else{
				ctx.strokeStyle=drawInfo.controlStroke || "#1db9bb";
				ctx.fillStyle=drawInfo.controlFill || "#d9f9f9";
			}
		}
		
		var max=2 * Math.PI;
		
		ctx.beginPath();
		ctx.arc(midX,midY,drawInfo.nodeRadius || 2,0,max);
		
		ctx.fill();
		ctx.stroke();
	}
	
	drawPoint(x,y,drawInfo){
		
		var midX=x+drawInfo.x;
		var midY=y+drawInfo.y;
		var ctx=drawInfo.context;
		
		if(drawInfo.controlPointDrawn){
			// Swap to non-control:
			drawInfo.controlPointDrawn=false;
			
			if(drawInfo.isReference){
				ctx.strokeStyle=drawInfo.nodeRefStroke || "#861dbb";
				ctx.fillStyle=drawInfo.nodeRefFill || "#333333";
			}else{
				ctx.strokeStyle=drawInfo.nodeStroke || "#2D898A";
				ctx.fillStyle=drawInfo.nodeFill || "#1db9bb";
			}
			
		}
		
		var max=2 * Math.PI;
		
		ctx.beginPath();
		ctx.arc(midX,midY,drawInfo.nodeRadius || 2,0,max);
		
		ctx.fill();
		ctx.stroke();
		
	}
	
	copy(){
		return null;
	}
	
	toString(round){
		return "";
	}
}



class VectorLine extends VectorPoint{
	
	constructor(x,y){
		super(x,y);
		this.hasLine=true;
		this.length=0;
	}
}

class VectorPath{
	
	constructor(){
		this.minX=0;
		this.minY=0;
		this.width=0;
		this.height=1;
		this.pathNodeCount=0;
		this.closeNode=null;
		this.firstPathNode=null;
		this.latestPathNode=null;
	}
	
	move(x,y){
		
		var current=this.firstPathNode;
		
		while(current!=null){
			
			// Move by min:
			current.move(x,y);
			
			// Hop to the next one:
			current=current.next;
		}
		
	}

	getRelativeTo(maxHeight){
		
		// Recalc bounds:
		this.recalculateBounds();
		
		// How big is it?
		var mX=this.minX;
		var mY=this.minY;
		var height=this.height;
		
		var scaleFactorY=maxHeight / height;
		var scaleFactorX=scaleFactorY<0?-scaleFactorY:scaleFactorY;
		
		// Make a new path:
		var path=new VectorPath();
		
		var current=this.firstPathNode;
		
		while(current!=null){
			
			// Copy it:
			var copiedPoint=current.copy();
			
			// Move by min:
			copiedPoint.move(-mX,-mY);
			
			// Scale by factor:
			copiedPoint.multiply(scaleFactorX,scaleFactorY);
			
			// Add to new path:
			path.addPathNode(copiedPoint);
			
			// Hop to the next one:
			current=current.next;
		}
		
		return path;
	}

	moveTo(x,y){
		
		// We need to add the first end:
		var point=new MoveToPoint(x,y);
		
		this.addPathNode(point);
		
		this.closeNode=point;
		
	}

	copyInto(path){
		
		var point=this.firstPathNode;
		
		while(point!=null){
			
			var copiedPoint=point.copy();
			
			path.addPathNode(copiedPoint);
			
			// Copy close status:
			if(point.isClose){
				
				copiedPoint.isClose=true;
				path.closeNode.closePoint=copiedPoint;
				
			}
			
			point=point.next;
		}
	}

	undoRemove(data){
		
		this.pathNodeCount++;
		
		if(data.replaced!=null){
			
			// We replaced a node with a moveto. Undo:
			var toReplace=this.firstPathNode;
			data.replaced.next=toReplace.next;
			
			// Temp to front:
			this.firstPathNode=data.replaced;
			
		}
		
		var node=data.removed;
		node.next=data.next;
		node.previous=data.previous;
		
		if(data.next==null){
			// Last one:
			this.latestPathNode=node;
		}else{
			data.next.previous=node;
		}
		
		if(data.previous==null){
			// First one:
			this.firstPathNode=node;
		}else{
			data.previous.next=node;
		}
		
	}

	remove(node){
		
		var replaced=null;
		
		this.pathNodeCount--;
		
		if(node.next==null){
			this.latestPathNode=node.previous;
		}else{
			node.next.previous=node.previous;
		}
		
		if(node.previous==null){
			this.firstPathNode=node.next;
			
			if(this.firstPathNode!=null){
				// Must "convert" first into a MoveTo node:
				if(!(this.firstPathNode instanceof MoveToPoint)){
					
					// Get the node:
					var first=this.firstPathNode;
					
					// Create moveto:
					var move=new MoveToPoint(first.x,first.y);
					
					// Add it:
					this.firstPathNode=move;
					move.next=first.next;
					replaced=first;
					
				}
			}
			
		}else{
			node.previous.next=node.next;
		}
		
		// Get next/prev:
		var next=node.next;
		var prev=node.previous;
		
		// Clear them:
		node.next=null;
		node.previous=null;
		
		return {removed:node,replaced,next,previous:prev};
		
	}

	clear(){
		this.firstPathNode=null;
		this.latestPathNode=null;
		this.pathNodeCount=0;
	}
	
	addPathNode(point){
		this.pathNodeCount++;
		
		if(this.firstPathNode==null){
			
			if(point.unloaded){
				
				this.firstPathNode=this.latestPathNode=point;
				
			}else{
				
				if(!(point instanceof MoveToPoint)){
					
					// Add a blank MoveTo - this means that moveTo's are always the close nodes.
					var move=new MoveToPoint(0,0);
					this.firstPathNode=this.latestPathNode=this.move;
					this.closeNode=move;
					
					this.pathNodeCount++;
					
					point.previous=move;
					this.latestPathNode=move.next=point;
					
				}else{
					
					this.firstPathNode=this.latestPathNode=point;
					this.closeNode=move;
					
				}
			
			}
			
		}else{
			
			// Hook it onto the end:
			point.previous=this.latestPathNode;
			this.latestPathNode=this.latestPathNode.next=point;
		}

	}
	
	closePathFast(){

		if(this.closeNode==null || this.latestPathNode==null){
			return;
		}
		
		var point=LineTo(this.closeNode.x,this.closeNode.y);
		point.isClose=true;
		this.closeNode.closePoint=point;
		
	}
	
	closePath(){
		if(this.closeNode==null || this.latestPathNode==null){
			return;
		}
		
		if(this.latestPathNode.x==this.closeNode.x && this.latestPathNode.y==this.closeNode.y){
			this.latestPathNode.isClose=true;
			this.closeNode.closePoint=this.latestPathNode;
		}else{
			var point=this.lineTo(this.closeNode.x,this.closeNode.y);
			point.isClose=true;
			this.closeNode.closePoint=point;
		}
	}
	
	closeLast(){
		if(this.latestPathNode==null){
			return;
		}
		
		this.latestPathNode.isClose=true;
		
		if(this.closeNode!=null){
			this.closeNode.closePoint=this.latestPathNode;
		}
	}
	
	lineTo(x,y){
		// Create the straight line:
		var newNode=new StraightLinePoint(x,y);
		
		// Add it:
		this.addPathNode(newNode);
		
		return newNode;
	}
	
	quadraticCurveTo(cx,cy,x,y){
		
		// Create the curve line:
		var newNode=new QuadLinePoint(x,y);
		
		newNode.control1X=cx;
		newNode.control1Y=cy;
		
		// Add it:
		this.addPathNode(newNode);
		
		return newNode;
	}
	
	draw(drawInfo){
		var ctx = drawInfo.context;
		ctx.lineWidth=drawInfo.lineWidth || 1;
		
		if(this.isReference){
			ctx.strokeStyle=drawInfo.strokeRef || "#861dbb";
		}else{
			ctx.strokeStyle=drawInfo.stroke || "#1db9bb";
		}
		
		ctx.beginPath();
		
		// For each node in the path:
		var node=this.firstPathNode;
		
		while(node!=null){
			
			// Add:
			node.add(drawInfo);
			
			if(node.isClose){
				if(this.isReference){
					ctx.fillStyle=drawInfo.fillRef || "#f5f1fe";
				}else{
					ctx.fillStyle=drawInfo.fill || "#edfcfc";
				}
				
				ctx.closePath();
				ctx.fill();
			}
			
			// Hop to next one:
			node=node.next;
			
		}
		
		ctx.stroke();
		ctx.lineWidth = drawInfo.nodeLineWidth || 1;
		
		if(this.noNodes || drawInfo.noNodes){
			return;
		}
		
		drawInfo.isReference=this.isReference;
		
		// We always draw a non-control point first, so this forces the colour to refresh.
		drawInfo.controlPointDrawn=true;
		
		// For each node in the path:
		var node=this.firstPathNode;
		
		while(node!=null){
			
			// Draw the node:
			node.draw(drawInfo);
			
			// Hop to next one:
			node=node.next;
		}
	}
	
	curveTo(c1x,c1y,c2x,c2y,x,y){
		
		// Create the curve line:
		var newNode=new CurveLinePoint(x,y);
		
		newNode.control1X=c1x;
		newNode.control1Y=c1y;
		newNode.control2X=c2x;
		newNode.control2Y=c2y;
		
		// Add it:
		this.addPathNode(newNode);
		
	}

	recalculateMeta(){
		this.recalculateBounds();
	}

	recalculateBounds(){
		if(this.firstPathNode==null){
			this.width=0;
			this.height=0;
			this.minY=0;
			this.minX=0;
			
			return;
		}
		
		// Our temp boundaries:
		this.minX=99999;
		this.minY=99999;
		
		// We'll be using width/height temporarily as max:
		this.width=-999999;
		this.height=-999999;
		
		var current=this.firstPathNode;
		
		while(current!=null){
			
			// Recalc bounds:
			current.recalculateBounds(this);
			
			// Hop to the next one:
			current=current.next;
		}
		
		// Remove min values from width/height:
		this.width-=this.minX;
		this.height-=this.minY;
	}
	
	unclosed(){
		if(this.closeNode==null){
			return true;
		}
		
		if(this.latestPathNode.x==this.closeNode.x && this.latestPathNode.y==this.closeNode.y){
			return false;
		}
		
		return true;
	}

	toString(prepend,round,csMode){
		var text="";
		
		var current=this.firstPathNode;
		
		while(current!=null){
			
			// Get as str:
			var str=current.toString(round);
			
			if(csMode){
				str=str.replace(/,/gi,"f,").replace(/\)/gi,"f)").replace("quadratic","Quadratic").
				replace("bezier","").replace("move","Move").replace("line","Line");
			}
			
			if(prepend){
				text+=prepend+"."+str+";\r\n";
			}else{
				text+=str+"\r\n";
			}
			
			// Hop to the next one:
			current=current.next;
		}
		
		return text;
	}

	toJson(){
		var result = {
			typeLoader: 'VectorPath',
			points: []
		};
		
		var current=this.firstPathNode;
		var first=true;
		
		while(current!=null){
			result.points.push(current.toJson());
			// Hop to the next one:
			current=current.next;
		}
		
		return result;
	}
}

class MoveToPoint extends VectorPoint{
	
	constructor(x,y){
		super(x,y);
		this.closePoint=null;
	}
	
	toJson(){
		return {
			type: 'MoveToPoint',
			isClose: this.isClose,
			x: this.x,
			y: this.y
		};
	}
	
	copy(){
		var point=new MoveToPoint(this.x,this.y);
		return point;
	}

	toString(round){
		
		if(round){
			return "moveTo("+this.x.toFixed(round)+","+this.y.toFixed(round)+")";
		}
		
		return "moveTo("+this.x+","+this.y+")";
	}
	
	add(drawInfo){
		drawInfo.context.moveTo(this.x+drawInfo.x,this.y+drawInfo.y);
	}
}

class StraightLinePoint extends VectorLine{
	
	constructor(x,y){
		super(x,y);
	}
	
	recalculateBounds(path){
		
		// Get deltas:
		var dx=this.x-this.previous.x;
		var dy=this.y-this.previous.y;
		
		// Length:
		this.length=Math.sqrt((dx*dx)+(dy*dy));
		
		super.recalculateBounds(path);
	}

	toJson(){
		return {
			type: 'StraightLinePoint',
			isClose: this.isClose,
			x: this.x,
			y: this.y
		};
	}

	copy(){
		var point=new StraightLinePoint(this.x,this.y);
		point.length=this.length;
		return point;
	}

	toString(round){
		if(this.isClose){
			return "closePath()";
		}
		
		if(round){
			return "lineTo("+this.x.toFixed(round)+","+this.y.toFixed(round)+")";
		}
		
		return "lineTo("+this.x+","+this.y+")";
	}
	
	add(drawInfo){
		drawInfo.context.lineTo(this.x+drawInfo.x,this.y+drawInfo.y);
	}

	split(x,y,path){
		
		var p3x=this.x;
		var p3y=this.y;
		var node=this;
		
		// Get the "progress" of x/y along the line:
		var C = p3x - this.previous.x;
		var D = p3y - this.previous.y;
		var len_sq = C * C + D * D;
		var t=this.progressAlongFast(x,y,C,D,len_sq);
		
		var results={
			x:p3x,
			y:p3y,
			node,
			splitNode:null,
			
			undo:function(){
				
				// Restore X/Y:
				this.node.x=this.x;
				this.node.y=this.y;
				
				// Remove next:
				path.remove(this.node.next);
				
			}
		};
		
		// Get the new points coords:
		this.x=this.previous.x + t * C;
		this.y=this.previous.y + t * D;
		
		// Create the next one:
		var pt=new StraightLinePoint(p3x,p3y);
		
		path.pathNodeCount++;
		
		// Insert after this:
		if(this.next==null){
			path.latestPathNode=pt;
		}else{
			pt.next=this.next;
			this.next.previous=pt;
		}
		
		pt.orevious=this;
		this.next=pt;
		
		results.splitNode=pt;
		
		return results;
	}

	addControl(x,y,path){
		
		// Create:
		var pt=new QuadLinePoint(this.x,this.y);
		pt.control1X=x;
		pt.control1Y=y;
		
		// Remove this and add pt in it's place:
		this.replaceWith(pt,path);
		
		return {control:pt,id:1,replaced:this};
	}
}

class QuadLinePoint extends VectorLine{
	
	constructor(x,y){
		super(x,y);
		this.isCurve=true;
		this.control1X=0;
		this.control1Y=0;
		
	}

	move(x,y){
		this.x+=x;
		this.y+=y;
		this.control1X+=x;
		this.control1Y+=y;
	}

	multiply(x,y){
		this.x*=x;
		this.y*=y;
		this.control1X*=x;
		this.control1Y*=y;
	}
	
	recalculateBounds(path){
		
		// Take control point into account too:
		if(this.control1X<path.minX){
			path.minX=this.control1X;
		}
		
		if(this.control1Y<path.minY){
			path.minY=this.control1Y;
		}
		
		// Width/height are used as max to save some memory:
		if(this.control1X>path.width){
			path.width=this.control1X;
		}
		
		if(this.control1Y>path.height){
			path.height=this.control1Y;
		}
		
		// Start figuring out the length..
		var vaX=this.previous.x-(2*this.control1X)+this.x;
		var vaY=this.previous.y-(2*this.control1Y)+this.y;
		
		var vbX=(2*this.control1X) - (2*this.previous.x);
		var vbY=(2*this.control1Y) - (2*this.previous.y);
		
		var a=4*((vaX*vaX) + (vaY*vaY));
		
		var b=4*((vaX*vbX) + (vaY*vbY));
		
		var c=(vbX*vbX) + (vbY*vbY);
		
		var rootABC = 2*Math.sqrt(a+b+c);
		var rootA = Math.sqrt(a);
		var aRootA = 2*a*rootA;
		
		if(aRootA==0){
			
			this.length=0;
			
		}else{
			
			var rootC = 2*Math.sqrt(c);
			var bA = b/rootA;
			
			this.length=(
				aRootA * rootABC + rootA*b*(rootABC-rootC) + (4*c*a - b*b)*Math.log(
					(2*rootA+bA+rootABC) / (bA+rootC)
				)
			) / (4*aRootA);
		
		}
		
		super.recalculateBounds(path);
	}
	
	split(x,y,path){
		
		// Get the "progress" of x/y along the line:
		var C = this.x - this.previous.x;
		var D = this.y - this.previous.y;
		var len_sq = C * C + D * D;
		var t=this.progressAlongFast(x,y,C,D,len_sq);
		
		var invert=1-t;
		
		var p0x=this.previous.x;
		var p0y=this.previous.y;
		
		var p1x=this.control1X;
		var p1y=this.control1Y;
		
		var p2x=this.x;
		var p2y=this.y;
		var node=this;
		
		var results={
			control1X:p1x,
			control1Y:p1y,
			x:p3x,
			y:p3y,
			node,
			splitNode:null,
			
			undo:function(){
				
				// Restore control points:
				this.node.control1X=this.control1X;
				this.node.control1Y=this.control1Y;
				
				// Restore X/Y:
				this.node.x=this.x;
				this.node.y=this.y;
				
				// Remove next:
				path.remove(this.node.next);
				
			}
		};
		
		// The new points:
		var p3x=p0x * invert + p1x * t;
		var p3y=p0y * invert + p1y * t;
		
		var p4x=p1x * invert + p2x * t;
		var p4y=p1y * invert + p2y * t;
		
		var p5x=p3x * invert + p4x * t;
		var p5y=p3y * invert + p4y * t;
		
		// This curve will become the new 1st half:
		this.control1X=p3x;
		this.control1Y=p3y;
		
		this.x=p5x;
		this.y=p5y;
		
		// Create the next one:
		var pt=new QuadLinePoint(p2x,p2y);
		results.splitNode=pt;
		
		pt.control1X=p4x;
		pt.control1Y=p4y;
		
		path.pathNodeCount++;
		
		// Insert after this:
		if(this.next==null){
			path.latestPathNode=pt;
		}else{
			pt.next=this.next;
			this.next.previous=pt;
		}
		
		pt.previous=this;
		this.next=pt;
		
		return results;
	}

	toJson(){
		return {
			type: 'QuadLinePoint',
			isClose: this.isClose,
			x: this.x,
			y: this.y,
			control1X: this.control1X,
			control1Y: this.control1Y
		};
	}

	copy(){
		var point=new QuadLinePoint(this.x,this.y);
		point.length=this.length;
		
		point.control1X=this.control1X;
		point.control1Y=this.control1Y;
		
		return point;
	}
	
	getNear(clickX,clickY,path,result, radius){
		radius = radius || 3;
		// This point?
		var res=super.getNearPoint(clickX,clickY,path,result, radius);
		
		if(res){
			return true;
		}
		
		// Control 1:
		var dx=this.control1X - clickX;
		var dy=this.control1Y - clickY;
		
		// How close?
		if(dx<radius && dx>-radius){
			if(dy<radius && dy>-radius){
				// Got it!
				result.control=1;
				result.node=this;
				result.stagePath=path;
				return true;
			}
		}
		
		return false;
	}

	trySelect(selections,region){
		// Contains?
		if(region.contains(this.x,this.y)){
			// Add to selections set:
			selections.push(this);
		}
		
		// Control 1?
		if(region.contains(this.control1X,this.control1Y)){
			// Add control to selections set:
			selections.push({control:1,node:this});
		}
		
	}
	
	draw(drawInfo){
		
		// Draw the control point:
		this.drawControlPoint(this.control1X,this.control1Y,drawInfo);
		
		// Draw main point:
		this.drawPoint(this.x,this.y,drawInfo);
		
	}

	toString(round){
		
		if(round){
			return "quadraticCurveTo("+this.control1X.toFixed(round)+","+this.control1Y.toFixed(round)+","+this.x.toFixed(round)+","+this.y.toFixed(round)+")";
		}
		
		return "quadraticCurveTo("+this.control1X+","+this.control1Y+","+this.x+","+this.y+")";
	}

	add(drawInfo){
		drawInfo.context.quadraticCurveTo(this.control1X+drawInfo.x,this.control1Y+drawInfo.y,this.x+drawInfo.x,this.y+drawInfo.y);
	}
		
	deleteControl(id,path){
		// Create:
		var pt=new StraightLinePoint(this.x,this.y);
		
		var removeInfo={
			controlX:0,
			controlY:0,
			path,
			id,
			replacement:pt,
			replaced:this
		};
		
		// Deleting control point 1 (only is 1 anyway!)
		removeInfo.controlX=this.control1X;
		removeInfo.controlY=this.control1Y;
		
		// Remove this and add in it's place:
		this.replaceWith(pt,path);
		
		return removeInfo;	
	}

	addControl(x,y,path){
		
		// Create:
		var pt=new CurveLinePoint(this.x,this.y);
		
		// Get the "progress" of x/y along the line, vs control point progress.
		var C = this.x - this.previous.x;
		var D = this.y - this.previous.y;
		var len_sq = C * C + D * D;
		
		var newProg=this.progressAlongFast(x,y,C,D,len_sq);
		var p1Prog=this.progressAlongFast(this.control1X,this.control1Y,C,D,len_sq);
		
		// Should this new control be control point #1?
		var first=(newProg < p1Prog);
		var id;
		
		if(first){
			
			// Pt 1:
			pt.control1X=x;
			pt.control1Y=y;
			
			pt.control2X=this.control1X;
			pt.control2Y=this.control1Y;
			
			id=1;
			
		}else{
			
			// Pt 2:
			pt.control1X=this.control1X;
			pt.control1Y=this.control1Y;
			
			pt.control2X=x;
			pt.control2Y=y;
			
			id=2;
			
		}
		
		// Remove this and add in it's place:
		this.replaceWith(pt,path);
		
		return {control:pt,id,replaced:this};	
	}
}

class CurveLinePoint extends QuadLinePoint{
	
	constructor(x,y){
		super(x,y);
		this.control2X=0;
		this.control2Y=0;
	}
	
	move(x,y){
		this.x+=x;
		this.y+=y;
		this.control1X+=x;
		this.control1Y+=y;
		this.control2X+=x;
		this.control2Y+=y;
	}

	multiply(x,y){
		this.x*=x;
		this.y*=y;
		this.control1X*=x;
		this.control1Y*=y;
		this.control2X*=x;
		this.control2Y*=y;
	}

	recalculateBounds(path){
		// Take control point into account too:
		if(this.control2X<path.minX){
			path.minX=this.control2X;
		}
		
		if(this.control2Y<path.minY){
			path.minY=this.control2Y;
		}
		
		// Width/height are used as max to save some memory:
		if(this.control2X>path.width){
			path.width=this.control2X;
		}
		
		if(this.control2Y>path.height){
			path.height=this.control2Y;
		}
		
		super.recalculateBounds(path);
	}

	toJson(){
		return {
			type: 'CurveLinePoint',
			isClose: this.isClose,
			x: this.x,
			y: this.y,
			control1X: this.control1X,
			control1Y: this.control1Y,
			control2X: this.control2X,
			control2Y: this.control2Y
		};
	}

	copy(){
		
		var point=new CurveLinePoint(this.x,this.y);
		point.length=this.length;
		
		point.control1X=this.control1X;
		point.control1Y=this.control1Y;
		point.control2X=this.control2X;
		point.control2Y=this.control2Y;
		
		return point;
	}

	deleteControl(id,path){
		
		// Create:
		var pt=new QuadLinePoint(this.x,this.y);
		
		var removeInfo={
			controlX:0,
			controlY:0,
			path,
			id,
			replacement:pt,
			replaced:this
		};
		
		if(removeInfo.id==1){
			
			// Deleting control point 1.
			pt.control1X=this.control2X;
			pt.control1Y=this.control2Y;
			
			removeInfo.controlX=this.control1X;
			removeInfo.controlY=this.control1Y;
			
		}else{
			
			// Deleting control point 2.
			pt.control1X=this.control1X;
			pt.control1Y=this.control1Y;
			
			removeInfo.controlX=this.control2X;
			removeInfo.controlY=this.control2Y;
			
		}
		
		// Remove this and add in it's place:
		this.replaceWith(pt,path);
		
		return removeInfo;
	}
	
	split(x,y,path){
		
		// Get the "progress" of x/y along the line:
		var C = this.x - this.previous.x;
		var D = this.y - this.previous.y;
		var len_sq = C * C + D * D;
		var t=this.progressAlongFast(x,y,C,D,len_sq);
		
		var invert=1-t;
		
		var p0x=this.previous.x;
		var p0y=this.previous.y;
		
		var p1x=this.control1X;
		var p1y=this.control1Y;
		
		var p2x=this.control2X;
		var p2y=this.control2Y;
		
		var p3x=this.x;
		var p3y=this.y;
		var node=this;
		
		var results={
			control1X:p1x,
			control1Y:p1y,
			control2X:p2x,
			control2Y:p2y,
			x:p3x,
			y:p3y,
			node,
			splitNode:null,
			
			undo:function(){
				
				// Restore control points:
				this.node.control1X=this.control1X;
				this.node.control1Y=this.control1Y;
				this.node.control2X=this.control2X;
				this.node.control2Y=this.control2Y;
				
				// Restore X/Y:
				this.node.x=this.x;
				this.node.y=this.y;
				
				// Remove next:
				path.remove(this.node.next);
				
			}
		};
		
		// The new points:
		var p4x=p0x * invert + p1x * t;
		var p4y=p0y * invert + p1y * t;
		
		var p5x=p1x * invert + p2x * t;
		var p5y=p1y * invert + p2y * t;
		
		var p6x=p2x * invert + p3x * t;
		var p6y=p2y * invert + p3y * t;
		
		var p7x=p4x * invert + p5x * t;
		var p7y=p4y * invert + p5y * t;
		
		var p8x=p5x * invert + p6x * t;
		var p8y=p5y * invert + p6y * t;
		
		var p9x=p7x * invert + p8x * t;
		var p9y=p7y * invert + p8y * t;
		
		
		// This curve will become the new 1st half:
		this.control1X=p4x;
		this.control1Y=p4y;
		
		this.control2X=p7x;
		this.control2Y=p7y;
		
		this.x=p9x;
		this.y=p9y;
		
		// Create the next one:
		var pt=new CurveLinePoint(p3x,p3y);
		results.splitNode=pt;
		
		pt.control1X=p8x;
		pt.control1Y=p8y;
		pt.control2X=p6x;
		pt.control2Y=p6y;
		
		path.pathNodeCount++;
		
		// Insert after this:
		if(this.next==null){
			path.latestPathNode=pt;
		}else{
			pt.next=this.next;
			this.next.previous=pt;
		}
		
		pt.previous=this;
		this.next=pt;
		
		return results;
	}

	addControl(x,y,path){
		
		// Just split the line:
		var splitNode=this.split(x,y,path);
		
		return {control:this,id:0,split:splitNode};
	}

	getNear(clickX,clickY,path,result, radius){
		radius = radius || 3;
		// This point?
		var res=super.getNearPoint(clickX,clickY,path,result, radius);
		
		if(res){
			return true;
		}
		
		// Control 1:
		var dx=this.control1X - clickX;
		var dy=this.control1Y - clickY;
		
		// How close?
		if(dx<radius && dx>-radius){
			if(dy<radius && dy>-radius){
				// Got it!
				result.control=1;
				result.node=this;
				result.stagePath=path;
				return true;
			}
		}
		
		// Control 2:
		var dx=this.control2X - clickX;
		var dy=this.control2Y - clickY;
		
		// How close?
		if(dx<radius && dx>-radius){
			if(dy<radius && dy>-radius){
				// Got it!
				result.control=2;
				result.node=this;
				result.stagePath=path;
				return true;
			}
		}
		
		return false;
	}
	
	trySelect(selections,region){
		// Contains?
		if(region.contains(this.x,this.y)){
			// Add to selections set:
			selections.push(this);
		}
		
		// Control 1?
		if(region.contains(this.control1X,this.control1Y)){
			// Add control to selections set:
			selections.push({control:1,node:this});
		}
		
		// Control 2?
		if(region.contains(this.control2X,this.control2Y)){
			// Add control to selections set:
			selections.push({control:2,node:this});
		}
		
	}

	draw(drawInfo){
		// Draw the control points:
		this.drawControlPoint(this.control1X,this.control1Y,drawInfo);
		this.drawControlPoint(this.control2X,this.control2Y,drawInfo);
		
		// Draw main point:
		this.drawPoint(this.x,this.y,drawInfo);
	}

	toString(round){
		
		if(round){
			return "bezierCurveTo("+this.control1X.toFixed(round)+","+this.control1Y.toFixed(round)+","+this.control2X.toFixed(round)+","+this.control2Y.toFixed(round)+","+this.x.toFixed(round)+","+this.y.toFixed(round)+")";
		}
		
		return "bezierCurveTo("+this.control1X+","+this.control1Y+","+this.control2X+","+this.control2Y+","+this.x+","+this.y+")";
	}

	add(drawInfo){
		drawInfo.context.bezierCurveTo(this.control1X+drawInfo.x,this.control1Y+drawInfo.y,this.control2X+drawInfo.x,this.control2Y+drawInfo.y,this.x+drawInfo.x,this.y+drawInfo.y);
	}

}


function Vector2(x,y){
	
	this.x=x;
	this.y=y;
	
	this.getDistance=function(x,y){
		var dx = x - this.x;
		var dy = y - this.y;
		var d = dx * dx + dy * dy;
		
		return Math.sqrt(d);
	};
	
	this.getDistancePoint=function(p){
		var x = p.x - this.x;
		var y = p.y - this.y;
		var d = x * x + y * y;
		
		return Math.sqrt(d);
	};
	
	this.normalize=function(){
		
		var length=Math.sqrt( (this.x * this.x) + (this.y * this.y) );
		if(length==0){
			return new Vector2(0,0);
		}
		
		return new Vector2(this.x / length, this.y / length);
	};
	
	this.normalizeTo=function(l){
		
		var length=Math.sqrt( (this.x * this.x) + (this.y * this.y) ) / l;
		
		if(length==0){
			return new Vector2(0,0);
		}
		
		return new Vector2(this.x / length, this.y / length);
	};
	
	this.normalizeIn=function(){
		
		var length=Math.sqrt( (this.x * this.x) + (this.y * this.y) );
		
		if(length==0){
			this.x=0;
			this.y=0;
		}else{
			this.x /= length;
			this.y /= length;
		}
		
	};
	
	// Distance of this point from a line
	this.lineDistancePoints=function(line1,line2){
		
		var C = line2.x - line1.x;
		var D = line2.y - line1.y;
		var len_sq = C * C + D * D;
		return this.lineDistancePointsFast(line1,D,C,len_sq);
		
	};
	
	// Line distance using known line values
	this.lineDistancePointsFast=function(line1,D,C,len_sq){
		
		var x1=line1.x;
		var y1=line1.y;
		
		var A = this.x - x1;
		var B = this.y - y1;

		var dot = A * C + B * D;
		var param = 0;
		if (len_sq != 0){
			param = dot / len_sq;
		}
		
		var xx = x1 + param * C;
		var yy = y1 + param * D;
		
		var dx = this.x - xx;
		var dy = this.y - yy;
		
		return Math.sqrt(dx * dx + dy * dy);
	};
	
	this.divide=function(x,y){
		return new Vector2(this.x/x,this.y/y);
	};

	this.dot=function(x,y){
		return this.x*x + this.y*y;
	};

	this.dotPoint=function(p){
		return this.x*p.x + this.y*p.y;
	};

	this.multiply=function(x,y){
		return new Vector2(this.x*x,this.y*y);
	};

	this.add=function(x,y){
		return new Vector2(this.x+x,this.y+y);
	};

	this.subtract=function(x,y){
		return new Vector2(this.x-x,this.y-y);
	};
	
	this.negate=function(){
		return new Vector2(-this.x,-this.y);
	};
	
	this.addPoint=function(p){
		return new Vector2(this.x+p.x,this.y+p.y);
	};

	this.subtractPoint=function(p){
		return new Vector2(this.x-p.x,this.y-p.y);
	};
	
	this.divideIn=function(x,y){
		this.x/=x;
		this.y/=y;
	};
	
	this.multiplyIn=function(x,y){
		this.x*=x;
		this.y*=y;
	};

	this.addIn=function(x,y){
		this.x+=x;
		this.y+=y;
	};

	this.subtractIn=function(x,y){
		this.x-=x;
		this.y-=y;
	};
	
	this.addInPoint=function(p){
		this.x+=p.x;
		this.y+=p.y;
	};

	this.subtractInPoint=function(p){
		this.x-=p.x;
		this.y-=p.y;
	};
	
}

// An Algorithm for Automatically Fitting Digitized Curves
// by Philip J. Schneider
// from "Graphics Gems", Academic Press, 1990
// Modifications and optimisations of original algorithm by Juerg Lehni.
// Modified some more by Luke Briggs for the Blade Engine Editor.
function PathFitter(path, error){
	
	this.EPSILON = 1e-12;
	this.TOLERANCE = 1e-6;
	this.points = [];
	this.path = path;
	// Straight line tolerance, in pixels.
	this.straightTolerance=2;
	var pt=path.firstPathNode;
	
	while(pt!=null){
		this.points.push(new Vector2(pt.x,pt.y));
		pt=pt.next;
	}
	
	this.error = error;
    
    this.fit=function() {
        var points = this.points;
		var length = this.points.length;
		this.path.clear();
		
		var pt=points[0];
		this.path.addPathNode(new MoveToPoint(pt.x,pt.y));
		
        if (length > 1){
			
			var leftTangent=points[1].subtractPoint(pt);
			leftTangent.normalizeIn();
			
			var rightTangent=points[length - 2].subtractPoint(points[length - 1]);
			rightTangent.normalizeIn();
			
            this.fitCubic(0, length - 1,leftTangent,rightTangent);
		}
		
        return this.path;
    };

    // Fit a Bezier curve to a (sub)set of digitized points
    this.fitCubic=function(first, last, tan1, tan2) {
        //  Use heuristic if region only has two points in it
        if (last - first == 1) {
            var pt1 = this.points[first];
            var pt2 = this.points[last];
            var dist = pt1.getDistancePoint(pt2) / 3;
			
			var tan1Norm=tan1.normalizeTo(dist);
			var tan2Norm=tan2.normalizeTo(dist);
			
			tan1Norm.addInPoint(pt1);
			tan2Norm.addInPoint(pt2);
			
			this.addCurve([pt1,tan1Norm,tan2Norm,pt2]);
			
            return;
        }
		
        // Parameterize points, and attempt to fit curve
        var uPrime = this.chordLengthParameterize(first, last),
            maxError = Math.max(this.error, this.error * this.error),
            split;
        // Try 4 iterations
        for (var i = 0; i <= 4; i++) {
            var curve = this.generateBezier(first, last, uPrime, tan1, tan2);
            //  Find max deviation of points to fitted curve
            var max = this.findMaxError(first, last, curve, uPrime);
            if (max.error < this.error) {
                this.addCurve(curve);
                return;
            }
            split = max.index;
            // If error not too large, try reparameterization and iteration
            if (max.error >= maxError)
                break;
            this.reparameterize(first, last, uPrime, curve);
            maxError = max.error;
        }
		
        // Fitting failed -- split at max error point and fit recursively
        var V1 = this.points[split - 1].subtractPoint(this.points[split]);
        var V2 = this.points[split].subtractPoint(this.points[split + 1]);
			
        var tanCenter = V1;
		tanCenter.addIn(V2.x,V2.y);
		tanCenter.divideIn(2,2);
		tanCenter.normalizeIn();
		
        this.fitCubic(first, split, tan1, tanCenter);
        this.fitCubic(split, last, tanCenter.negate(), tan2);
		
    };

    this.addCurve=function(curve) {
        
		// Add:
		var start=curve[0];
		var c1=curve[1];
		var c2=curve[2];
		var end=curve[3];
		
		// Is it straight?
		var C = end.x - start.x;
		var D = end.y - start.y;
		var len_sq = C * C + D * D;
		
		var d1=c1.lineDistancePointsFast(start,D,C,len_sq);
		var d2=c2.lineDistancePointsFast(start,D,C,len_sq);
		
		if(d1<=this.straightTolerance && d2<=this.straightTolerance){
			
			// Straight
			var node=new StraightLinePoint(end.x,end.y);
			
		}else{
			
			// 2 control point curve
			var node=new CurveLinePoint(end.x,end.y);
			
			node.control1X=c1.x;
			node.control1Y=c1.y;
			node.control2X=c2.x;
			node.control2Y=c2.y;
			
		}
		
		this.path.addPathNode(node);
		
    };

    // Use least-squares method to find Bezier control points for region.
    this.generateBezier=function(first, last, uPrime, tan1, tan2) {
        var epsilon = /*#=*/this.EPSILON,
            pt1 = this.points[first],
            pt2 = this.points[last],
            // Create the C and X matrices
            C = [[0, 0], [0, 0]],
            X = [0, 0];

        for (var i = 0, l = last - first + 1; i < l; i++) {
            var u = uPrime[i],
                t = 1 - u,
                b = 3 * u * t,
                b0 = t * t * t,
                b1 = b * t,
                b2 = b * u,
                b3 = u * u * u;
            var a1 = tan1.normalizeTo(b1);
            var a2 = tan2.normalizeTo(b2);
			
			b0+=b1;
			b2+=b3;
			
            var tmp = this.points[first + i].subtract(pt1.x * b0,pt1.y * b0);
			tmp.subtractIn(pt2.x * b2,pt2.y * b2);
			
            C[0][0] += a1.dotPoint(a1);
            C[0][1] += a1.dotPoint(a2);
            // C[1][0] += a1.dotPoint(a2);
            C[1][0] = C[0][1];
            C[1][1] += a2.dotPoint(a2);
            X[0] += a1.dotPoint(tmp);
            X[1] += a2.dotPoint(tmp);
        }

        // Compute the determinants of C and X
        var detC0C1 = C[0][0] * C[1][1] - C[1][0] * C[0][1],
            alpha1, alpha2;
        if (Math.abs(detC0C1) > epsilon) {
            // Kramer's rule
            var detC0X  = C[0][0] * X[1]    - C[1][0] * X[0],
                detXC1  = X[0]    * C[1][1] - X[1]    * C[0][1];
            // Derive alpha values
            alpha1 = detXC1 / detC0C1;
            alpha2 = detC0X / detC0C1;
        } else {
            // Matrix is under-determined, try assuming alpha1 == alpha2
            var c0 = C[0][0] + C[0][1],
                c1 = C[1][0] + C[1][1];
            if (Math.abs(c0) > epsilon) {
                alpha1 = alpha2 = X[0] / c0;
            } else if (Math.abs(c1) > epsilon) {
                alpha1 = alpha2 = X[1] / c1;
            } else {
                // Handle below
                alpha1 = alpha2 = 0;
            }
        }

        // If alpha negative, use the Wu/Barsky heuristic (see text)
        // (if alpha is 0, you get coincident control points that lead to
        // divide by zero in any subsequent NewtonRaphsonRootFind() call.
        var segLength = pt2.getDistancePoint(pt1);
        epsilon *= segLength;
        if (alpha1 < epsilon || alpha2 < epsilon) {
            // fall back on standard (probably inaccurate) formula,
            // and subdivide further if needed.
            alpha1 = alpha2 = segLength / 3;
        }

        // First and last control points of the Bezier curve are
        // positioned exactly at the first and last data points
        // Control points 1 and 2 are positioned an alpha distance out
        // on the tangent vectors, left and right, respectively
        return [pt1, pt1.addPoint(tan1.normalizeTo(alpha1)),
                pt2.addPoint(tan2.normalizeTo(alpha2)), pt2];
    };

    // Given set of points and their parameterization, try to find
    // a better parameterization.
    this.reparameterize=function(first, last, u, curve) {
        for (var i = first; i <= last; i++) {
            u[i - first] = this.findRoot(curve, this.points[i], u[i - first]);
        }
    };

    // Use Newton-Raphson iteration to find better root.
    this.findRoot=function(curve, point, u) {
        var curve1 = [],
            curve2 = [];
        
		// Generate control vertices for Q'
        for (var i = 0; i <= 2; i++) {
			var dupe=curve[i + 1].subtractPoint(curve[i]);
			dupe.multiplyIn(3,3);
            curve1[i] = dupe;
        }
        
		// Generate control vertices for Q''
        for (var i = 0; i <= 1; i++) {
			var dupe=curve1[i + 1].subtractPoint(curve1[i]);
			dupe.multiplyIn(2,2);
            curve2[i] = dupe;
        }
		
        // Compute Q(u), Q'(u) and Q''(u)
        var pt = this.evaluate(3, curve, u),
            pt1 = this.evaluate(2, curve1, u),
            pt2 = this.evaluate(1, curve2, u);
            pt.subtractIn(point.x,point.y);
            df = pt1.dotPoint(pt1) + pt.dotPoint(pt2);
        // Compute f(u) / f'(u)
        if (Math.abs(df) < /*#=*/this.TOLERANCE)
            return u;
        // u = u - f(u) / f'(u)
        return u - pt.dotPoint(pt1) / df;
    };

    // Evaluate a bezier curve at a particular parameter value
    this.evaluate=function(degree, curve, t) {
        // Copy array
        var tmp = curve.slice();
		
		var inverse=1-t;
		
        // Triangle computation
        for (var i = 1; i <= degree; i++) {
            for (var j = 0; j <= degree - i; j++) {	
				
				var dupe=tmp[j].multiply(inverse,inverse);
				
				var dupeB=tmp[j + 1].multiply(t,t);
				
				dupe.addIn(dupeB.x,dupeB.y);
				
                tmp[j] = dupe;
            }
        }
		
        return tmp[0];
    };

    // Assign parameter values to digitized points
    // using relative distances between points.
    this.chordLengthParameterize= function(first, last) {
        var u = [0];
        for (var i = first + 1; i <= last; i++) {
            u[i - first] = u[i - first - 1]
                    + this.points[i].getDistancePoint(this.points[i - 1]);
        }
        for (var i = 1, m = last - first; i <= m; i++) {
            u[i] /= u[m];
        }
        return u;
    };

    // Find the maximum squared distance of digitized points to fitted curve.
    this.findMaxError= function(first, last, curve, u) {
        var index = Math.floor((last - first + 1) / 2),
            maxDist = 0;
        for (var i = first + 1; i < last; i++) {
            var v = this.evaluate(3, curve, u[i - first]);
            v.subtractInPoint(this.points[i]);
            var dist = v.x * v.x + v.y * v.y; // squared
            if (dist >= maxDist) {
                maxDist = dist;
                index = i;
            }
        }
        return {
            error: maxDist,
            index: index
        };
    };
	
}

export {
	VectorPoint,
	MoveToPoint,
	StraightLinePoint,
	QuadLinePoint,
	CurveLinePoint,
	VectorPath,
	PathFitter,
	Vector2
};

