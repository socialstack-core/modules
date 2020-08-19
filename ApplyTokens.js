/*
* Replaces {tokens} in a given string with values from the given context.
* For example, applyTokens("hello {world}", {world: 'Jonesy'}); returns the string "hello Jonesy".
* You can use {multiple.layers} if required.
*/

function applyTokens(str, tokenContext){
	if(!str){
		return str;
	}
	
	return str.replace(/\{\w+\}/g, function(textToken) {
		var parts = textToken.substring(1, textToken.length - 1).split('.');
		var context = tokenContext;
		
		for(var i=0;i<parts.length;i++){
			context && (context = context[parts[i]]);
		}
		
		if(context === null || context === undefined){
			// No result in context - return original:
			return textToken;
		}
		
		return context.toString();
	});
}

module.exports = applyTokens;