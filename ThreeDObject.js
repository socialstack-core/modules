var THREE = require('UI/Functions/ThreeJs/ThreeJs.js');


export default class ThreeDObject extends React.Component {
	
	constructor(props){
		super(props);
		this.refChange = this.refChange.bind(this);
	}
	
	componentWillUnmount(){
		var scene = global.scene;
		scene && this.obj && scene.remove(this.obj);
		this.obj = null;
	}
	
	componentDidMount(){
		this.setup(this.props);
	}
	
	refChange(ref){
		if(!ref){
			return;
		}
		this.ref = ref;
		this.setup(this.props);
		this.props.onDivRef && this.props.onDivRef(ref);
	}
	
	setup(props){
		var ref = this.ref;
		var scene = global.scene;
		if(!ref || !scene){
			return;
		}
		
		if(this.obj){
			if(this.obj.element == ref){
				this.transform(props);
				return;
			}
			scene && scene.remove(this.obj);
		}
		
		this.obj = new THREE.CSS3DObject(ref);
		scene && scene.add(this.obj);
		
		this.transform(props);
	}
	
	transform(props){
		var obj = this.obj;
		var {position, rotation, scale} = props;
		
		if(position){
			obj.position.x = position.x || 0;
			obj.position.y = position.y || 0;
			obj.position.z = position.z || 0;
		}	
		
		if(rotation){
			obj.rotation.x = rotation.x || 0;
			obj.rotation.y = rotation.y || 0;
			obj.rotation.z = rotation.z || 0;
		}
		
		if(scale){
			obj.scale.x = scale.x || 1;
			obj.scale.y = scale.y || 1;
			obj.scale.z = scale.z || 1;
		}
	}
	
	render(){
		var { children } = this.props;
		
		if(this.obj){
			this.transform(this.props);
		}
		
		if(!children){
			return <div ref={this.refChange} />;
		}
		
		return <div ref={this.refChange}>{children}</div>;
	}
	
}