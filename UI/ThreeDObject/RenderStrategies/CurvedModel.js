import * as THREE from 'UI/Functions/ThreeJs';
import getRef from 'UI/Functions/GetRef';
import RenderStrategy from "./RenderStrategy";

const baseSize = 5;
const defaultCurve = 0.1;
const defaultScale = {x: 1.0, y: 1.0, z: 1.0};
const defaultNumberOfSegments = 64;

export default class CurvedModel extends RenderStrategy
{
    constructor(threeDObject) {
        super(threeDObject);

		this.widthRatio = 1.0;
		this.heightRatio = 1.0;
        this.mousePosNormal = {x: 0, y: 0};
		this.transformScaleOverrides = {translateScale: 0.02, scaleScale: 0.01};
		this.videoElementRef = React.createRef();
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

		var curve = props.curve ?? defaultCurve;
		var scale = props.scale ?? defaultScale;
		var numberOfSegments = props.numberOfSegments ?? defaultNumberOfSegments;

		var scaleX = scale?.x ?? 1.0;
		curve = curve * scaleX;

		var width  = baseSize * 16/10;
		var height = baseSize * 9/10;

		const geometry = new THREE.PlaneBufferGeometry(width, height, numberOfSegments, 1);
		this.geometry = geometry;

		this.planeCurve(geometry, curve);

		// load media to be displayed on the curved plane
		var material = new THREE.MeshBasicMaterial( { color: 0x00ff00 } );

		var loader = new THREE.TextureLoader();

		if (this.videoElementRef.current) {
			const texture = new THREE.VideoTexture( this.videoElementRef.current );
			material = new THREE.MeshBasicMaterial( { map: texture } );
		}
		else if (imageRef) {
			var texture = loader.load(
				getRef(imageRef, {url:true}),
				(loaded) => {
					var image = loaded.image;

					if (image) {
						this.widthRatio  = image.naturalWidth  / image.naturalHeight;
						this.heightRatio = image.naturalHeight / image.naturalWidth;

						if (this.widthRatio > this.heightRatio) {
							mesh.scale.set(scale.x * this.widthRatio, scale.y, scale.z);
						} else {
							mesh.scale.set(scale.x * this.widthRatio, scale.y * this.heightRatio, scale.z);
						}
						this.planeCurve(geometry, curve);
					}

					props.onLoad && props.onLoad();
				},
				undefined,
				function (err) {
					console.error("error loading texture for curved image");
					console.error(err);
				}
			);

			material = new THREE.MeshBasicMaterial( { map: texture } );
		}

		if (!props.hideBackface) {
			material.side = THREE.DoubleSide;
		}

		// create the mesh by combining the geometry and material
		const mesh = new THREE.Mesh( geometry, material );

		this.obj = mesh;
		scene.add(mesh);
	}

    processMouseDown(e) {
		// Only raycast if object clicking or transforming is enabled
		if (RenderStrategy.isClickEnabled() || RenderStrategy.isTransformControlsEnabled()) {
			this.mouseDrag = this.raycastModel(this.mousePosNormal, this.obj);
			this.mouseTarget = e.target;
		} else {
			this.mouseDrag = false;
		}

		if (this.mouseDrag && RenderStrategy.isTransformControlsEnabled() && !RenderStrategy.isTransforming) {
			RenderStrategy.isTransforming = true;
			RenderStrategy.lastTransformedObj = this.threeDObject;

			if (this.threeDObject.props.onTransform) {
				this.threeDObject.props.onTransform();
			}
		}
	}

    processMouseMove(e) {
		var mouseMovement = {x: -e.movementX, y: -e.movementY}; // Invert for model rotation
		var mousePosition = {x: e.clientX, y: e.clientY};

		var parentRect = this.parent?.getBoundingClientRect();
		var parentTop    = parentRect?.top    ?? 0;
		var parentLeft   = parentRect?.left   ?? 0;
		var parentWidth  = parentRect?.width  ?? 0;
		var parentHeight = parentRect?.height ?? 0;

		this.mousePosNormal.x =  (((mousePosition.x - parentLeft) / parentWidth) * 2) - 1;
		this.mousePosNormal.y = -(((mousePosition.y - parentTop) / parentHeight) * 2) + 1;

        if (this.mouseDrag && RenderStrategy.isTransformControlsEnabled()) {
			RenderStrategy.transform3DObject({x: e.movementX, y: -e.movementY}, this.threeDObject, this.transformScaleOverrides);
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

		var curve = (props.curve ? props.curve : 0.1) * scale.x;

		if (this.obj) {
			if (this.widthRatio > this.heightRatio) {
				this.obj.scale.set(scale.x * this.widthRatio, scale.y, scale.z);
			} else {
				this.obj.scale.set(scale.x * this.widthRatio, scale.y * this.heightRatio, scale.z);
			}
		}

		//this.geometry.parameters.widthSegments = Math.floor(props.numberOfSegments);
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

		if (this.threeDObject.ref && !this.parent) {
			this.parent = this.threeDObject.ref.parentNode;
		}

		var El = props.element || "div";

		return <El className = "curvedModel" ref={this.threeDObject.refChange}>
			{videoRef &&
				<video ref={this.videoElementRef} onloadeddata={() => { props.onLoad && props.onLoad(); }} src={getRef(videoRef, {url:true})} muted autoplay loop></video>
			}
		</El>;
    }
}
