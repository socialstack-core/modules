import deviceInput from './DeviceInput';

const planeModKey    = 'ShiftLeft';
const propModeKey    = 'ControlLeft';
const reduceScaleKey = 'IntlBackslash';

const translateScale = 1.0;
const rotateScale = 0.004;
const scaleScale = 0.001;
const cropScale = 0.005;

/**
 * Provides transform controls as a helper for
 * ThreeDObject.
 *
 * A ThreeDObject can be transformed by calling
 * transform3DObject, which is intended to be called
 * in a mousemove event with the vector2d in the format:
 *
 * {x: e.movementX, y: -e.movementY}
 * 
 * The ObjectTransformer processes key inputs to change
 * transform options, but only when it is enabled (disabled
 * by default, use isEnabled and setEnabled functions to
 * check/change the value).
 *
 * When enabled, the left shift and control keys will alter
 * the current plane mode and prop mode respectively, while
 * holding the backslash key will reduce the global scale, 
 * allowing for more fine-tuning.
 */
export default class ObjectTransformer {
    constructor() {
        this.enabled = false;
        this.planeModActive = false;
        this.propMode = 'translate';
        this.flipTranslateAxes = {x: false, y: false, z: false};
		this.rotateAxisMode = 'x';
		this.scaleAxisMode = 'xy';
        this.cropAxisMode = 't';
        this.globalScale = 1.0;
        this.detailsUpdateSubscribers = [];

        this.processKeyDown = this.processKeyDown.bind(this);
        this.processKeyUp = this.processKeyUp.bind(this);
        this.onDetailsUpdate = this.onDetailsUpdate.bind(this);

        deviceInput.addKeyDownListener(null, this.processKeyDown);
        deviceInput.addKeyUpListener(null, this.processKeyUp);
    }

    processKeyDown(e) {
        if (this.enabled) {
            if (e.code === planeModKey) {
                this.setPlaneMod(true);
                this.cycleAxisMode();
            } else if (e.code === propModeKey) {
                this.cyclePropMode();
            } else if (e.code === reduceScaleKey) {
                this.setGlobalScale(0.1);
            }

            this.onDetailsUpdate();
        }
    }

    processKeyUp(e) {
        if (this.enabled) {
            if (e.code === planeModKey) {
                this.setPlaneMod(false);
            } else if (e.code === reduceScaleKey) {
                this.resetGlobalScale();
            }

            this.onDetailsUpdate();
        }
    }

    transform3DObject(vector2d, threeDObj) {
        var flipX = threeDObj.props.position && threeDObj.props.position.z > 0;
        var flipZ = threeDObj.props.position && threeDObj.props.position.x < 0;
        this.setTranslateFlipX(flipX);
        this.setTranslateFlipZ(flipZ);

        this.applyTransform(threeDObj, vector2d);
        threeDObj.transform(threeDObj.props);
        this.onDetailsUpdate();
	}

    formatVec(v, decimalPlaces) {
        var newV = {};
        newV.x = v.x ? Number.parseFloat(v.x).toFixed(decimalPlaces) : 0.0;
        newV.y = v.y ? Number.parseFloat(v.y).toFixed(decimalPlaces) : 0.0;
        newV.z = v.z ? Number.parseFloat(v.z).toFixed(decimalPlaces) : 0.0;

        return newV;
    }

    formatRect(r, decimalPlaces) {
        var newR = {};
        newR.t = r.t ? Number.parseFloat(r.t).toFixed(decimalPlaces) : 0.0;
        newR.r = r.r ? Number.parseFloat(r.r).toFixed(decimalPlaces) : 0.0;
        newR.b = r.b ? Number.parseFloat(r.b).toFixed(decimalPlaces) : 0.0;
        newR.l = r.l ? Number.parseFloat(r.l).toFixed(decimalPlaces) : 0.0;

        return newR;
    }

    cyclePropMode() {
        this.propMode = this.nextPropMode(this.propMode);
        this.rotateAxisMode = 'x';
        this.scaleAxisMode  = 'xy';
        this.cropAxisMode   = 't';
    }

    cycleAxisMode() {
        if (this.propMode === 'rotate') {
            this.rotateAxisMode = this.nextRotateAxis(this.rotateAxisMode);
        } else if (this.propMode === 'scale') {
            this.scaleAxisMode = this.nextScaleAxis(this.scaleAxisMode);
        } else if (this.propMode === 'crop') {
            this.cropAxisMode = this.nextCropAxis(this.cropAxisMode);
        }
    }

    isEnabled() {
        return this.enabled;
    }

    setEnabled(enabled) {
        if (enabled && typeof enabled !== "boolean") {
            console.error('Cannot set enabled; value is not a bool:', enabled);
        }

        this.enabled = enabled;
    }

    setPlaneMod(isActive) {
        this.planeModActive = isActive;
    }

    setTranslateFlipX(isFlipped) {
        this.flipTranslateAxes.x = isFlipped;
    }

    setTranslateFlipY(isFlipped) {
        this.flipTranslateAxes.y = isFlipped;
    }

    setTranslateFlipZ(isFlipped) {
        this.flipTranslateAxes.z = isFlipped;
    }

    setGlobalScale(value) {
        this.globalScale = value;
    }

    resetGlobalScale() {
        this.globalScale = 1.0;
    }

    applyTransform(comp, vector2d) {
        if (this.propMode === 'translate') {
            if (!comp.props.position) {
                comp.props.position = {x: 0, y: 0, z: 0};
            }

            var flipModX = this.flipTranslateAxes.x ? -1 : 1;
            var flipModY = this.flipTranslateAxes.y ? -1 : 1;
            var flipModZ = this.flipTranslateAxes.z ? -1 : 1;
            if (!this.planeModActive) {
                comp.props.position.x += (vector2d.x * translateScale * this.globalScale * flipModX);
            } else {
                comp.props.position.z += (vector2d.x * translateScale * this.globalScale * flipModZ);
            }
            comp.props.position.y += (vector2d.y * translateScale * this.globalScale * flipModY);
        } else if (this.propMode === 'rotate') {
            if (!comp.props.rotation) {
                comp.props.rotation = {x: 0.0, y: 0.0, z: 0.0};
            }

            if (this.rotateAxisMode === 'x') {
                comp.props.rotation.x += (vector2d.x * rotateScale * this.globalScale);
            } else if (this.rotateAxisMode === 'y') {
                comp.props.rotation.y += (vector2d.x * rotateScale * this.globalScale);
            } else if (this.rotateAxisMode === 'z') {
                comp.props.rotation.z += (vector2d.x * rotateScale * this.globalScale);
            }
        } else if (this.propMode === 'scale') {
            if (!comp.props.scale) {
                comp.props.scale = {x: 1.0, y: 1.0, z: 1.0};
            }

            if (this.scaleAxisMode === 'xy') {
                comp.props.scale.x += (vector2d.x * scaleScale * this.globalScale);
                comp.props.scale.y += (vector2d.x * scaleScale * this.globalScale);
            } else if (this.scaleAxisMode === 'x') {
                comp.props.scale.x += (vector2d.x * scaleScale * this.globalScale);
            } else if (this.scaleAxisMode === 'y') {
                comp.props.scale.y += (vector2d.x * scaleScale * this.globalScale);
            }
        } else if (this.propMode === 'crop') {
            if (!comp.props.crop) {
                comp.props.crop = {t: 0.0, r: 0.0, b: 0.0, l: 0.0};
            }

            if (this.cropAxisMode === 't') {
                comp.props.crop.t += (vector2d.x * cropScale * this.globalScale);
                comp.props.crop.t = this.clamp(comp.props.crop.t, 0.0, 1.0);
            } else if (this.cropAxisMode === 'r') {
                comp.props.crop.r += (vector2d.x * cropScale * this.globalScale);
                comp.props.crop.r = this.clamp(comp.props.crop.r, 0.0, 1.0);
            } else if (this.cropAxisMode === 'b') {
                comp.props.crop.b += (vector2d.x * cropScale * this.globalScale);
                comp.props.crop.b = this.clamp(comp.props.crop.b, 0.0, 1.0);
            } else if (this.cropAxisMode === 'l') {
                comp.props.crop.l += (vector2d.x * cropScale * this.globalScale);
                comp.props.crop.l = this.clamp(comp.props.crop.l, 0.0, 1.0);
            }
        }
    }

    nextPropMode(currentMode) {
        switch (currentMode) {
            case 'translate': return 'rotate';
            case   'rotate' : return 'scale';
            case    'scale' : return 'crop';
            case     'crop' : return 'translate';
        }
    }

    nextRotateAxis(axis) {
        switch (axis) {
            case 'x': return 'y';
            case 'y': return 'z';
            case 'z': return 'x';
        }
    }

    nextScaleAxis(axis) {
        switch (axis) {
            case 'xy': return 'x';
            case 'x': return 'y';
            case 'y': return 'xy';
        }
    }

    nextCropAxis(axis) {
        switch (axis) {
            case 't': return 'r';
            case 'r': return 'b';
            case 'b': return 'l';
            case 'l': return 't';
        }
    }

    clamp(val, min, max) {
        return Math.min(Math.max(val, min), max);
    }

    addDetailsUpdateListener(callback) {
        this.detailsUpdateSubscribers.push(callback);
    }

    onDetailsUpdate() {
        for (let callback of this.detailsUpdateSubscribers) {
            callback();
        }
    }

    outputDetails(threeDObj = null) {
        var details = `Prop Mode: ${this.propMode}\n`;

        var axisMode;
        if (this.propMode === 'translate') {
            axisMode = (this.planeModActive) ? 'zy' : 'xy';
        } if (this.propMode === 'rotate') {
            axisMode = this.rotateAxisMode;
        } else if (this.propMode === 'scale') {
            axisMode = this.scaleAxisMode;
        } else if (this.propMode === 'crop') {
            axisMode = this.cropAxisMode;
        }
        details += `Axis Mode: ${axisMode}\n`;

        if (threeDObj && threeDObj.props) {
            var props = threeDObj.props;

            if (props.position) {
                var pos = this.formatVec(props.position, 1);
                details += `Position: {${pos.x}, ${pos.y}, ${pos.z}}\n`;
            }

            if (props.rotation) {
                var rot = this.formatVec(props.rotation, 2);
                details += `Rotation: {${rot.x}, ${rot.y}, ${rot.z}}\n`;
            }

            if (props.scale) {
                var sca = this.formatVec(props.scale, 2);
                details += `Scale: {${sca.x}, ${sca.y}, ${sca.z}}\n`;
            }

            if (props.crop) {
                var cro = this.formatRect(props.crop, 2);
                details += `Crop: {${cro.t}, ${cro.r}, ${cro.b}, ${cro.l}}\n`;
            }
        }

        return details;
    }
}
