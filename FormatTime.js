import * as dateTools from 'UI/Functions/DateTools';

/*
* Very basic date/time formatting, using a US date or EU date for now format.
*/
const longMonths = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

export default function FormatTime(date, format, noTime = false, delimiter = null){
    if(!date){
        return '-';
    }
	
	date = dateTools.isoConvert(date);
    var day = date.getDate();
    var year = date.getFullYear();
    var month = date.getMonth() + 1;
    var hour = date.getHours();
    var minute = date.getMinutes();
    var evening = false;

    if(hour >= 12 ){
        evening = true;
        hour -= 12; 
    }
    if(hour == 0){
        hour = 12;
    }

    now = new Date();
    
    var meridiem = "";
    evening ? meridiem = "PM" : meridiem = "AM";
    if (minute < 10){
        minute = "0" + minute;
    }
    if(format == "us") {
        return month + "/" + day + "/" + year + " " + hour + ":" + minute + " " + meridiem;
    }
    else if(format == "eu") {
        if(!delimiter) {
            delimiter = "-"
        }

        var dateString = day + delimiter + month + delimiter + year;
        
        if(!noTime) {
            dateString += " " + hour + ":" + minute + " " + meridiem;
        }

        return dateString;
    }
    else if (format == "eu-readable") {
        return day + " " + longMonths[month - 1] + " " + year;
    } 
    else {
        // Defaulting to Euro, even though its listed twice.
        return day + "-" + month + "-" + year + " " + hour + ":" + minute + " " + meridiem;
    }
}