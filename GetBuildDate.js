var buildDate = null;

/*
* Reads the build date whenever it's available.
* It originates from the main.generated?v=X number (where X is actually a UTC unix timestamp in ms)
*/
export default () => {
	if(!buildDate){
		var scr = global.document.scripts;
		for(var i=0;i<scr.length;i++){
			var src = scr[i].src;
			
			if(!src || (src.indexOf("/main.js?") == -1 && src.indexOf("/main.generated.js?") == -1)){
				continue;
			}
			
			var versionParts = src.split('?');
			var vp = versionParts[1].split('&');
			var map = {};
			for(var n=0;n<vp.length;n++){
				var entry = vp[n].split('=');
				if(entry.length == 2){
					map[entry[0]] = entry[1];
				}
			}
			
			var timestamp = parseInt(map['v']);
			var date = new Date(timestamp);
			buildDate = {
				date,
				hash: map['h'],
				dateString: date.toUTCString(),
				timestamp
			};
			break;
		}
	}
	
	return buildDate;
};