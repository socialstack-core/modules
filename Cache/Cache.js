var THREE = require('UI/Functions/ThreeJs/ThreeJs.js');

var _cache = {};

module.exports = (url, onLoaded) => {
	if(_cache[url]){
		onLoaded();
		return _cache[url];
	}
	return _cache[url] = new THREE.TextureLoader().load(url, onLoaded);
}
