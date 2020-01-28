export default function sinceDate(date){
    if(!date){
        return '-';
    }

    if (typeof date !== 'object') {
        if(!date.endsWith('Z')){
            date += 'Z';
        }
        date = new Date(date);
    }
    
    var seconds = Math.floor((new Date() - date) / 1000);
    var intervalType;

    var interval = Math.floor(seconds / 31536000);
    if (interval >= 1) {
        intervalType = 'year';
    } else {
        interval = Math.floor(seconds / 2592000);
        if (interval >= 1) {
            intervalType = 'month';
        } else {
            interval = Math.floor(seconds / 86400);
            if (interval >= 1) {
            intervalType = 'day';
            } else {
                interval = Math.floor(seconds / 3600);
                if (interval >= 1) {
                    intervalType = "hr";
                } else {
                    interval = Math.floor(seconds / 60);
                    if (interval >= 1) {
                    intervalType = "min";
                    } else if(interval > 30){
                        interval = seconds;
                        intervalType = "sec";
                    }else{
                        return 'just now';
                    }
                }
            }
        }
    }
    
    if (interval != 1) {
        intervalType += 's';
    }

    return interval + ' ' + intervalType + " ago"; 

}