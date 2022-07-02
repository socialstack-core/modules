/*
* Clones an object but omits certain keys.
* Great for forwarding props - will also accept a prop spec.
* For example
* omit({a: 1, b: 2}, 'b') will return {a: 1}.
* omit({a: 1, b: 2, c: 3}, ['b', 'c']) will return {a: 1}.
* keys called 'children' are ignored by default unless you set noDefaults to true.
*/
export default function(object, keyOrKeys, noDefaults){
	if(!object){
		return object;
	}
	keyOrKeys = keyOrKeys instanceof Array ? keyOrKeys : [keyOrKeys];
	var result = {};
	for(var key in object){
		if(!noDefaults){
			if(key == 'children'){
				// exclude
				continue;
			}
		}
		
		if(keyOrKeys.indexOf(key) == -1){
			// not excluded
			result[key] = object[key];
		}
	}
	return result;
}