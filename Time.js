import dateTools from 'UI/Functions/DateTools';

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
	
	dateText(date){
		var monthIndex = date.getMonth();
		var dayIndex = date.getDate();
		return dateTools.ordinal(dayIndex) + " " + dateTools.monthNames[monthIndex] + " " + this.timeOnly(date);
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
			return 'Just now';
		}
		
		var minutes = (seconds / 60);
		
		if(minutes < 60){
			return Math.floor(minutes) + 'm ago';
		}
		
		var hours = minutes / 60;
		
		if(hours < 24){
			return Math.floor(hours) + 'h ago';
		}
		
		var days = hours / 24;
		
		return Math.floor(days) + 'd ago';
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
