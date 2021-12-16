import * as THREE from 'UI/Functions/ThreeJs';
import getRef from 'UI/Functions/GetRef';
import RenderStrategy from "./RenderStrategy";

export default class CurvedModel extends RenderStrategy
{
    constructor(threeDObject) {
        super(threeDObject);
        this.mousePosNormal = {x: 0, y: 0};
    }

    setup(props, ref){
		var scene = global.scene;

		if (global.scene) {
			for (let obj of global.scene.children) {
				if (obj.type === "PerspectiveCamera") {
					this.camera = obj;
				}
			}
		}

		if (!this.camera) {
			console.error('Camera not found in ', this.constructor.name);
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
		this.geometry = geometry;

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
/* 		mesh.scale.x = (scaleX * 16/10) * 0.1;
		mesh.scale.y = (scaleY * 9/10) * 0.1; */

		this.obj = mesh;
		scene.add(mesh);

		//this.transform(props);
		//this.obj.updateMatrixWorld();
	}

	onSceneAdd() {
        var video = document.getElementById("video");
		if (video) {
			video.play();
		}
    }

    processMouseDown(e) {
		this.mouseDrag = this.raycastModel(this.mousePosNormal, this.obj);

		if (this.mouseDrag) {
			RenderStrategy.lastTransformedObj = this.threeDObject;

			if (this.threeDObject.props.onTransform) {
				this.threeDObject.props.onTransform();
			}
		}
	}

	processMouseUp(e) {
        this.mouseDrag = false;
		this.lastTransformedObj = null;
    }

    processMouseMove(e) {
		var mouseMovement = {x: -e.movementX, y: -e.movementY}; // Invert for model rotation
		var mousePosition = {x: e.clientX, y: e.clientY};

		this.mousePosNormal.x =  (mousePosition.x   / window.innerWidth)  * 2 - 1;
		this.mousePosNormal.y = -((mousePosition.y + window.pageYOffset) / window.innerHeight) * 2 + 2;

        if (this.mouseDrag && RenderStrategy.isTransformControlsEnabled()) {
			RenderStrategy.transform3DObject({x: e.movementX, y: -e.movementY}, this.threeDObject);
        }
    }

	afterTransform(props) {
		var {
			scale,
			scaleX,
			scaleY,
			scaleZ,
		} = props;

		if(!scale && (scaleX || scaleY || scaleZ)) {
			scale = {};
			scale.x = scaleX;
			scale.y = scaleY;
			scale.z = scaleZ;
		}

		var width  = scale.x * 16/10;
		var height = scale.y * 9/10;
		var curve = props.curve ? props.curve : 0.1;
		//curve *= scale.x;

		this.geometry.parameters.width = width;
		this.geometry.parameters.height = height;
		this.geometry.parameters.widthSegments = Math.floor(props.numberOfSegments);
		this.planeCurve(this.geometry, curve);
	}

	raycastModel(mousePosNormal, model) {
		if (this.camera && model) {
			var mouse = new THREE.Vector2();
			mouse.x = mousePosNormal.x;
			mouse.y = mousePosNormal.y;

			var raycaster = new THREE.Raycaster();
			raycaster.setFromCamera(mouse, this.camera);

			if (model.children.length > 0) {
				var intersects = raycaster.intersectObjects(model.children);
			} else {
				var intersects = raycaster.intersectObjects([model]);
			}

			if (intersects.length > 0) {
				return true;
			}
		}
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

	render(props) {
        var { children, videoRef } = props;

		if(this.obj){
			this.threeDObject.transform(props);
		}

		var El = props.element || "div";

		return <El className = "curvedImage" ref={this.threeDObject.refChange}>
			{children && children}
			{videoRef &&
				<video id="video" src={getRef(videoRef, {url:true})} ref="vidRef" muted autoplay loop></video>
			}
		</El>;
    }
}
