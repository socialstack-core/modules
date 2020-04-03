var latest = null;
var interval = null;

/*
* Gets the current location, defaulting to resolving it every 15 seconds.
* Returns a promise.
*/

function setCurrent(loc){
	latest = loc.coords;
}

function done(opts, success, coords){
	if(opts.grid){
		coords.grid = gridId(coords.latitude, coords.longitude, {res: opts.grid});
	}
	success(coords);
}

function get(opts){
	opts = opts || {};
	var updateTime = opts.updateTime || 15000;
	
	return new Promise((success, reject) => {
		
		if(opts.lockTo){
			return done(opts, success, opts.lockTo);
		}
		
		navigator.geolocation.getCurrentPosition(function(location) {
			setCurrent(location);
			
			done(opts, success, latest);
			
			// Update every x seconds:
			if(updateTime == -1 || interval != null){
				return;
			}
			
			interval = setInterval(() => {
				
				navigator.geolocation.getCurrentPosition(function(location) {
					setCurrent(location);
				});
				
			}, updateTime);
		}, reject);
		
	});
}

/*
Used to griddify GPS coordinates.
Helps find things within approximately x km rapidly
*/

function gridId(lat, lon, opts) {
	opts = opts || {};
	
	// Grid resolution. This defines how big the cells are. Specifically the number is how many of them are on the equator.
	// 8000 on the ~40,000km equator represents one every 5km or so.
	var res = opts.res || 8000;
	var scalar = res / 360; // Maps the grid to lat/lon range.
	
	return Math.floor((lat + 90) * scalar) + res * Math.floor((lon + 180) * scalar);
};

module.exports = (opts) => {
	if(interval){
		return Promise.resolve(current);
	}
	
	return get(opts);
};

module.exports.gridId = gridId;