import * as THREE from 'UI/Functions/ThreeJs';

export default class Billboard extends React.Component {
	
	constructor(props){
		super(props);
		this.state={style: {}};
		this.vector = new THREE.Vector3();
		this.refChange = this.refChange.bind(this);
		this.onSceneRender = this.onSceneRender.bind(this);
	}
	
	refChange(ref){
		if(!ref){
			return;
		}
		this.ref = ref;
		this.props.onDivRef && this.props.onDivRef(ref);
		this.setup(this.props);
	}
	
	componentDidMount(){
		this.setup(this.props);
	}
	
	componentWillReceiveProps(props){
		this.setup(props);
	}
	
	componentWillUnmount(){
		if(!this.isSetup){
			return;
		}
		var scene = global.scene;
		scene.onRender = scene.onRender.filter(f => f != this.onSceneRender);
	}
	
	setup(props){
		var ref = this.ref;
		var scene = global.scene;
		if(!ref || !scene || this.isSetup){
			return;
		}
		this.isSetup = true;
		scene.onRender.push(this.onSceneRender);
	}
	
	onSceneRender(deltaTime, renderer, camera, scene){
		
		var props = this.props;
		var vector = this.vector;
		
		if(props.objectName){
			// Must set an object to scene.billboards
			if(!scene.billboards){
				return;
			}
			
			if(!this._obj || this._obj.name != props.objectName){
				this._obj = scene.billboards.getObjectByName(props.objectName);
			}
			
			if(!this._obj){
				return;
			}
			
			vector.copy(this._obj.position);
		}else{
			vector.set(props.x, props.y, props.z);
		}
		
		camera._dirHelper.subVectors( vector, camera.position ).normalize();
		
		var widthHalf = 0.5 * renderer.w;
		var heightHalf = 0.5 * renderer.h;
		vector.project(camera);
		vector.x = ( vector.x * widthHalf ) + widthHalf;
		vector.y = ( vector.y * heightHalf ) + heightHalf;
		
		var visible = camera._dirHelper.dot(camera._direction) > 0;
		
		if(vector.x != this.vectorX || vector.y != this.vectorY || visible != this.visible){
			this.vectorX = vector.x;
			this.vectorY = vector.y;
			this.visible = visible;
			this.setState({
				style: {
					display: this.visible == 1 ? 'block': 'none',
					left: vector.x + 'px',
					bottom: vector.y + 'px'
				}
			});
		}
	}
	
	render(){
		var { children, className } = this.props;
		var { style } = this.state;
		
		var El = this.props.element || "div";
		
		if(!children){
			return <El className = {className} ref={this.refChange} style={style} />;
		}
		
		return <El className = {className} ref={this.refChange} style={style}>{children}</El>;
	}
	
}
