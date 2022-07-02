import * as THREE from 'UI/Functions/ThreeJs';
import getRef from 'UI/Functions/GetRef';

export default class ThreeDCurvedImage extends React.Component {
	
	constructor(props){
		super(props);
		this.planeCurve = this.planeCurve.bind(this);
	}
	
	componentWillUnmount(){
		var scene = global.scene;
		scene && this.obj && scene.remove(this.obj);
		this.obj = null;
	}
	
	componentDidMount(){
		var video = document.getElementById("video");
		if (video) {
			video.play();
		}		

		this.setup(this.props);
	}

	planeCurve(geometry, z){
		if (!geometry instanceof THREE.PlaneGeometry) {
			console.log('first argument of planeCurve() MUST be an instance of PlaneGeometry');
			return;
		}

		let p = geometry.parameters;
		let hw = p.width * 0.5;
		
		let a = new THREE.Vector2(-hw, 0);
		let b = new THREE.Vector2(0, z);
		let c = new THREE.Vector2(hw, 0);
		
		let ab = new THREE.Vector2().subVectors(a, b);
		let bc = new THREE.Vector2().subVectors(b, c);
		let ac = new THREE.Vector2().subVectors(a, c);
		
		let r = (ab.length() * bc.length() * ac.length()) / (2 * Math.abs(ab.cross(ac)));
		
		let center = new THREE.Vector2(0, z - r);
		let baseV = new THREE.Vector2().subVectors(a, center);
		let baseAngle = baseV.angle() - (Math.PI * 0.5);
		let arc = baseAngle * 2;

		let uv = geometry.attributes.uv;
		let pos = geometry.attributes.position;
		let mainV = new THREE.Vector2();

		for (let i = 0; i < uv.count; i++) {
			let uvRatio = 1 - uv.getX(i);
			let y = pos.getY(i);
			mainV.copy(c).rotateAround(center, (arc * uvRatio));
			pos.setXYZ(i, mainV.x, y, -mainV.y);
		}
		
		pos.needsUpdate = true;
    }
	
	setup(props){
		var scene = global.scene;

		if (global.scene) {
			for (let obj of global.scene.children) {
				if (obj.type === "PerspectiveCamera") {
					this.camera = obj;
				}
			}
		}

		if(!scene || this.obj){
			return;
		}

		var imageRef = props.imageRef;
		var videoRef = props.videoRef;

		// create curved plane
		var curve = props.curve ? props.curve : 0.1;
		var scale = props.scale
		var numberOfSegments = props.numberOfSegments ? props.numberOfSegments : 64;

		var scaleX = scale?.x ? scale?.x : 1;
		var scaleY = scale?.y ? scale?.y : 1;
		curve = curve * scaleX;

		var width = scaleX * 16/10;
		var height = scaleY * 9/10;

		const geometry = new THREE.PlaneBufferGeometry(width, height, numberOfSegments, 1);

		this.planeCurve(geometry, curve);

		// load media to be displayed on the curved plane
		var material = new THREE.MeshBasicMaterial( { color: 0x00ff00 } );

		var loader = new THREE.TextureLoader();

		if (videoRef) {
			const video = document.getElementById( 'video' );
			const texture = new THREE.VideoTexture( video );
			material = new THREE.MeshBasicMaterial( { map: texture } );
		}
		else if (imageRef) {
			var texture = loader.load(
				getRef(imageRef, {url:true}),
				undefined,
				undefined,
				function (err) {
					console.error("error loading texture for curved image");
					console.error(err);
				}
			);

			material = new THREE.MeshBasicMaterial( { map: texture } );
		}

		material.side = THREE.DoubleSide;

		// create the mesh by combining the geometry and material
		const mesh = new THREE.Mesh( geometry, material );

		this.obj = mesh;
		scene.add(mesh);

		this.transform(props);
	}
	
	transform(props){
		var obj = this.obj;
		var {position,
			rotation,
			circularCoords,
			positionX,
			positionY,
			positionZ,
			rotationX,
			rotationY,
			rotationZ,
			radius,
			angle,
			height,
		} = props;


		if(!position && (positionX || positionY || positionZ)) {
			position = {};
			position.x = positionX;
			position.y = positionY;
			position.z = positionZ;
		}

		if(!rotation && (rotationX || rotationY || rotationZ)) {
			rotation = {};
			rotation.x = rotationX;
			rotation.y = rotationY;
			rotation.z = rotationZ;
		}

		if(!circularCoords && (radius || angle || height)) {
			circularCoords = {};
			circularCoords.radius = radius;
			circularCoords.angle = angle;
			circularCoords.height = height;
		}

        if (circularCoords) {
            var degToRad = Math.PI / 180;
            obj.position.x = circularCoords.radius * Math.sin(circularCoords.angle * degToRad);
            obj.position.z = -circularCoords.radius * Math.cos(circularCoords.angle * degToRad);
            obj.position.y = circularCoords.height;
            obj.rotation.y = -circularCoords.angle * degToRad;
            obj.rotation.x = rotationX || 0;
            obj.rotation.z = rotationY || 0;
        } else {
            if(position){
				obj.position.set(position.x, position.y, position.z);
            }

            if(rotation){
                obj.rotation.x = rotation.x || 0;
                obj.rotation.y = rotation.y || 0;
                obj.rotation.z = rotation.z || 0;
            }
        }
	}
	
	render(){
		var { children, videoRef } = this.props;
		
		if(this.obj){
			this.transform(this.props);
		}
		
		var El = this.props.element || "div";
		
		return <El className = "curvedImage" ref={this.refChange}>
			{children && children}
			{videoRef &&
				<video id="video" src={getRef(videoRef, {url:true})} ref="vidRef" muted autoplay loop></video>
			}
		</El>;
	}
	
}

ThreeDCurvedImage.propTypes = {
	positionX: 'int',
	positionY: 'int',
	positionZ: 'int',
	rotationX: 'float',
	rotationY: 'float',
	rotationZ: 'float',
	scaleX: 'float',
	scaleY: 'float',
	scaleZ: 'float',
	radius: 'int',
	angle: 'float',
	height: 'int',
	curve: "float",
	imageRef: 'image',
	numberOfSegments: 'int',
	children: true
}
