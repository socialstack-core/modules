import ObjectTransformer from "../ObjectTransformer";

export default class RenderStrategy
{
    static clickEnabled = false;
    static isTransforming = false;
    static lastTransformedObj = null;
    static objTransform = new ObjectTransformer();

    static getObjTransformDetails() {
		return RenderStrategy.objTransform.outputDetails(RenderStrategy.getLastTransformedObj());
	}

    static getLastTransformedObj() {
        return RenderStrategy.lastTransformedObj;
    }

    static isClickEnabled() {
        return RenderStrategy.clickEnabled;
    }

    static isTransformControlsEnabled() {
        return RenderStrategy.objTransform.enabled;
    }

    static setClickEnabled(enabled) {
        RenderStrategy.clickEnabled = enabled;
    }

    static setTransformControlsEnabled(enabled) {
		RenderStrategy.objTransform.setEnabled(enabled);
	}

    static transform3DObject(vector2d, threeDObj, scaleOverrides = null) {
        RenderStrategy.objTransform.transform3DObject(vector2d, threeDObj, scaleOverrides);
    }

    constructor(threeDObject) {
        this.mouseDrag = false;
        this.threeDObject = threeDObject;
        this.obj = null;

        RenderStrategy.objTransform.addDetailsUpdateListener(() => {
			if (this.threeDObject.props.onTransformDetailsUpdate) {
				this.threeDObject.props.onTransformDetailsUpdate(this.constructor.getObjTransformDetails());
			}
		})

        this.processMouseDown = this.processMouseDown.bind(this);
        this.processMouseMove = this.processMouseMove.bind(this);
		this.processMouseUp = this.processMouseUp.bind(this);
    }

    getRenderObject() {
        return this.obj;
    }

    removeFromScene() {
        var scene = global.scene;
		scene && this.obj && scene.remove(this.obj);
		this.obj = null;
    }

    processMouseUp(e) {
        if (RenderStrategy.isClickEnabled() && this.mouseDrag) {
			if (this.threeDObject.props.onClick) {
				this.threeDObject.props.onClick();
			}
		}

        this.mouseDrag = false;
        RenderStrategy.isTransforming = false;
    }

    setup(props, ref) {
        console.warn('setup not implemented in RenderStrategy', this.constructor.name);
    }

    onSceneAdd() {
        // Optional
    }

    processMouseDown(e) {
        console.warn('processMouseMove not implemented in RenderStrategy', this.constructor.name);
    }

    processMouseMove(e) {
        console.warn('processMouseMove not implemented in RenderStrategy', this.constructor.name);
    }

    afterTransform(props) {
        // Optional
    }

    render(props){
		this.threeDObject.transform(props);

		var { children, className } = props;
		var El = props.element || "div";

		if(!children){
			return <El className = {className} ref={this.threeDObject.refChange} />;
		}

		return <El className = {className} ref={this.threeDObject.refChange}>{children}</El>;
	}
}
