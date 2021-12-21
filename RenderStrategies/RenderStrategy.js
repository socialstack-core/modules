import ObjectTransformer from "../ObjectTransformer";

export default class RenderStrategy
{
    static isTransforming = false;
    static lastTransformedObj = null;
    static objTransform = new ObjectTransformer();

    static getObjTransformDetails() {
		return RenderStrategy.objTransform.outputDetails(RenderStrategy.getLastTransformedObj());
	}

    static getLastTransformedObj() {
        return RenderStrategy.lastTransformedObj;
    }

    static isTransformControlsEnabled() {
        return RenderStrategy.objTransform.enabled;
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

		/* if (this.renderMode === 'auto') {
			var hasCurve = (this.props.curve && this.props.curve != 0);
			var isCurvedModel = (this.renderStrat instanceof CurvedModel);
			var isCss3D = (this.renderStrat instanceof Css3D);

			if ((hasCurve && !isCurvedModel) || (!hasCurve && !isCss3D)) {
				var targetRenderStrat = (isCurvedModel) ? Css3D : CurvedModel;

				var prevRenderStrat = this.renderStrat.constructor.name;
				console.log('changing to', targetRenderStrat.name, '( from', prevRenderStrat, ')')
				this.renderStrat.removeFromScene();
				this.renderStrat = null;
				this.renderStrat = new targetRenderStrat(this);

				// Doesn't work
				if (!this.state.renderStratChanged) {
					this.setState({renderStratChanged: true});
					var newRef = React.createRef();
					this.refChange(newRef);
				}
			}
		} */

		var { children, className } = props;
		var El = props.element || "div";

		if(!children){
			return <El className = {className} ref={this.threeDObject.refChange} />;
		}

		return <El className = {className} ref={this.threeDObject.refChange}>{children}</El>;
	}
}