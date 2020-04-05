
/*
* Gets a UI module by its public name. Case sensitive. E.g. getModule("UI/Start")
*/

export default function getModule(name) {
	var parts = name.split('/');
	name += '/' + parts[parts.length-1] + '.js';
	return global.require(name);
}