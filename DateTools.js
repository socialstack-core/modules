function ordinal(i) {
    var j = i % 10,
        k = i % 100;
    if (j == 1 && k != 11) {
        return i + "st";
    }
    if (j == 2 && k != 12) {
        return i + "nd";
    }
    if (j == 3 && k != 13) {
        return i + "rd";
    }
    return i + "th";
}

const dayNames = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
const shortDayNames = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

const monthNames = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
const shortMonthNames = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

const isoConvert = (isoish) => {
	
	if(typeof isoish != 'string'){
		// already a date
		return isoish;
	}
	
	 // Split the string into an array based on the digit groups.
	 var dateParts = isoish.split( /\D+/ );
	 // Set up a date object with the current time.
	 var returnDate = new Date();
 
	 // Manually parse the parts of the string and set each part for the
	 // date. Note: Using the UTC versions of these functions is necessary
	 // because we're manually adjusting for time zones stored in the
	 // string.
	 returnDate.setUTCFullYear( parseInt( dateParts[ 0 ] ) );
	 // The month numbers are one "off" from what normal humans would expect
	 // because January == 0.
	 returnDate.setUTCMonth( parseInt( dateParts[ 1 ] - 1 ) );
	 returnDate.setUTCDate( parseInt( dateParts[ 2 ] ) );
 
	 // Set the time parts of the date object.
	 returnDate.setUTCHours( parseInt( dateParts[ 3 ] ) );
	 returnDate.setUTCMinutes( parseInt( dateParts[ 4 ] ) );
	 returnDate.setUTCSeconds( parseInt( dateParts[ 5 ] ) );
	 //returnDate.setUTCMilliseconds( parseInt( dateParts[ 6 ] ) );
 
	 // Track the number of hours we need to adjust the date by based
	 // on the timezone.
	 var timezoneOffsetHours = 0;
 
	 // If there's a value for either the hours or minutes offset.
	 if ( dateParts[ 7 ] || dateParts[ 8 ] ) {
 
		 // Track the number of minutes we need to adjust the date by
		 // based on the timezone.
		 var timezoneOffsetMinutes = 0;
 
		 // If there's a value for the minutes offset.
		 if ( dateParts[ 8 ] ) {
 
			 // Convert the minutes value into an hours value.
			 timezoneOffsetMinutes = parseInt( dateParts[ 8 ] ) / 60;
		 }
 
		 // Add the hours and minutes values to get the total offset in
		 // hours.
		 timezoneOffsetHours = parseInt( dateParts[ 7 ] ) + timezoneOffsetMinutes;
 
		 // If the sign for the timezone is a plus to indicate the
		 // timezone is ahead of UTC time.
		 if ( isoish.substr( -6, 1 ) == "+" ) {
 
			 // Make the offset negative since the hours will need to be
			 // subtracted from the date.
			 timezoneOffsetHours *= -1;
		 }
	 }
	
	 // Return the Date object calculated from the string.
	 return returnDate;
}

function localToUtc(date){
	return new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate(),
	date.getUTCHours(), date.getUTCMinutes(), date.getUTCSeconds()));
}

function getMonday(date) {
  d = new Date(date);
  var day = d.getDay(),
      diff = d.getDate() - day + (day == 0 ? -6:1); // adjust when day is sunday
  return new Date(d.setDate(diff));
}

function addDays(date, days) {
    var date = new Date(date.valueOf());
    date.setDate(date.getDate() + days);
    return date;
}

function addHours(date, hours) {
	return addMinutes(date, hours * 60);
}

function addMinutes(date, minutes) {
	var date = new Date(date.valueOf() + (1000 * 60 * minutes));
	return date;
}

function daysUntilDate(date) {
	var start = isoConvert(date);
	var currentTimeUTC = new Date();
	var diff = start.getTime() - currentTimeUTC.getTime();
	var days = Math.ceil(diff / (1000 * 3600 * 24));

	switch (days) {
		case 0:
		case 1:
			return start.setHours(0, 0, 0, 0) == currentTimeUTC.setHours(0, 0, 0, 0) ? 0 : 1;

		default:
			return days;
	}
}

function daysBetween(startdate , enddate) {
	var start = isoConvert(startdate).setHours(0, 0, 0, 0);
	var end = isoConvert(enddate).setHours(0, 0, 0, 0);	
	var diff = end - start;
	var days = Math.ceil(diff / (1000 * 3600 * 24));

	return days;
}


module.exports = {
	ordinal,
	dayNames,
	shortDayNames,
	monthNames,
	shortMonthNames,
	isoConvert,
	localToUtc,
	getMonday,
	addDays,
	addMinutes,
	addHours,
	daysUntilDate,
	daysBetween
};