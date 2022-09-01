import React, { Component } from 'react';
import Icon from 'UI/Icon';

function clamp(min, value, max) {
  return Math.max(min, Math.min(value, max));
}

function distance(p1, p2) {
  const dx = p1.x - p2.x;
  const dy = p1.y - p2.y;
  return Math.sqrt(Math.pow(dx, 2) + Math.pow(dy, 2));
}

function midpoint(p1, p2) {
  return {
    x: (p1.x + p2.x) / 2,
    y: (p1.y + p2.y) / 2
  };
}

function touchPt(touch) {
  return { x: touch.clientX, y: touch.clientY };
}

function touchDistance(t0, t1) {
  const p0 = touchPt(t0);
  const p1 = touchPt(t1);
  return distance(p0, p1);
}

let passiveSupported;

function makePassiveEventOption(passive) {
	
	if(passiveSupported === undefined){
		try {
			const options = {
				get passive() {
					passiveSupported = true;
				}
			};
			window.addEventListener("test", options, options);
			window.removeEventListener("test", options, options);
		} catch {
			passiveSupported = false;
		}
	}
	
	return passiveSupported ? { passive } : passive;
}

// The amount that a value of a dimension will change given a new scale
const coordChange = (coordinate, scaleRatio) => {
  return (scaleRatio * coordinate) - coordinate;
};

/*
  This contains logic for providing a map-like interaction to any DOM node.
  It allows a user to pinch, zoom, translate, etc, as they would an interactive map.
  It renders its children with the current state of the translation and does not do any scaling
  or translating on its own. This works on both desktop, and mobile.
*/
export class MapInteractionControlled extends Component {
  
  static get defaultProps() {
    return {
      minScale: 0.1,
      maxScale: 8,
      showControls: false,
      translationBounds: {},
      disableZoom: false,
      disablePan: false
    };
  }

  constructor(props) {
    super(props);

	global._map = this;

    this.state = {
      shouldPreventTouchEndDefault: false
    };

    this.startPointerInfo = undefined;

    this.onMouseDown = this.onMouseDown.bind(this);
    this.onTouchStart = this.onTouchStart.bind(this);

    this.onMouseMove = this.onMouseMove.bind(this);
    this.onTouchMove = this.onTouchMove.bind(this);

    this.onMouseUp = this.onMouseUp.bind(this);
    this.onTouchEnd = this.onTouchEnd.bind(this);

    this.onWheel = this.onWheel.bind(this);
  }

  componentDidMount() {
    const passiveOption = makePassiveEventOption(false);

    this.getContainerNode().addEventListener('wheel', this.onWheel, passiveOption);

    /*
      Setup events for the gesture lifecycle: start, move, end touch
    */

    // start gesture
    this.getContainerNode().addEventListener('touchstart', this.onTouchStart, passiveOption);
    this.getContainerNode().addEventListener('mousedown', this.onMouseDown, passiveOption);

    // move gesture
    window.addEventListener('touchmove', this.onTouchMove, passiveOption);
    window.addEventListener('mousemove', this.onMouseMove, passiveOption);

    // end gesture
    const touchAndMouseEndOptions = { capture: true, ...passiveOption };
    window.addEventListener('touchend', this.onTouchEnd, touchAndMouseEndOptions);
    window.addEventListener('mouseup', this.onMouseUp, touchAndMouseEndOptions);
	
	if(this.props.target){
		this.goToTarget(this.props);
	}
  }
  
  resetToInitialState(){
	  this.props.resetToInitialState();
  }
  
  componentWillUnmount() {
	  
	if(this.isAnimating){
		window.cancelAnimationFrame(this.isAnimating);
		this.isAnimating = null;
	}
	
    this.getContainerNode().removeEventListener('wheel', this.onWheel);

    // Remove touch events
    this.getContainerNode().removeEventListener('touchstart', this.onTouchStart);
    window.removeEventListener('touchmove', this.onTouchMove);
    window.removeEventListener('touchend', this.onTouchEnd);

    // Remove mouse events
    this.getContainerNode().removeEventListener('mousedown', this.onMouseDown);
    window.removeEventListener('mousemove', this.onMouseMove);
    window.removeEventListener('mouseup', this.onMouseUp);
  }

  /*
    Event handlers
    All touch/mouse handlers preventDefault because we add
    both touch and mouse handlers in the same session to support devicse
    with both touch screen and mouse inputs. The browser may fire both
    a touch and mouse event for a *single* user action, so we have to ensure
    that only one handler is used by canceling the event in the first handler.
    https://developer.mozilla.org/en-US/docs/Web/API/Touch_events/Supporting_both_TouchEvent_and_MouseEvent
  */

  onMouseDown(e) {
    e.preventDefault();
    this.setPointerState([e]);
  }

  onTouchStart(e) {
    // prevent default only if we are not clicking on an anchor tag
    if (!(e?.target?.nodeName === "A" || e?.target?.parentNode?.nodeName === "A")) {
      e.preventDefault();
    }

    this.setPointerState(e.touches);
  }

  onMouseUp(e) {
    this.setPointerState();
  }

  onTouchEnd(e) {
    this.setPointerState(e.touches);
  }

  onMouseMove(e) {
    if (!this.startPointerInfo || this.props.disablePan || this.isAnimating) {
      return;
    }
    e.preventDefault();
    this.onDrag(e);
  }

  onTouchMove(e) {
    if (!this.startPointerInfo || this.isAnimating) {
      return;
    }

    e.preventDefault();

    const { disablePan, disableZoom } = this.props;

    const isPinchAction = e.touches.length == 2 && this.startPointerInfo.pointers.length > 1;
    if (isPinchAction && !disableZoom) {
      this.scaleFromMultiTouch(e);
    } else if ((e.touches.length === 1) && this.startPointerInfo && !disablePan) {
      this.onDrag(e.touches[0]);
    }
  }

  // handles both touch and mouse drags
  onDrag(pointer) {
    const { translation, pointers } = this.startPointerInfo;
    const startPointer = pointers[0];
    const dragX = pointer.clientX - startPointer.clientX;
    const dragY = pointer.clientY - startPointer.clientY;
    const newTranslation = {
      x: translation.x + dragX,
      y: translation.y + dragY
    };

    const shouldPreventTouchEndDefault = Math.abs(dragX) > 1 || Math.abs(dragY) > 1;

    this.setState({
      shouldPreventTouchEndDefault
    }, () => {
      this.props.onChange({
        ...this.props.value,
        translation: this.clampTranslation(newTranslation)
      });
    });
  }
  
  onRotate(dir){ // + or - 90
	  var rotation = this.props.value.rotation + dir;
	  
	  // clamp:
	  if(rotation < 0){
		  rotation += 360;
	  }else if(rotation >= 360){
		  rotation -= 360;
	  }
	  
	  this.props.onChange({
        ...this.props.value,
        rotation
      });
  }
  
  onWheel(e) {
    if (this.props.disableZoom) {
      return;
    }

    e.preventDefault();
    e.stopPropagation();

    const scaleChange = 2 ** (e.deltaY * 0.002);
	
	var userScale = this.props.value.userScale || 1;
	userScale += (1 - scaleChange);
	
	// Scale power adjusted such that it gives the user a continuous scaling velocity
	
    const newScale = clamp(
      this.props.minScale,
      Math.pow(2, userScale - 1),
      this.props.maxScale
    );
	
    const mousePos = this.clientPosToTranslatedPos({ x: e.clientX, y: e.clientY });

    this.scaleFromPoint(newScale, mousePos, userScale);
  }

  setPointerState(pointers) {
    if (!pointers || pointers.length === 0) {
      this.startPointerInfo = undefined;
      return;
    }

    this.startPointerInfo = {
      pointers,
	  ...this.props.value
    }
  }

  clampTranslation(desiredTranslation, props = this.props) {
    const { x, y } = desiredTranslation;
    let { xMax, xMin, yMax, yMin } = props.translationBounds;
    xMin = xMin != undefined ? xMin : -Infinity;
    yMin = yMin != undefined ? yMin : -Infinity;
    xMax = xMax != undefined ? xMax : Infinity;
    yMax = yMax != undefined ? yMax : Infinity;

    return {
      x: clamp(xMin, x, xMax),
      y: clamp(yMin, y, yMax)
    };
  }

  translatedOrigin(translation = this.props.value.translation) {
    const clientOffset = this.getContainerBoundingClientRect();
    return {
      x: clientOffset.left + translation.x,
      y: clientOffset.top + translation.y
    };
  }

  // From a given screen point return it as a point
  // in the coordinate system of the given translation
  clientPosToTranslatedPos({ x, y }, translation = this.props.value.translation) {
    const origin = this.translatedOrigin(translation);
    return {
      x: x - origin.x,
      y: y - origin.y
    };
  }

  scaleFromPoint(newScale, focalPt, userScale) {
    const { translation, scale } = this.props.value;
    const scaleRatio = newScale / (scale != 0 ? scale : 1);

    const focalPtDelta = {
      x: coordChange(focalPt.x, scaleRatio),
      y: coordChange(focalPt.y, scaleRatio)
    };

    const newTranslation = {
      x: translation.x - focalPtDelta.x,
      y: translation.y - focalPtDelta.y
    };
    this.props.onChange({
		...this.props.value,
      scale: newScale,
      userScale,
      translation: this.clampTranslation(newTranslation)
    })
  }

  // Given the start touches and new e.touches, scale and translate
  // such that the initial midpoint remains as the new midpoint. This is
  // to achieve the effect of keeping the content that was directly
  // in the middle of the two fingers as the focal point throughout the zoom.
  scaleFromMultiTouch(e) {
    var startTouches = this.startPointerInfo.pointers;
    var newTouches   = e.touches;

    // calculate new scale
    var dist0       = touchDistance(startTouches[0], startTouches[1]);
    var dist1       = touchDistance(newTouches[0], newTouches[1]);
    var delta = dist1 / dist0;
	
	var userScale = (this.startPointerInfo.userScale || 1) * delta;
    var { minScale, maxScale } = this.props;
    var scale = clamp(minScale, Math.pow(2, userScale - 1), maxScale);
	
	var startMidpoint = midpoint(touchPt(startTouches[0]), touchPt(startTouches[1]));
    var x = startMidpoint.x;
    var y = startMidpoint.y;
	
    var focalPoint = this.clientPosToTranslatedPos({ x, y });
    this.scaleFromPoint(scale, focalPoint, userScale);
  }

  discreteScaleStepSize() {
    const { minScale, maxScale } = this.props;
    const delta = Math.abs(maxScale - minScale);
    return delta / 10;
  }

  // Scale using the center of the content as a focal point
  changeScale(delta) {
    const userScale = (this.props.value.userScale || 1) + delta;
    const { minScale, maxScale } = this.props;
    const scale = clamp(minScale, Math.pow(2, userScale - 1), maxScale);

    const rect = this.getContainerBoundingClientRect();
    const x = rect.left + (rect.width / 2);
    const y = rect.top + (rect.height / 2);

    const focalPoint = this.clientPosToTranslatedPos({ x, y });
    this.scaleFromPoint(scale, focalPoint, userScale);
  }

  // Done like this so it is mockable
  getContainerNode() { return this.containerNode }
  getContainerBoundingClientRect() {
    return this.getContainerNode().getBoundingClientRect();
  }

  renderControls() {
    const step = this.discreteScaleStepSize();
	  return <div className="overhead-ui">
			{this.props.controls(step, this.props.value, this)}
		</div>;
  }
  
  goToTarget(props){
	  // New target
		var targetRotation = props.value.rotation;
		
		if(props.targetRotation){
			// Immediately rotate
			targetRotation = Math.round(props.targetRotation);
			
			if(targetRotation == 360){
				targetRotation = 0; // 360 used to indicate 0
			}
			
			this.props.onChange({
				...props.value,
				rotation: targetRotation
			});
		}
		
		if(this.isAnimating){
			window.cancelAnimationFrame(this.isAnimating);
			this.isAnimating = null;
		}
		
		// Step 1: Get the target hotspot in terms of its current screen position
		var w = document.body.clientWidth;
		var h = document.body.clientHeight;
		
		var widthHalf = 0.5 * w;
		var heightHalf = 0.5 * h;
		
		// This represents the translation at the current zoom and camera orientation
		var spV = this.props.getScreenPosition(props.target);
		
		var screenX = ( spV.x * widthHalf ) + widthHalf;
		var screenY = ( spV.y * heightHalf ) + heightHalf;
		
		var startX = props.value.translation.x;
		var startY = props.value.translation.y;
		var startUserZoom = props.value.userScale || 1;
		var startScale = props.value.scale || 1;
		var targetScale = props.target.targetZoomLevel || 1;
		
		if(w < 1920){
			targetScale /= (1920 / w);
		}
		
		var scaleRatio = targetScale / startScale;
		
		var targetX = startX - ( spV.x * widthHalf );
		var targetY = startY + ( spV.y * heightHalf );
		
		// Offset for the sidebar:
		targetX += (w > 1200) ? 150 : 70;
		
		targetX = targetX - coordChange(widthHalf - targetX, scaleRatio);
		targetY = targetY - coordChange(heightHalf - targetY, scaleRatio);
		
		var targetUserScale = Math.sqrt(targetScale) + 1;
		
		var start = null;
		var time = 3000;
		
		var step = (currentTime) => {
			start = !start ? currentTime : start;
			var progress = (currentTime - start) / time;
			
			if(progress > 1){
				progress = 1;
			}
			
			// progress is currently linear - apply a timing function (easeInOut):
			var easedProgress = progress<.5 ? 2*progress*progress : 1-((progress-1)*(progress-1)*2);
			
			var newScale = startScale + ((targetScale - startScale) * easedProgress);
			
			var curX = startX + ((targetX - startX) * easedProgress);
			var curY = startY + ((targetY - startY) * easedProgress);
			
			this.props.onChange({
				...props.value,
			  scale: newScale,
			  userScale: targetUserScale,
			  translation: {
				  x: curX,
				  y: curY
			  }
			});
			
			if (progress < 1) {
				window.requestAnimationFrame(step);
			}else{
				this.isAnimating = null;
			}
		};
		
		this.isAnimating = step;
		
		window.requestAnimationFrame(step);
		
  }
  
componentWillReceiveProps(props){
	if(this.props.target != props.target){
		this.goToTarget(props);
	}
}

  render() {
    const { controls, children } = this.props;
    const scale = this.props.value.scale;
    // Defensively clamp the translation. This should not be necessary if we properly set state elsewhere.
    const translation = this.clampTranslation(this.props.value.translation);
	
	const rotation = this.props.value.rotation;
	

    /*
      This is a little trick to allow the following ux: We want the parent of this
      component to decide if elements inside the map are clickable. Normally, you wouldn't
      want to trigger a click event when the user *drags* on an element (only if they click
      and then release w/o dragging at all). However we don't want to assume this
      behavior here, so we call `preventDefault` and then let the parent check
      `e.defaultPrevented`. That value being true means that we are signalling that
      a drag event ended, not a click.
    */
    const handleEventCapture = (e) => {
      if (this.state.shouldPreventTouchEndDefault) {
        e.preventDefault();
        this.setState({ shouldPreventTouchEndDefault: false });
      }
    }

    return (
      <div
        ref={(node) => {
          this.containerNode = node;
        }}
        style={{
          height: '100%',
          width: '100%',
          position: 'relative', // for absolutely positioned children
          touchAction: 'none'
        }}
        onClickCapture={handleEventCapture}
        onTouchEndCapture={handleEventCapture}
      >
        {(children || undefined) && children({ translation, scale, rotation })}
		{controls && this.renderControls()}
      </div>
    );
  }
}

/*
  Main entry point component.
  Determines if it's parent is controlling (eg it manages state) or leaving us uncontrolled
  (eg we manage our own internal state)
*/
class MapInteractionController extends Component {
  
  constructor(props) {
    super(props);

    const controlled = MapInteractionController.isControlled(props);
    if (controlled) {
      this.state = {
        lastKnownValueFromProps: props.value
      };
    } else {

      // Set the necessary state for controlling map interaction ourselves
	  var initialScale = props.screenScale;
	  
      this.state = {
        value: props.defaultValue || {
          scale: initialScale,
		  userScale: initialScale,
		  rotation : 0,
          translation: { 
              x: 0, 
              y: 0
		  }
        },
        lastKnownValueFromProps: undefined
      };
    }
  }
	
	resetToInitialState(){
		var initialScale = this.props.screenScale;
	  
		this.setState({
			value: this.props.defaultValue || {
			  scale: initialScale,
			  userScale: initialScale,
			  rotation : 0,
			  translation: { 
				  x: 0, 
				  y: 0
			  }
			},
			lastKnownValueFromProps: undefined
      });
	}
	
  /*
    Handle the parent switchg form controlled to uncontrolled or vice versa.
    This is at most a best-effort attempt. It is not gauranteed by our API
    but it will do its best to maintain the state such that if the parent
    accidentally switches between controlled/uncontrolled there won't be
    any jankiness or jumpiness.
    This tries to mimick how the React <input /> component behaves.
  */
  static getDerivedStateFromProps(props, state) {
    const nowControlled = MapInteractionController.isControlled(props);
    const wasControlled = state.lastKnownValueFromProps && MapInteractionController.isControlled({ value: state.lastKnownValueFromProps })

    /*
      State transitions:
        uncontrolled --> controlled   (unset internal state, set last props from parent)
        controlled   --> uncontrolled (set internal state to last props from parent)
        controlled   --> controlled   (update last props from parent)
        uncontrolled --> uncontrolled (do nothing)
      Note that the second two (no change in control) will also happen on the
      initial render because we set lastKnownValueFromProps in the constructor.
    */
    if (!wasControlled && nowControlled) {
      return {
        value: undefined,
        lastKnownValueFromProps: props.value
      };
    } else if (wasControlled && !nowControlled) {
      return {
        value: state.lastKnownValueFromProps,
        lastKnownValueFromProps: undefined
      };
    } else if (wasControlled && nowControlled) {
      return { lastKnownValueFromProps: props.value };
    } else if (!wasControlled && !nowControlled) {
      return null;
    }
  }

  static isControlled(props) {
    // Similar to React's <input /> API, setting a value declares
    // that you want to control this component.
    return props.value != undefined;
  }

  // The subset of this component's props that need to be passed
  // down to the core RMI component
  innerProps() {
    const { value, defaultValue, onChange, ...innerProps } = this.props;
    return innerProps;
  }

  getValue() {
    const controlled = MapInteractionController.isControlled(this.props);
    return controlled ? this.props.value : this.state.value;
  }

  render() {
    const { onChange, children } = this.props;
    const controlled = MapInteractionController.isControlled(this.props);
    const value = controlled ? this.props.value : this.state.value;
    return (
      <MapInteractionControlled
        onChange={(value) => {
          controlled ? onChange(value) : this.setState({ value });
        }}
        value={value}
		resetToInitialState={() => this.resetToInitialState()}
        {...this.innerProps()}
      >
       {children}
      </MapInteractionControlled>
    );
  }
}

export default MapInteractionController;