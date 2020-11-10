var THREE = require('UI/Functions/ThreeJs/ThreeJs.js');
import getRef from 'UI/Functions/GetRef';
import omit from 'UI/Functions/Omit';
import DeviceOrientationControls from 'UI/Functions/DeviceOrientationControls';
import imageCache from 'UI/Photosphere/Cache';
var HlsVideo;
try{
	HlsVideo = require('UI/HlsVideo/HlsVideo.js').default;
}catch(e){
	// No video sphere support
}

import SphereContext from 'UI/Photosphere/SphereContext';

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
		this.setContainerRef = this.setContainerRef.bind(this);
		this.set3DRef = this.set3DRef.bind(this);
		this.setCanvasRef = this.setCanvasRef.bind(this);
		this.onMouseUp = this.onMouseUp.bind(this);
		this.onMouseDown = this.onMouseDown.bind(this);
		this.onMouseMove = this.onMouseMove.bind(this);
        this.onTouchStart = this.onTouchStart.bind(this);
        this.onTouchMove = this.onTouchMove.bind(this);
		this.animate = this.animate.bind(this);
		this.onLoaded = this.onLoaded.bind(this);
		this.onWheel = this.onWheel.bind(this);
		this.setPhotosphereUIRef = this.setPhotosphereUIRef.bind(this);

	}
	
	componentWillReceiveProps(props){
		this.setup(props);
	}
	
	setRef(ref){
		this.hostEle = ref;
		this.setup(this.props);
	}
	
	setContainerRef(ref){
		this.containerEle = ref;
		this.setup(this.props);
	}

	setPhotosphereUIRef(ref) {
		this.photosphereUIRef = ref;
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
        global.addEventListener("wheel", this.onWheel);
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
        global.removeEventListener("wheel", this.onWheel);
		global.cancelAnimationFrame(this.animate);
		
	}
	
	onMouseUp(){
		this.setState({click: null});
	}
    
    onWheel(props) {
		if(props.target != this.hostEle && props.target != this.containerEle && props.target != this.root3DEle && props.target != this.canvasEle) {
			return;
		}	
        var scaleFactor = 2;
        var maxFov = this.props.maxFov || 70;
        var minFov = this.props.minFov || 30;
        this.camera.fov = Math.max(Math.min(this.camera.fov + (Math.sign(props.deltaY) * scaleFactor), maxFov), minFov);
        this.camera.updateProjectionMatrix();
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
		
		// sphere.rotation.y = startRotation * deg2rad;
		this.rotateCamera(deltaX, deltaY, 0.001);
	}
	
	rotateCamera(deltaX, deltaY, scale){
		if(!this.camera){
			return;
		}
		
		this.camera.rotation.y += deltaX * scale;
		var x = this.camera.rotation.x;
		x += deltaY * scale;
		
		if(x < -1.4){
			x = -1.4;
		}else if(x>1.4){
			x = 1.4;
		}
		
		this.camera.rotation.x = x;
	}
	
	onMouseDown(e){
		this.setState({
			click: {
				x: e.clientX,
				y: e.clientY
			}
		});
	}
    
    onTouchStart(e){
        this.setState({
			touch: {
				x: e.touches[0].clientX,
				y: e.touches[0].clientY
			}
		});
    }
    
    onTouchMove(e){
		var { touch } = this.state;
		if(!touch){
			return;
		}
		var deltaX=e.touches[0].clientX - touch.x;
		var deltaY=e.touches[0].clientY - touch.y;        
		touch.x = e.touches[0].clientX;
		touch.y = e.touches[0].clientY;
		
		this.rotateCamera(deltaX, deltaY, 0.002);
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
	
	onLoaded(fullsize){
		this.setState({loaded: true});
		if(!fullsize){
			var {startRotation} = this.props;
            
            if(startRotation && startRotation.y !== undefined){
                    this.camera.rotation.y = startRotation.y;
                    this.camera.rotation.x = startRotation.x;
            }else{
                var startRotation = this.props.startRotation || 0;
                this.camera.rotation.y = startRotation;
                this.camera.rotation.x = 0;
            }
		}
		this.props.onLoad && this.props.onLoad(fullsize);
	}
	
	setup(props){
		// Expose the three.js scenegraph to inspection tools
		global.scene = this.state.scene;
		
		if(!this.hostEle || !this.containerEle || !this.root3DEle || !this.canvasEle){
			return;
		}
		
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
		var bounds = this.containerEle.getBoundingClientRect();
		var {size, scene} = this.state;
		size.w = bounds.width;
		size.h = bounds.height;
		
		renderer.setSize(size.w, size.h, true);
		renderer.renderers[0].setClearColor( 0x000000, 0 ); // the default
		// hostEle.appendChild(renderer.domElement);
		
		var camera = this.camera;
		
		if(!camera){
			camera = new THREE.PerspectiveCamera(70, size.w / size.h, 0.1, 100);
			camera.rotation.order = 'YXZ';
			this.camera = camera;
			scene.add(camera);
		}
		
		if(props.ar && !this.doc){
			this.doc = new DeviceOrientationControls(this.camera);
		}
		
		// +ve z is north
		// +ve x is west
		
		// Is it a video?
		var imgUrlLoad = getRef(props.imageRef, {url: true, size: '512'});
		var imgUrl = props.videoRef ? null : getRef(props.imageRef, {url: true, size: 'original'});
		var material = this.material;
		
		try{
			var vt = this.videoTex;
			this.videoTex = null;
			if(vt && !props.videoRef){
				vt.dispose();
			}
		}catch(e){
			// Failed tidying up video - this is ok; it was likely tidied up internally
		}
		
		if(material){
			if(material._url != imgUrlLoad){
				material._url = imgUrlLoad;
				var mapSmall = imageCache(imgUrlLoad, () => this.onLoaded(false));
				
				if(imgUrl){
					var mapFull = imageCache(imgUrl, () => {
						if(material._url == imgUrlLoad){
							material.map = mapFull;
						}
						this.onLoaded(true);
					});
					material.map = mapFull.image ? mapFull : mapSmall;
				}else{
					// It's a video - we show the low res image until the video loads:
					material.map = this.videoTex || mapSmall;
				}
			}
		}else{
			var mapSmall = imageCache(imgUrlLoad, () => this.onLoaded(false));
			var map;
			
			if(imgUrl){
				var mapFull = imageCache(imgUrl, () => {
					if(this.material && this.material._url == imgUrlLoad){
						this.material.map = mapFull;
					}
					this.onLoaded(true);
				});
				map = mapFull.image ? mapFull : mapSmall;
			}else{
				map = this.videoTex || mapSmall;
			}
			var geometry = new THREE.SphereGeometry( 5, 32, 32 );
			this.material = material = new THREE.MeshBasicMaterial( {map, side: THREE.DoubleSide} );
			var sphere = new THREE.Mesh( geometry, material );
			material._url = imgUrlLoad;
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
		var bounds = this.containerEle.getBoundingClientRect();
		var size = this.state.size;
		if(bounds.width != size.w || bounds.height != size.h){
			size.w = bounds.width;
			size.h = bounds.height;
			this.renderer.setSize(size.w, size.h, true);
			
			if(this.camera){
				this.camera.aspect = size.w / size.h;
				this.camera.updateProjectionMatrix();
			}
			
		}
		
		this.doc && this.doc.update();
		this.renderer.render(this.state.scene, this.camera);
	}
	
	render(){
		const { size } = this.state;
		
		var width = size.w + 'px';
		var height= size.h + 'px';
		
		return (
		<SphereContext.Provider value={{scene: this.state.scene, uiNode: this.photosphereUIRef }}>
			<div ref={this.setContainerRef} {...omit(this.props, ['ar', 'children', 'imageRef', 'videoRef', 'onLoad', 'startRotation', 'skipFade'])}>
				<div ref={this.setRef} style={{width: '100%', height: '100%', position: 'absolute'}} className={"photosphere" + (this.state.loaded ? ' loaded' : '') + (this.props.skipFade ? ' no-fade' : '')}>
					<canvas ref={this.setCanvasRef} style={{position: 'absolute', top: '0px', left: '0px', width, height}} />
					<div ref={this.set3DRef} style={{overflow: 'hidden', position: 'absolute', top: '0px', left: '0px', width, height}} onMouseDown={this.onMouseDown} onMouseMove={this.onMouseMove} onTouchStart={this.onTouchStart} onTouchMove={this.onTouchMove}>
						<div style={{WebkitTransformStyle: 'preserve-3d', transformStyle: 'preserve-3d', pointerEvents: 'none', width, height}}>
							{this.props.children}
						</div>
					</div>
					{this.props.videoRef && <HlsVideo style={{display: 'none'}} videoRef={this.props.videoRef} autoplay loop={this.props.loop} onEnded={this.props.onVideoEnded} onProgress={this.props.onVideoProgress} onVideo={videoEl => {
						
						if(!videoEl){
							return;
						}
						
						// Create video texture:
						var texture = new THREE.VideoTexture(videoEl);
						texture.minFilter = THREE.LinearFilter;
						texture.magFilter = THREE.LinearFilter;
						texture.format = THREE.RGBFormat;
						this.videoTex = texture;
						
						if(this.material){
							this.material.map = texture;
						}
						
					}}/>}
				</div>
				<div ref = {this.setPhotosphereUIRef} />
			</div>
		</SphereContext.Provider>);
	}

}

Photosphere.propTypes={
	imageRef: 'image'
};