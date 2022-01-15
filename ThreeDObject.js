import deviceInput from './DeviceInput';
import RenderStrategy from './RenderStrategies/Css3D';
import Css3D from './RenderStrategies/Css3D';
import CurvedModel from './RenderStrategies/CurvedModel';


export default class ThreeDObject extends React.Component {
	static getObjTransformDetails() {
		return RenderStrategy.getObjTransformDetails();
	}

	static isTransformControlsEnabled() {
		return RenderStrategy.isTransformControlsEnabled();
	}

	static setTransformControlsEnabled(enabled) {
		RenderStrategy.setTransformControlsEnabled(enabled);
	}

	constructor(props){
		super(props);
		this.state = {
			renderStratChanged: false,
		};

		this.renderMode = props.renderMode ?? 'auto';

		if ((this.renderMode === 'auto' && props.curve) || this.renderMode === 'CurvedModel') {
        	this.renderStrat = new CurvedModel(this);
		} else if ((this.renderMode === 'auto' && !props.curve) || this.renderMode === 'Css3D') {
			this.renderStrat = new Css3D(this);
		}

		this.refChange = this.refChange.bind(this);
        this.processMouseDown = this.processMouseDown.bind(this);
		this.processMouseUp = this.processMouseUp.bind(this);
		this.processMouseMove = this.processMouseMove.bind(this);

		deviceInput.addMouseDownListener(null, this.processMouseDown);
		deviceInput.addMouseUpListener(null, this.processMouseUp);
		deviceInput.addMouseMoveListener(null, this.processMouseMove);
	}

	componentWillUnmount(){
		this.renderStrat.removeFromScene();
	}

	componentDidMount(){
		this.renderStrat.onSceneAdd();
		this.setup(this.props);
	}

	componentDidUpdate() {
		if (this.state.renderStratChanged) {
			this.setState({renderStratChanged: false});
			this.renderStrat.onSceneAdd();
			this.setup(this.props);
		}
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
        this.renderStrat.setup(this.props, this.ref);
		this.transform(props);
	}

    processMouseUp(e) {
        this.renderStrat.processMouseUp(e);
    }

    processMouseDown(e) {
        this.renderStrat.processMouseDown(e);
    }

    processMouseMove(e) {
        this.renderStrat.processMouseMove(e);
    }

	transform(props){
		var obj = this.renderStrat.getRenderObject();
		var {position,
			rotation,
			scale,
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
			radius,
			angle,
			height,
		} = props;

		if (!obj) {
			return;
		}


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
				if(rotation.w !== undefined){
					obj.quaternion.x = rotation.x || 0;
					obj.quaternion.y = rotation.y || 0;
					obj.quaternion.z = rotation.z || 0;
					obj.quaternion.w = rotation.w || 0;
				}else{
					obj.rotation.x = rotation.x || 0;
					obj.rotation.y = rotation.y || 0;
					obj.rotation.z = rotation.z || 0;
				}
            }
        }

        this.renderStrat.afterTransform(props);

		if (this.state.curve !== this.props.curve) {
			this.setState({curve: this.props.curve});
		}
	}

	render(){
		return this.renderStrat.render(this.props);
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
	curve: 'float',
	numberOfSegments: 'int',
	children: true
}
