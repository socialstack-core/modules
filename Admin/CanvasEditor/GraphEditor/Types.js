var number = {
	color: [1, 0.85, 0.5], // red
	name: 'number' // don't translate
};

var decimal = {
	color: [0.08, 0.85, 0.5], // orange
	name: 'decimal' // don't translate
};

var img_type = {
	color: [0.525, 0.42, 0.73], // pastel blue
	name: 'image' // don't translate
};

var string_type = {
	color: [0.83, 0.383, 0.466],
	name: 'text'
};

var comp_type = {
	color: [0.15, 1, 0.5],
	name: 'component'
};

var bool_type = {
	color: [ 0.69, 0.03, 0.21 ],
	name: 'checkbox'
};

var canvas = {
	color: [ 0.4, 0.7, 0.7 ],
	name: 'canvas'
};

var anything = {
	name: 'anything',
	color: [0,0,1],
	isAny: true
};

var exec = {
	color: [0.33, 1, 0.39], 
	name: 'execute'
};

var existing_types = {
	'bool': bool_type,
	'boolean': bool_type,
	'checkbox': bool_type,
	'byte': number,
	'sbyte': number,
	'short': number,
	'ushort': number,
	'int': number,
	'uint': number,
	'long': number,
	'ulong': number,
	'number': number,
	'canvas': canvas,
	'float': decimal,
	'double': decimal,
	'decimal': decimal,
	'ref': img_type,
	'image': img_type,
	'file': img_type,
	'component': comp_type,
	'jsx': comp_type,
	'array': {
		name: 'Any list',
		color: [0,0,1],
		isArray: true
	},
	'anything': anything,
	'object': anything,
	'string': string_type,
	'text': string_type,
	'execute': exec
};

export function isDefaultType(type){
	return type.name ? existing_types[type.name] : existing_types[type];
}

export function colorAsHsl(hslArray){
	return 'hsl(' + (hslArray[0] * 360) + ',' + (hslArray[1] * 100) + '%,' + (hslArray[2] * 100) + '%)';
}

function hashCode(str) {
    var hash = 0;
    for (var i = 0; i < str.length; i++) {
       hash = str.charCodeAt(i) + ((hash << 5) - hash);
    }
    return hash;
} 

export function getType(type){
	
	var isArray = (type.elementType || type == 'array');
	
	if(isArray){
		if(!type.elementType){
			return lookupByName('array');
		}
		var eleType = getType(type.elementType);
		
		return {
			type: 'array',
			isArray: true,
			elementType: eleType,
			color: eleType.color,
			name: `A list of ${eleType.name}s`
		};
	}else{
		return lookupByName(type.name || type);
	}
}

function lookupByName(typeName){
	typeName = typeName.toLowerCase();
	// Common names:
	var existing = existing_types[typeName];
	
	if(existing){
		return existing;
	}
	
	// Generate:
	var i = hashCode('rng:' + typeName);
	var h = i & 0x000000FF;
	var l = (i & 0x0000FF00) >> 8;
	var s = (i & 0x00FF0000) >> 16;
	
	existing = {
		color: [h / 255,s / 255,l / 255],
		name: typeName
	};
	
	existing_types[typeName] = existing;
	return existing;
}

var OK = {ok: true};
var MISSING = {error: `One of the connecting types is missing`};
var INCOMPAT = {error: `The types are too different`};
var ARRAY_COMPAT = {error: `List element types do not match, and the output does not permit any list`};

/*
* returns info on if the given in/out combo is compatible.
*/
export function typeCompatibility(input, output){
	
	if(!input || !output){
		return MISSING;
	}
	
	if(input == output){
		// That was easy
		return OK;
	}
	
	// Might have a string vs an object etc.
	if(typeof input === 'string'){
		input = {name: input};
	}
	
	if(typeof output === 'string'){
		output = {name: output};
	}
	
	if(output.name == 'anything' && input.name != 'array'){
		// Output accepts any non-array
		return OK;
	}
	
	var inputMatched = existing_types[input.name.toLowerCase()];
	var outputMatched = existing_types[input.name.toLowerCase()];
	
	if(inputMatched && outputMatched && (inputMatched == outputMatched || inputMatched.name == outputMatched.name)){
		// Common types or their aliases
		return OK;
	}
	
	if(input.name != output.name){
		return INCOMPAT;
	}
	
	// Might both be array. Check elementType and if any is permitted.
	if(input.name == 'array'){
		
		if(output.elementType){
			// Output does have a strict element type requirement. Does it get satisfied?
			if(typeCompatibility(input.elementType, output.elementType).ok){
				return OK;
			}else{
				return ARRAY_COMPAT;
			}
		}else{
			// Output doesn't care what type of array it is given.
			return OK;
		}
		
	}
	
	// The names are the same and they are concrete non-array types. All other situs are ok.
	return OK;
}