import * as dateTools from 'UI/Functions/DateTools';

/*
* Very basic date/time formatting, using a US date or EU date for now format.
*/
const longMonths = [`January`, `February`, `March`, `April`, `May`, `June`, `July`, `August`, `September`, `October`, `November`, `December`];

export default function FormatTime(dateish : Dateish, format : string, noTime = false, delimiter : string | null = null, noDate = false, isHtml = false){
    if (!dateish || (noDate && noTime)){
        return '-';
    }
	
	var date = dateTools.isoConvert(dateish);
    var day = date.getDate() as int;
    var year = date.getFullYear() as int;
    var month = (date.getMonth() + 1) as int;
    var hour = date.getHours() as int;
    var minute = date.getMinutes() as int;
    var evening = false;

    if(hour >= 12 ){
        evening = true;
        hour = (hour - 12) as int; 
    }
    if(hour == 0){
        hour = 12 as int;
    }

    var now = new Date();
    
    var meridiem = "";
    evening ? meridiem = `PM` : meridiem = `AM`;
    var minuteStr = (minute < 10) ? "0" + minute : minute.toString();
    
    if(format == "us") {
        var dateString = "";

        if(!noDate) {
            dateString += month + "/" + day + "/" + year;
        }
        
        if(!noTime) {

            if (isHtml) {
                dateString += "<time>";
            }

            dateString += " " + hour + ":" + minuteStr + " " + meridiem;

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

            dateString += " " + hour + ":" + minuteStr + " " + meridiem;

            if (isHtml) {
                dateString += "</time>";
            }

        }

        return dateString;
    }
    else if (format == "eu-readable") {
        var dateString = "";

        if(!noDate) {
            dateString += dateTools.ordinal(day) + " " + longMonths[month - 1] + " " + year;
        }
        
        if(!noTime) {

            if (isHtml) {
                dateString += "<time>";
            }

            dateString += " " + hour + ":" + minuteStr + " " + meridiem;

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

            dateString += " " + hour + ":" + minuteStr + " " + meridiem;

            if (isHtml) {
                dateString += "</time>";
            }

        }

        return dateString;
    }
}