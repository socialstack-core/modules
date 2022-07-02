
const anyKey = 'AnyKey';

var keysDown = [];
var mouseMoveCallbacks = {};
var mouseDownCallbacks = {};
var mouseUpCallbacks   = {};
var keyDownCallbacks   = {};
var keyUpCallbacks     = {};

document.addEventListener('mousemove', (e) => processMouseMove(e));
document.addEventListener('mousedown', (e) => processMouseDown(e));
document.addEventListener('mouseup',   (e) => processMouseUp(e));
document.addEventListener('keydown',   (e) => processKeyDown(e));
document.addEventListener('keyup',     (e) => processKeyUp(e));

function processMouseMove(e) {
    callback(e.buttons, e, mouseMoveCallbacks);
    callback(anyKey, e, mouseMoveCallbacks);
}

function processMouseDown(e) {
    callback(e.buttons, e, mouseDownCallbacks);
    callback(anyKey, e, mouseDownCallbacks);
}

function processMouseUp(e) {
    callback(e.buttons, e, mouseUpCallbacks);
    callback(anyKey, e, mouseUpCallbacks);
}

function processKeyDown(e) {
    if (!keysDown.includes(e.code)) {
        keysDown.push(e.code);

        callback(e.code, e, keyDownCallbacks);
        callback(anyKey, e, keyDownCallbacks);
    }
}

function processKeyUp(e) {
    if (keysDown.includes(e.code)) {
        var index = keysDown.indexOf(e.code);
        keysDown.splice(index, 1);

        callback(e.code, e, keyUpCallbacks);
        callback(anyKey, e, keyUpCallbacks);
    }
}

function callback(key, event, callbackArray) {
    if (callbackArray[key]) {
        for (let cb of callbackArray[key]) {
            cb(event);
        }
    }
}

function addEventListener(key, callback, callbackArray) {
    var k = key ?? anyKey;

    if (!callbackArray[k]) {
        callbackArray[k] = [];
    }
    callbackArray[k].push(callback);
}

function removeEventListener(keyOrButtons, callback, callbackArray) {
    var k = keyOrButtons ?? anyKey;

    if (!callbackArray[k]) {
        return false;
    }

    if (!callbackArray[k].includes(callback)) {
        return false;
    }

    callbackArray[k] = callbackArray[k].filter(cb => callback !== cb);
    return true;
}

function addMouseMoveListener(buttons, callback) {
    addEventListener(buttons, callback, mouseMoveCallbacks);
}

function removeMouseMoveListener(buttons, callback) {
    return removeEventListener(buttons, callback, mouseMoveCallbacks);
}

function addMouseDownListener(buttons, callback) {
    addEventListener(buttons, callback, mouseDownCallbacks);
}

function removeMouseDownListener(buttons, callback) {
    return removeEventListener(buttons, callback, mouseDownCallbacks);
}

function addMouseUpListener(buttons, callback) {
    addEventListener(buttons, callback, mouseUpCallbacks);
}

function removeMouseUpListener(buttons, callback) {
    return removeEventListener(buttons, callback, mouseUpCallbacks);
}

function addKeyDownListener(key, callback) {
    addEventListener(key, callback, keyDownCallbacks);
}

function removeKeyDownListener(key, callback) {
    return removeEventListener(key, callback, keyDownCallbacks);
}

function addKeyUpListener(key, callback) {
    addEventListener(key, callback, keyUpCallbacks);
}

function removeKeyUpListener(key, callback) {
    return removeEventListener(key, callback, keyUpCallbacks);
}

/**
 * A wrapper for mouse and keyboard events, which fires
 * keydown events without subsequent auto-repeat events
 * by default.
 *
 * Events can be registered for specific keys / buttons
 * any key - To specify any key/button, you can provide
 * null or 'AnyKey' for the key/buttons parameter.
 * 
 * Note that the keyboard key param uses the e.code string
 * on the KeyboardEvent, while the mouse buttons param uses
 * the e.buttons integer on the MouseEvent.
 */
export default {
    addMouseMoveListener,
    removeMouseMoveListener,
    addMouseDownListener,
    removeMouseDownListener,
    addMouseUpListener,
    removeMouseUpListener,
	addKeyDownListener,
	removeKeyDownListener,
    addKeyUpListener,
    removeKeyUpListener,
};