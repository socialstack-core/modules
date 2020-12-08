var THREE = require('UI/Functions/ThreeJs/ThreeJs.js');

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
		var vector = this.vector;
		
		var props = this.props;
		vector.set(props.x, props.y, props.z);
		
		var widthHalf = 0.5 * renderer.w;
		var heightHalf = 0.5 * renderer.h;
		vector.project(camera);
		vector.x = ( vector.x * widthHalf ) + widthHalf;
		vector.y = -( vector.y * heightHalf ) + heightHalf;
		
		if(vector.x != this.vectorX || vector.y != this.vectorY){
			this.vectorX = vector.x;
			this.vectorY = vector.y;
			this.setState({
				style: {
					left: vector.x + 'px',
					top: vector.y + 'px'
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
