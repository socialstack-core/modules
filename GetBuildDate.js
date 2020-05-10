var buildDate = null;

/*
* Reads the build date whenever it's available.
* It originates from the main.generated?v=X number (where X is actually a UTC unix timestamp in ms)
*/
module.exports = () => {
	if(!buildDate){
		var scr = global.document.scripts;
		for(var i=0;i<scr.length;i++){
			var src = scr[i].src;
			
			if(!src || src.indexOf("generated.js") == -1){
				continue;
			}
			
			var versionParts = src.split('?v=');
			if(versionParts.length == 2){
				var timestamp = versionParts[1];
				buildDate = new Date(parseInt(timestamp)).toUTCString();
			}
		}
	}
	
	return buildDate;
};