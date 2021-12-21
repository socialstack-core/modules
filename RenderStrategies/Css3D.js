import * as THREE from 'UI/Functions/ThreeJs';
import RenderStrategy from "./RenderStrategy";

export default class Css3D extends RenderStrategy
{
    constructor(threeDObject) {
        super(threeDObject);
        this.ref = null;
    }

    setup(props, ref) {
        //var ref = this.ref;
		var scene = global.scene;
		if(/* !ref || */ !scene){
			return;
		}
		
		if(!scene._css){
			scene._css = {nodes: []};
		}
		
		/* if(this.obj){
			if(this.obj.element == ref){
				this.transform(props);
				return;
			}
			
			scene._css.nodes = scene._css.nodes.filter(a => a!=this.obj);
		} */
		
		this.obj = new THREE.CSS3DObject(ref);
		scene._css.nodes.push(this.obj);
		this.decorateElement(ref);
    }

	processMouseDown(e) {
        // Not needed - Mouse down event is added to element in decorateElement
    }

    processMouseMove(e) {
        if (this.mouseDrag && RenderStrategy.isTransformControlsEnabled()) {
			RenderStrategy.transform3DObject({x: e.movementX, y: -e.movementY}, this.threeDObject);
        }
    }

    afterTransform(props) {
        var {scale,
			scaleX,
			scaleY,
			scaleZ,
			crop,
            cropT,
			cropR,
			cropB,
			cropL,
        } = props;

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

		if(scale){
			this.obj.scale.x = scale.x || 1;
			this.obj.scale.y = scale.y || 1;
			this.obj.scale.z = scale.z || 1;
		}

        if (crop) {
			var {t, r, b, l} = crop;
			t = t || 0;
			r = r || 0;
			b = b || 0;
			l = l || 0;
			this.obj.element.style.clipPath = `inset(${t*100}% ${r*100}% ${b*100}% ${l*100}%)`;
		}

        this.obj.update();
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

				if (RenderStrategy.isTransformControlsEnabled() && !RenderStrategy.isTransforming) {
					this.mouseDrag = true;
					RenderStrategy.isTransforming = true;
					RenderStrategy.lastTransformedObj = this.threeDObject;

					if (this.threeDObject.props.onTransform) {
						this.threeDObject.props.onTransform();
					}
				}
			}
		}
	}
}