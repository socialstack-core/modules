/*
* {a:'hi', b:'hello'} becomes ?a=hi&b=hello etc.
*/
export default function queryString(fields) {
	
	if(!fields){
		return '';
	}
	
	var qs = '';
	
	for(var f in fields){
		if(qs.length){
			qs += '&';
		}
		qs += encodeURIComponent(f) + '=' + encodeURIComponent(fields[f]);
	}
	
	return '?' + qs;
}