import hlsjs from '../hls.js';

var cache = {};

module.exports = (src, onManifest) => {
	var hls = cache[src];
	
	if(hls){
		if(hls.__manif){
			onManifest && onManifest();
		}else{
			hls.___manifest.push(onManifest);
		}
	}else{
		hls = new hlsjs({startFragPrefetch: true});
		hls.loadSource(src);
		hls.__manif = [onManifest];
		hls.on(hlsjs.Events.MANIFEST_PARSED, () => {
			hls.___manifest = true;			
			hls.__manif.map(s => {
				s && s();
			});
		});
		
		// Caching is off for now:
		// cache[src] = hls;
	}
	
	return hls;
};