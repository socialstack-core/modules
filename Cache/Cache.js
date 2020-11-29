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
		hls = new hlsjs({startFragPrefetch: true, liveMaxLatencyDuration: 12, liveSyncDuration: 3});
		hls.loadSource(src);
		hls.__manif = [onManifest];
		hls.on(hlsjs.Events.MANIFEST_PARSED, () => {
			hls.___manifest = true;			
			hls.__manif.map(s => {
				s && s();
			});
		});
		
		// When adding these, only the ones in the future and the latest from the past will be triggered.
		// Each entry is {time: serverTime, mtd: methodToTrigger}.
		// If it has an id field, it will ensure that it is ticked only ever once, even if you add it repeatedly.
		hls.addTimedEvents = events => {
			if(!events || !events.length){
				return;
			}
			
			if(!hls.__eTimer){
				hls.__pend = [];
				hls.__tickedEvts = {};
				
				// 10 times per second, check if there are any events in the queue.
				hls.__tick = () => {
					
					// Get time:
					var time = hls.getServerTime();
					
					if(time == 0){
						// Not loaded yet
						return;
					}
					
					// Find the latest one to activate.
					var liveNow = -1;
					for(var i=0; i<hls.__pend.length;i++){
						if(hls.__pend[i].time < time){
							liveNow = i;
						}else{
							break;
						}
					}
					
					if(liveNow != -1){
						
						var active = hls.__pend[liveNow];
						
						// Slice off that much:
						hls.__pend = hls.__pend.slice(liveNow + 1);
						
						// Trigger it:
						try{
							active.mtd && active.mtd();
						}catch(e){
							console.error(e);
						}
					}
					
				};
				hls.__eTimer = setInterval(hls.__tick, 100);
				
				hls.on(hlsjs.Events.DESTROYING, () => {
					clearInterval(hls.__eTimer);
				});
			}
			
			// Add to pending:
			for(var i=events.length-1; i>=0;i--){
				var e = events[i];
				if(e.id && hls.__tickedEvts[e.id + "_"]){
					continue;
				}
				hls.__tickedEvts[e.id + "_"] = true;
				hls.__pend.push(e);
			}
			
			hls.__pend.sort((a,b) => a.time > b.time ? 1 : -1);
			hls.__tick();
		};
		
		/* a long integer in UTC milliseconds of the currently measurable time at the stream 
		server for the piece of the stream _this_ player is at */
		hls.getServerTime = () => {
			if(!hls.media || !hls.streamController){
				// Media not loaded
				return 0;
			}
			
			var fc = hls.streamController.fragCurrent;
			
			if(!fc){
				// Media not loaded
				return 0;
			}
			
			var serverTimestamp;
			
			if(fc == hls.__sChunk){
				// Cached:
				serverTimestamp = hls.__sStamp;
			}else{
				hls.__sChunk = fc;
				var fSlash = fc._url.lastIndexOf('/');
				
				// UTC in ms of the start of the current chunk:
				serverTimestamp = hls.__sStamp = parseInt(fc._url.substring(fSlash + 1, fc._url.length - 3));
			}
			
			// Current chunk starts in this many ms:
			var deltaInMs = (fc.start - hls.media.currentTime) * 1000;
			
			// which means the above timestamp is that delta in the future:
			return Math.floor(serverTimestamp - deltaInMs);
		};
		
		// Caching is off for now:
		// cache[src] = hls;
	}
	
	return hls;
};