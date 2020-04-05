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
Used to gridify GPS coordinates.
Helps find things within approximately x km rapidly
*/

function gridId(lat, lon, opts) {
	opts = opts || {};
	
	// Grid resolution. This defines how big the cells are. Specifically the number is how many of them are on the equator.
	// 8000 on the ~40,000km equator represents one every 5km or so.
	var res = opts.res || 8000;
	var scalar = res / 360; // Maps the grid to lat/lon range.
	
	return compose(Math.floor((lat + 90) * scalar), Math.floor((lon + 180) * scalar), res);
}

function compose(latId, lonId, resolution){
	return latId + (lonId * resolution);
}

function decompose(gridId, resolution){
	var lonId = Math.floor(gridId/resolution);
	var latId = gridId - (lonId * resolution);
	
	return {lonId, latId};
}

/*
* Offsets the given lat/lon amount from the given ID on a grid of the given resolution value.
* Note that latitude is clipped, whilst longitude wraps.
*/
function gridOffset(id, latOffset, lonOffset, resolution){
	var gridPos = decompose(id, resolution);
	gridPos.lonId = (gridPos.lonId + lonOffset) % resolution;
	gridPos.latId += latOffset;
	if(gridPos.latId<0){
		gridPos.latId = 0;
	}else if(gridPos.latId >= resolution){
		gridPos.latId = resolution - 1;
	}
	return compose(gridPos.latId, gridPos.lonId, resolution);
}

const deg2Rad = (2 * Math.PI) / 360;

function toRad(deg){
	return deg * deg2Rad;
}

/*
* Circle distance between (lat,lon) and (lat,lon)
*/
function distance(a, b){
	if(!a || !b){
		return 0;
	}
	
	var radius = 6371e3; // metres
	var ang1 = toRad(a.lat);
	var ang2 = toRad(b.lat);
	var d1 = toRad(b.lat-a.lat);
	var d2 = toRad(b.lon-a.lon);
	
	var a = Math.sin(d1/2) * Math.sin(d1/2) +
			Math.cos(ang1) * Math.cos(ang2) *
			Math.sin(d2/2) * Math.sin(d2/2);
	var c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));

	return radius * c;
}

module.exports = (opts) => {
	if(interval && current){
		return Promise.resolve(current);
	}
	
	return get(opts);
};

module.exports.gridId = gridId;
module.exports.gridOffset = gridOffset;
module.exports.distance = distance;