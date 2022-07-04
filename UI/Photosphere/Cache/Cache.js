//import THREE from 'UI/Functions/ThreeJs';
var THREE = require('UI/Functions/ThreeJs');

var _cache = {};

export default function cache (url, onLoaded){
	if(_cache[url]){
		onLoaded();
		return _cache[url];
	}
	return _cache[url] = new THREE.TextureLoader().load(url, onLoaded);
}
