import Row from 'UI/Row';
import Col from 'UI/Column';
import Container from 'UI/Container';
import { daysBetween, localToUtc, addDays, isoConvert, ordinal, shortMonthNames, monthNames } from 'UI/Functions/DateTools';
import Loading from 'UI/Loading';

export default class CalendarCompact extends React.Component {
	
	constructor(props){
		super(props);

		// in minutes
		var spacing = this.props.spacing || 30;

		var start = this.props.startTime || 9; // 9AM
		start = (start * 60) / spacing;

		var end = this.props.endTime || 18;  // 6PM
		end = (end * 60) / spacing;

		var spaces = [];
		
		for(var i=start;i<=end;i++){
			spaces.push({
				id: i,
				minuteStart: i * spacing, // After midnight
				minuteEnd: i * spacing + spacing,
				time: this.blockTime(spacing, i)
			});
		}
		
		this.state = {
			heightMinutes: ( end - start ) * spacing,
			startMinutes: start * spacing,
			currentView: [],
			spaces
		};
		
		// mobile is rendered as a single day (central column)
		var html = document.getElementsByTagName("html");
		
		if (html.length && html[0].classList.contains("device-mobile")) {
			this.state.mobile = true;
		}
		
		
	}
	
	blockTime(spacing, blockId){
		
		var minutes = blockId * spacing;
		var hours = Math.floor(minutes / 60);
		minutes -= hours * 60;
		
		var minuteStr = minutes.toString();
		
		if(minutes < 10){
			minuteStr = '0' + minuteStr;
		}
		
		return hours + ':' + minuteStr;
	}
	
	isToday(date) {
		var today = new Date().setHours(0,0,0,0);
		var timestamp = new Date(date).setHours(0,0,0,0);
		
		return today == timestamp;
	}
	
	componentDidMount(){
		this.load(0);
	}
	
	/*
	componentWillReceiveProps(props){
		this.load(this.props):
	}
	*/
	
	dayStartUtc(offset){
		// Local day start:
		var start = new Date();
		if(offset){
			start = addDays(start, offset);
		}
		start.setHours(0,0,0,0);
		
		// Conversion to UTC:
		return localToUtc(start);
	}
	
	updateOffset(adj) {
		var {offset} = this.state;
		
		if (!offset){
			offset = 0;
		}
		
		offset = offset + adj;
		this.load(offset);
	}
	
	load(offset){
		var { days } = this.props;
		
		if(!days){
			days = 3;
		}
		
		if(this.state.mobile){
			days = 1;
		}
		
		var sliceStart = this.dayStartUtc(offset);
		
		// The timeslice goes from sliceStart -> sliceStart + num of visible days:
		var sliceEnd = addDays(sliceStart, days);
		
		var dayMeta = [];
		
		for(var i=0;i<days;i++){
			dayMeta.push({
				offset: i,
				start: addDays(sliceStart, i),
				end: addDays(sliceStart, i+1),
				results: null // Not loaded yet
			});
		}
		
		this.setState({currentView: dayMeta, offset: offset});
		global.app.setState({forceCalendarRefresh : null});
		
		// Request for section:
		this.populateBetween(sliceStart, sliceEnd, dayMeta);
	}
	
	populateBetween(start, end, dayMeta){
		this.props.onGetData && this.props.onGetData(start, end).then(response => {
			if(response.json){
				// Can either give us an array or a raw API response.
				response = response.json.results;
			}
			
			for(var i=0;i<dayMeta.length;i++){
				dayMeta[i].results = [];
			}
			
			// Handle the sorting of results into each day now:
			for(var i=0;i<response.length;i++){
				
				var entry = response[i];
				entry.startUtc = isoConvert(entry.startUtc);
				entry.endUtc = isoConvert(entry.endUtc);
				
				// Start time:
				var startUtc = entry.startUtc;
				
				for(var d=0;d<dayMeta.length;d++){
					// Is startUtc in between this days start/ ends?
					var day = dayMeta[d];
					
					if(day.start <= startUtc && day.end >= startUtc){
						entry.minuteStart = Math.floor(((startUtc - day.start)/1000)/60);
						entry.minuteEnd = Math.floor(((entry.endUtc - day.start)/1000)/60);
						day.results.push(entry);
						break;
					}
				}
				
			}
			
			// Sort each day by ascending start time.
			for(var d=0;d<dayMeta.length;d++){
				var day = dayMeta[d];
				day.results.sort((a,b) => (a.startUtc > b.startUtc) ? 1 : ((b.startUtc > a.startUtc) ? -1 : 0));
			}
			
			this.setState({currentView: this.state.currentView});
		});
	}
	
	minutesToSize(mins, offset) {
		var { verticalScale } = this.props;

		if (!verticalScale) {
			verticalScale = 3.33;
		}

		if (!offset) {
			offset = 0;
		}

		// 5px; equates to 150px per 30min
		// 3.33px (default) should get us to the 100px per 30min shown in the original design
		return ((mins * verticalScale) + offset) + 'px';
	}
	
	renderDay(sortedEntries){
		
		return <div className="day">
			<div className="times">
				{this.state.spaces.map(spaceInfo => {
					
					return <div className="time-info" style={{
							height: this.minutesToSize(spaceInfo.minuteEnd - spaceInfo.minuteStart)
					}}>
						<h3 className="time">
							{spaceInfo.time}
						</h3>
					</div>
					
				})}
			</div>
			<div className="bookings">
				{sortedEntries.map(entry => this.props.onRenderEntry(entry, {
					// NB: 14px offset is based on h3.time being set to 28px tall - this gets events lining up
					top: this.minutesToSize(entry.minuteStart - this.state.startMinutes, 14 - 1),
					// extra pixel to cover start / end lines
					height: this.minutesToSize(entry.minuteEnd - entry.minuteStart, 1)
				}))}
			</div>
		</div>;
		
	}
	
	render(){
		
		// if we have added a new entry force calendar to redraw and focus
		if (global.app.state.forceCalendarRefresh)
		{
			var newOffset = daysBetween(new Date() , global.app.state.forceCalendarRefresh);
			this.load(newOffset);
		}
		
		var { days , showNav , showToday } = this.props;
		
		if(!days){
			days = 3;
		}
		
		var { mobile } = this.state;
		
		if(mobile){
			days = 1;
		}
		
		var colSize = 12/days;
		
		return <div className="calendar-compact">
			<Container>
				<Row className="calendar-header">
					{this.state.currentView.map((viewInfo,index) => {
						
						return <Col sizeXs={12} sizeMd={colSize} className={"calendar-header-col col-" + index}>

							{/* previous */}
							{((!mobile && index == 0 && showNav) || (mobile && showNav)) && 
							<button type="button" className="btn btn-link previous" 
								onClick={e => {
									e.stopPropagation();
									e.preventDefault();
									this.updateOffset(-1);
								}}>
									<i className="far fr-chevron-left"></i>
									<span className="sr-only">Previous</span>
							</button>
							}

							<h2 className="calendar-column-title">
							{showToday && this.isToday(viewInfo.start) ?
								<>Today</> : 
								<>{shortMonthNames[viewInfo.start.getMonth()] + ' ' + viewInfo.start.getDate() + ' ' + viewInfo.start.getFullYear()}</>
							}
							</h2>

							{/* next */}
							{((!mobile && index == this.state.currentView.length - 1 && showNav) || (mobile && showNav)) &&
							<button type="button" className="btn btn-link next"
								onClick={e => {
									e.stopPropagation();
									e.preventDefault();
									this.updateOffset(1);
								}}>
									<i className="far fr-chevron-right"></i>
									<span className="sr-only">Next</span>
								</button>
							}
						</Col>;
						
					})}
				</Row>
				<Row className="calendar-body">
					{this.state.currentView.map((viewInfo, index) => {
						var colClass = "calendar-body-col";

						if (index > 0) {
							colClass += ' bordered';
						}
						
						return <Col size={colSize} className={colClass}>
							{viewInfo.results ? this.renderDay(viewInfo.results) : <Loading />}
						</Col>;
						
					})}
				</Row>
			</Container>
		</div>;
		
	}
	
}

var timeOptions = [
	{ name: '12:00 AM', value: 0 },
	{ name: '1:00 AM', value: 2 },
	{ name: '2:00 AM', value: 4 },
	{ name: '3:00 AM', value: 6 },
	{ name: '4:00 AM', value: 8 },
	{ name: '5:00 AM', value: 10 },
	{ name: '6:00 AM', value: 12 },
	{ name: '7:00 AM', value: 14 },
	{ name: '8:00 AM', value: 16 },
	{ name: '9:00 AM', value: 18 },
	{ name: '10:00 AM', value: 20 },
	{ name: '11:00 AM', value: 22 },
	{ name: '12:00 PM', value: 24 },
	{ name: '1:00 PM', value: 26 },
	{ name: '2:00 PM', value: 28 },
	{ name: '3:00 PM', value: 30 },
	{ name: '4:00 PM', value: 32 },
	{ name: '5:00 PM', value: 34 },
	{ name: '6:00 PM', value: 36 },
	{ name: '7:00 PM', value: 38 },
	{ name: '8:00 PM', value: 40 },
	{ name: '9:00 PM', value: 42 },
	{ name: '10:00 PM', value: 44 },
	{ name: '11:00 PM', value: 46 }
];

CalendarCompact.propTypes = {
	verticalScale: { type: "int" },
	spacing: [
		{ name: '15 minutes', value: 15 },
		{ name: '30 minutes', value: 30 },
		{ name: '1 hour', value: 60 },
	],
	startTime: timeOptions,
	endTime: timeOptions
};

CalendarCompact.defaultProps = {
	verticalScale: 3.33,
	spacing: 30,
	startTime: 9, // 9AM
	endTime: 18 // 6PM
};