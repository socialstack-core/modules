import * as THREE from 'UI/Functions/ThreeJs';
import ObjectTransformer from './ObjectTransformer';
import deviceInput from './DeviceInput';


export default class ThreeDObject extends React.Component {
	static objTransform = new ObjectTransformer();
	static lastTransformedObj = null;
	
	static getObjTransformDetails() {
		return this.objTransform.outputDetails(this.lastTransformedObj);
	}

	static setTransformControlsEnabled(enabled) {
		this.objTransform.setEnabled(enabled);
	}

	constructor(props){
		super(props);
		this.state = {
			editMode: false,
		};

		this.objTransform = this.constructor.objTransform;

		this.objTransform.addDetailsUpdateListener(() => {
			if (this.props.onTransformDetailsUpdate) {
				this.props.onTransformDetailsUpdate(this.constructor.getObjTransformDetails());
			}
		})

		this.refChange = this.refChange.bind(this);
		this.processMouseMove = this.processMouseMove.bind(this);
		this.processMouseUp = this.processMouseUp.bind(this);

		deviceInput.addMouseMoveListener(null, this.processMouseMove);
		deviceInput.addMouseUpListener(null, this.processMouseUp);
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

	processMouseMove(e) {
        if (this.mouseDrag && this.props.enableMouseDrag) {
			this.objTransform.transform3DObject({x: e.movementX, y: -e.movementY}, this);
        }
    }

	processMouseUp(e) {
        this.mouseDrag = false;
		this.lastTransformedObj = null;
    }

	decorateElement(ele) {
		if (ele && !ele.decorated) {
			ele.decorated = true;

			// Prevent element selection, which can occur when
			// panning a camera with the mouse
			ele.classList.add('unselectable');

			ele.onmousedown = (e) => {
				// Disable element dragging
				e.preventDefault();

				if (this.props.enableMouseDrag) {
					this.mouseDrag = true;
					ThreeDObject.lastTransformedObj = this;

					if (this.props.onTransform) {
						this.props.onTransform();
					}
				}
			}
		}
	}

	
	setup(props){
		var ref = this.ref;
		var scene = global.scene;
		if(!ref || !scene){
			return;
		}
		
		if(!scene._css){
			scene._css = {nodes: []};
		}
		
		if(this.obj){
			if(this.obj.element == ref){
				this.transform(props);
				return;
			}
			
			scene._css.nodes = scene._css.nodes.filter(a => a!=this.obj);
		}
		
		this.obj = new THREE.CSS3DObject(ref);
		scene._css.nodes.push(this.obj);
		this.transform(props);
		this.decorateElement(ref);
	}
	
	transform(props){
		var obj = this.obj;
		var {position,
			rotation,
			scale,
			crop,
			circularCoords,
			positionX,
			positionY,
			positionZ,
			rotationX,
			rotationY,
			rotationZ,
			scaleX,
			scaleY,
			scaleZ,
			cropT,
			cropR,
			cropB,
			cropL,
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

		if(!scale && (scaleX || scaleY || scaleZ)) {
			scale = {};
			scale.x = scaleX;
			scale.y = scaleY;
			scale.z = scaleZ;
		}

		if(!crop && (cropT || cropR || cropB || cropL)) {
			crop = {};
			crop.t = cropT;
			crop.r = cropR;
			crop.b = cropB;
			crop.l = cropL;
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
            obj.rotation.z = rotationZ || 0;
        } else {
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
        }

		if(scale){
			obj.scale.x = scale.x || 1;
			obj.scale.y = scale.y || 1;
			obj.scale.z = scale.z || 1;
		}

		if (crop) {
			var {t, r, b, l} = crop;
			t = t || 0;
			r = r || 0;
			b = b || 0;
			l = l || 0;
			obj.element.style.clipPath = `inset(${t*100}% ${r*100}% ${b*100}% ${l*100}%)`;
		}

		obj.update();
	}
	
	render(){
		var { children, className } = this.props;
		
		if(this.obj){
			this.transform(this.props);
		}
		
		var El = this.props.element || "div";
		
		if(!children){
			return <El className = {className} ref={this.refChange} />;
		}
		
		return <El className = {className} ref={this.refChange}>{children}</El>;
	}
	
}

ThreeDObject.propTypes = {
	className: 'string',
	positionX: 'int',
	positionY: 'int',
	positionZ: 'int',
	rotationX: 'float',
	rotationY: 'float',
	rotationZ: 'float',
	scaleX: 'float',
	scaleY: 'float',
	scaleZ: 'float',
	cropT: 'float',
	cropR: 'float',
	cropB: 'float',
	cropL: 'float',
	radius: 'int',
	angle: 'float',
	height: 'int',
	children: true
}
