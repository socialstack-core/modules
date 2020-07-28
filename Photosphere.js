var THREE = require('UI/Functions/ThreeJs/ThreeJs.js');
import getRef from 'UI/Functions/GetRef';
import DeviceOrientationControls from 'UI/Functions/DeviceOrientationControls';

const SphereContext = React.createContext(null);

export default class Photosphere extends React.Component {
	
	constructor(props){
		super(props);
		var scene = new THREE.Scene();
		this.state = {
			scene,
			size: {}
		};
		
		// Expose the three.js scenegraph to inspection tools
		global.scene = scene;
		
		this.setRef = this.setRef.bind(this);
		this.set3DRef = this.set3DRef.bind(this);
		this.setCanvasRef = this.setCanvasRef.bind(this);
		this.onMouseUp = this.onMouseUp.bind(this);
		this.onMouseDown = this.onMouseDown.bind(this);
		this.onMouseMove = this.onMouseMove.bind(this);
		this.animate = this.animate.bind(this);
		this.onLoaded = this.onLoaded.bind(this);
	}
	
	componentWillReceiveProps(props){
		this.setup(props);
	}
	
	setRef(ref){
		this.hostEle = ref;
		this.setup(this.props);
	}
	
	set3DRef(ref){
		this.root3DEle = ref;
		this.setup(this.props);
	}
	
	setCanvasRef(ref){
		this.canvasEle = ref;
		this.setup(this.props);
	}
	
	/*
	* Makes the host element go fullscreen
	*/
	fs(){
		var elem = this.hostEle;
		if (elem.requestFullscreen) {
			elem.requestFullscreen();
		} else if (elem.mozRequestFullScreen) { /* Firefox */
			elem.mozRequestFullScreen();
		} else if (elem.webkitRequestFullscreen) { /* Chrome, Safari and Opera */
			elem.webkitRequestFullscreen();
		} else if (elem.msRequestFullscreen) { /* IE/Edge */
			elem.msRequestFullscreen();
		}
	}
	
	componentDidMount(){
		// Expose the three.js scenegraph to inspection tools
		global.scene = this.state.scene;
		
		global.addEventListener("mouseup", this.onMouseUp);
		this.setup(this.props);
	}
	
	componentWillUnmount(){
		var { scene } = this.state;
		this.animated = false;
		
		if(scene == global.scene){
			global.scene = null;
		}
		scene.dispose();
		global.removeEventListener("mouseup", this.onMouseUp);
		global.cancelAnimationFrame(this.animate);
		
	}
	
	onMouseUp(){
		this.setState({click: null});
	}
	
	onMouseMove(e){
		var { click } = this.state;
		if(!click){
			return;
		}
		
		var deltaX=e.clientX - click.x;
		var deltaY=e.clientY - click.y;
		click.x = e.clientX;
		click.y = e.clientY;
		
		if(!this.camera){
			return;
		}
		
		// sphere.rotation.y = startRotation * deg2rad;
		this.camera.rotation.y += deltaX * 0.001;
		this.camera.rotation.x += deltaY * 0.001;
	}
	
	onMouseDown(e){
		this.setState({
			click: {
				x: e.clientX,
				y: e.clientY
			}
		});
	}
	
	onFullscreen(){
		if (typeof DeviceMotionEvent.requestPermission === 'function') {
			DeviceMotionEvent.requestPermission()
			.then(permissionState => {
				if (permissionState === 'granted') {
					this.fs();
				}
			})

		}else{
			this.fs();
		}
	}
	
	onLoaded(){
		this.setState({loaded: true});
		this.props.onLoad && this.props.onLoad();
	}
	
	setup(props){
		// Expose the three.js scenegraph to inspection tools
		global.scene = this.state.scene;
		
		if(!this.hostEle || !this.root3DEle || !this.canvasEle){
			return;
		}
		
		var startRotation = props.startRotation || 45;
		var renderer = this.renderer;
		
		if(!renderer){
			renderer = this.renderer = new THREE.MultiRenderer(
				{
					domElement: this.hostEle,
					renderers: [THREE.WebGLRenderer, THREE.CSS3DRenderer],
					parameters: [{alpha: true, canvas: this.canvasEle}, this.root3DEle]
				}
			);
		}
		
		// var renderer = new THREE.WebGLRenderer({alpha: true});
		
		var hostEle = this.hostEle;
		var bounds = hostEle.getBoundingClientRect();
		var {size, scene} = this.state;
		size.w = bounds.width;
		size.h = bounds.height;
		
		renderer.setSize(size.w, size.h, false);
		renderer.renderers[0].setClearColor( 0x000000, 0 ); // the default
		// hostEle.appendChild(renderer.domElement);
		
		var camera = this.camera;
		
		if(!camera){
			camera = new THREE.PerspectiveCamera(70, size.w / size.h, 0.1, 100);
			camera.rotation.order = 'YXZ';
			this.camera = camera;
			scene.add(camera);
		}
		
		if(props.ar){
			this.doc = new DeviceOrientationControls(this.camera);
		}
		
		// +ve z is north
		// +ve x is west
		
		var imgUrl = getRef(props.imageRef, {url: true, size: 'original'});
		var material = this.material;
		
		if(material){
			if(material._url != imgUrl){
				material._url = imgUrl;
				material.map = new THREE.TextureLoader().load(imgUrl, this.onLoaded);
			}
		}else{
			var geometry = new THREE.SphereGeometry( 5, 32, 32 );
			this.material = material = new THREE.MeshBasicMaterial( {map: new THREE.TextureLoader().load(imgUrl, this.onLoaded), side: THREE.DoubleSide} );
			var sphere = new THREE.Mesh( geometry, material );
			material._url = imgUrl;
			this.sphere = sphere;
			var scaleFactor = 0.1;
			
			sphere.scale.x = -scaleFactor;
			sphere.scale.y = scaleFactor;
			sphere.scale.z = scaleFactor;
			scene.add( sphere );
		}
		
		// material.map.needsUpdate = true;
		if(!this.animated){
			this.animated = true;
			this.animate();
		}
	}
	
	animate() {
		if(!this.hostEle || !this.renderer || !this.animated){
			return;
		}
		
		if(global.scene && this.state.scene != global.scene){
			this.animated = false;
			return;
		}
		
		global.requestAnimationFrame( this.animate );
		var bounds = this.hostEle.getBoundingClientRect();
		var size = this.state.size;
		if(bounds.width != size.w || bounds.height != size.h){
			size.w = bounds.width;
			size.h = bounds.height;
			this.renderer.setSize(size.w, size.h, false);
		}
		
		this.doc && this.doc.update();
		this.renderer.render(this.state.scene, this.camera);
	}
	
	render(){
		const { size } = this.state;
		
		var width = size.w + 'px';
		var height= size.h + 'px';
		
		return (
		<SphereContext.Provider value={this.state.scene}>
			<div ref={this.setRef} style={this.props.style} className={"photosphere" + (this.state.loaded ? ' loaded' : '')}>
				<canvas ref={this.setCanvasRef} style={{position: 'absolute', top: '0px', left: '0px', width, height}} />
				<div ref={this.set3DRef} style={{overflow: 'hidden', position: 'absolute', top: '0px', left: '0px', width, height}} onMouseDown={this.onMouseDown} onMouseMove={this.onMouseMove}>
					<div style={{WebkitTransformStyle: 'preserve-3d', transformStyle: 'preserve-3d', pointerEvents: 'none', width, height}}>
						{this.props.children}
					</div>
				</div>
			</div>
			<div className="photosphereUI"></div>
		</SphereContext.Provider>);
	}

}

Photosphere.propTypes={
	imageRef: 'image'
};