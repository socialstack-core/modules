/*
* Very basic date/time formatting, using a US date or EU date for now format.
*/
export default function FormatTime(date, format){
    if(!date){
        return '-';
    }

    if (typeof date !== 'object') {
        date = new Date(date);
    }

    var day = date.getUTCDate();
    var year = date.getUTCFullYear();
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
        return day + "-" + month + "-" + year + " " + hour + ":" + minute + " " + meridiem;
    }
    else {
        // Defaulting to Euro, even though its listed twice.
        return day + "-" + month + "-" + year + " " + hour + ":" + minute + " " + meridiem;
    }
}