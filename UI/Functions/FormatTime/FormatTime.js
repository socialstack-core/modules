import * as dateTools from 'UI/Functions/DateTools';

/*
* Very basic date/time formatting, using a US date or EU date for now format.
*/
const longMonths = [`January`, `February`, `March`, `April`, `May`, `June`, `July`, `August`, `September`, `October`, `November`, `December`];

export function ordinal_suffix_of(i) {
    var j = i % 10,
        k = i % 100;
    if (j == 1 && k != 11) {
        return i + `st`;
    }
    if (j == 2 && k != 12) {
        return i + `nd`;
    }
    if (j == 3 && k != 13) {
        return i + `rd`;
    }
    return i + `th`;
}

export default function FormatTime(date, format, noTime = false, delimiter = null, noDate = false, isHtml = false){
    if(!date || (noDate && noTime)){
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
    evening ? meridiem = `PM` : meridiem = `AM`;
    if (minute < 10){
        minute = "0" + minute;
    }
    if(format == "us") {
        var dateString = "";

        if(!noDate) {
            dateString += month + "/" + day + "/" + year;
        }
        
        if(!noTime) {

            if (isHtml) {
                dateString += "<time>";
            }

            dateString += " " + hour + ":" + minute + " " + meridiem;

            if (isHtml) {
                dateString += "</time>";
            }

        }

        return dateString;
    }
    else if(format == "eu") {
        if(!delimiter) {
            delimiter = "-"
        }

        var dateString = "";

        if(!noDate) {
            dateString += day + delimiter + month + delimiter + year;
        }
        
        if(!noTime) {

            if (isHtml) {
                dateString += "<time>";
            }

            dateString += " " + hour + ":" + minute + " " + meridiem;

            if (isHtml) {
                dateString += "</time>";
            }

        }

        return dateString;
    }
    else if (format == "eu-readable") {
        var dateString = "";

        if(!noDate) {
            dateString += ordinal_suffix_of(day) + " " + longMonths[month - 1] + " " + year;
        }
        
        if(!noTime) {

            if (isHtml) {
                dateString += "<time>";
            }

            dateString += " " + hour + ":" + minute + " " + meridiem;

            if (isHtml) {
                dateString += "</time>";
            }

        }

        return dateString;
    } 
    else {
        // Defaulting to Euro, even though its listed twice.
        var dateString = "";

        if(!noDate) {
            dateString += day + "-" + month + "-" + year
        }
        
        if(!noTime) {

            if (isHtml) {
                dateString += "<time>";
            }

            dateString += " " + hour + ":" + minute + " " + meridiem;

            if (isHtml) {
                dateString += "</time>";
            }

        }

        return dateString;
    }
}