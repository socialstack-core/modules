const _hash1 = ((5381 << 16) + 5381)|0;

const floor = Math.floor;

/**
 * Math.imul() polyfill
 * https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Math/imul#:~:text=imul()%20allows%20for%2032,Math%20is%20not%20a%20constructor).
 */
if (!Math.imul) Math.imul = function (opA, opB) {
	opB |= 0; // ensure that opB is an integer. opA will automatically be coerced.
	// floating points give us 53 bits of precision to work with plus 1 sign bit
	// automatically handled for our convienence:
	// 1. 0x003fffff /*opA & 0x000fffff*/ * 0x7fffffff /*opB*/ = 0x1fffff7fc00001
	//    0x1fffff7fc00001 < Number.MAX_SAFE_INTEGER /*0x1fffffffffffff*/
	var result = (opA & 0x003fffff) * opB;
	// 2. We can remove an integer coersion from the statement above because:
	//    0x1fffff7fc00001 + 0xffc00000 = 0x1fffffff800001
	//    0x1fffffff800001 < Number.MAX_SAFE_INTEGER /*0x1fffffffffffff*/
	if (opA & 0xffc00000 /*!== 0*/) result += (opA & 0xffc00000) * opB | 0;
	return result | 0;
};

/*
	Converts a typeName like "BlogPost" to its numeric content type ID.
	If porting this, instead take a look at the C# version in ContentTypes.cs. 
	Most of the stuff here is for forcing JS to do integer arithmetic.
*/
export default function(typeName) {
	typeName = typeName.toLowerCase();
	var hash1 = _hash1;
	var hash2 = hash1;
	
	for (var i = 0; i < typeName.length; i += 2)
	{
		var s1 = ~~floor(hash1 << 5);
		hash1 = ~~floor(s1 + hash1);
		hash1 = hash1 ^ typeName.charCodeAt(i);
		if (i == typeName.length - 1)
			break;
		
		s1 = ~~floor(hash2 << 5);
		hash2 = ~~floor(s1 + hash2);
		hash2 = hash2 ^ typeName.charCodeAt(i+1);
	}
	
	var result = ~~floor(Math.imul(hash2, 1566083941));
	result = ~~floor(hash1 + result);
	return result;
};