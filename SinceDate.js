/*
Used to calculate the date diff between a passed in date and now.
*/

export default class SinceDate extends React.Component{

    render(){
        var {date} = this.props;
    
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
						intervalType = "hour";
					} else {
						interval = Math.floor(seconds / 60);
						if (interval >= 1) {
						intervalType = "minute";
						} else if(interval > 30){
							interval = seconds;
							intervalType = "second";
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
}