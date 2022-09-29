import * as dateTools from 'UI/Functions/DateTools';

/*
Displays "x ago" phrase, or just an absolute date/time.
*/

export default class Time extends React.Component {
	
	constructor(props){
		super(props);
		
		this.state = {
			agoTime: this.timeAgoString(this.props.date)
		};
	}
	
	componentWillReceiveProps(props){
		this.update(props);
	}
	
	componentDidMount(){
		if(this.interval){
			return;
		}
		
		this.interval = setInterval(() => this.update(this.props), (this.props.updateRate || 10) * 1000);
	}
	
	timeOnly(date){
		
		var hours = date.getHours();
		var mins = date.getMinutes();
		
		if(mins < 10){
			mins = '0' + mins;
		}
		
		if(hours < 10){
			hours = '0' + hours;
		}
		
		return hours + ':' + mins;
	}

    isToday(date) {
        return new Date(date).setHours(0, 0, 0, 0) == new Date().setHours(0, 0, 0, 0);
    }

    isThisWeek(date) {
		var now = new Date();
		var start = dateTools.addDays(now, -7);
        return (date <= now && date >= start);
    }

	dateText(date){
		var monthIndex = date.getMonth();
        var dayIndex = date.getDate();
		
		var dayStr;
		var { compact, shortDay, compactDayOnly } = this.props;
		
		var fullYear = date.getFullYear();
		var nowYear = new Date().getFullYear();
		var yearText = "";
		
		if(nowYear != fullYear){
			yearText = " " + fullYear;
		}
		
        if (compact) {
			
            if (this.isToday(date)) {
                return this.timeOnly(date);
            }

            if (this.isThisWeek(date)) {
                var dayIndex = date.getDay();
				
				dayStr = shortDay ? dateTools.shortDayNames[dayIndex] : dateTools.dayNames[dayIndex];
				
				if(!compactDayOnly){
					dayStr += " " + this.timeOnly(date);
				}
				
				return dayStr;
            }

        }
		
		dayStr = dateTools.ordinal(dayIndex) + " " + dateTools.monthNames[monthIndex] + yearText;
		
		if(compact && compactDayOnly){
			return dayStr;
		}
		
		return dayStr + " " + this.timeOnly(date);
	}
	
	timeAgoString(dateish){
		if(!dateish){
			return '';
		}
		
		var date = dateTools.isoConvert(dateish);

		if(!date){
			return '';
		}
		
		if(this.props.absolute){
			if(this.props.withDate){
				return this.dateText(date);
			}
			
			return this.timeOnly(date);
		}
		
		var seconds = (Date.now() - date.getTime()) / 1000;
		
		if(seconds < 60){
			return `Just now`;
		}
		
		var minutes = (seconds / 60);
		
		if(minutes < 60){
			var flooredMinutes = Math.floor(minutes);
			return `${flooredMinutes} m ago`;
		}
		
		var hours = minutes / 60;
		
		if(hours < 24){
			var flooredHours = Math.floor(hours);
			return `${flooredHours} h ago`;
		}
		
		var days = hours / 24;
		var flooredDays = Math.floor(days);
		return `${flooredDays} d ago`;
	}
	
	update(props){
		var agoTime = this.timeAgoString(props.date);
		
		this.setState({
			agoTime
		});
	}
	
	componentWillUnmount(){
		if(this.interval){
			clearInterval(this.interval);
			this.interval = null;
		}
	}
	
	render() {
		const { agoTime } = this.state;
		var isoString = false;
		var title = false;

		if (this.props.date) {
			var date = dateTools.isoConvert(this.props.date);

			if (date) {
				isoString = date.toISOString();
				title = this.dateText(date);
			}
		}

		return <time className={this.props.className} title={title} datetime={isoString}>
			{agoTime}
		</time>;
	}	
	
}
