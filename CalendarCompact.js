import Row from 'UI/Row';
import Col from 'UI/Column';
import Container from 'UI/Container';
import { daysBetween, localToUtc, addDays, isoConvert, ordinal, shortMonthNames, monthNames, addMinutes } from 'UI/Functions/DateTools';
import Loading from 'UI/Loading';
import { useSession, SessionConsumer} from 'UI/Session';
import webRequest from 'UI/Functions/WebRequest';

export default class CalendarCompact extends React.Component {
	
	constructor(props){
		super(props);

		// in minutes
		var spacing = this.props.spacing || 30;

		var start = this.props.startTime === undefined ? 9 : this.props.startTime; // 9AM
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

		// Let's get the current time in minutes after midnight
		var now = new Date();
		var nowMinutes = this.getMinutesSinceMidnight(now);
		var nowRelativeToStart = nowMinutes - (start * 60);

		if(nowRelativeToStart < 0){
			nowRelativeToStart = 0;
		}

		var domNodeIndex = Math.floor(nowRelativeToStart / spacing);

		this.state = {
			heightMinutes: ( end - start ) * spacing,
			startMinutes: start * spacing,
			currentView: [],
			spaces,
			domNodeIndex
		};
		
		// mobile is rendered as a single day (central column)
		var html = document.getElementsByTagName("html");
		
		if (html.length && html[0].classList.contains("device-mobile")) {
			this.state.mobile = true;
		}
		
		this.calendarRef = React.createRef();
	}

	getMinutesSinceMidnight(d){
		var e = new Date(d);
		return (d - e.setHours(0,0,0,0)) / 1000 / 60;
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
		this.load(0, this.props);
	}
	
	dayStartUtc(offset, props){
		// Local day start:
		if(props.date){
			return localToUtc(props.date);
		}
		
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

	load(offset, props){
		var { days } = props;
		
		if(!days){
			days = 3;
		}
		
		if(this.state.mobile){
			days = 1;
		}
		
		var sliceStart = this.dayStartUtc(offset, props);
		
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

		// Request for section:
		this.populateBetween(sliceStart, sliceEnd, dayMeta);
	}
	
	componentWillReceiveProps(props){
		if(props.date && props.date != this.props.date){
			this.load(0, props);
		}
		
	}
	
	componentDidUpdate(p){
		var {domNodeIndex} = this.state;

		if(this.calendarRef.current){
			this.calendarRef.current.childNodes[domNodeIndex].scrollIntoView(true);
		}
	}

	populateBetween(start, end, dayMeta){
		var {dataHandlers} = this.props;

		var dataRequests = dataHandlers.map((handler) =>
			webRequest(handler.type + "/list", { where: (handler.onGetFilter && handler.onGetFilter(start, end)) || {} }, { includes: handler.includes }).then((response) => {
				handler.onFixEntries && handler.onFixEntries(response.json.results);
				return response;
			})
		);

		Promise.all(dataRequests).then(responses => {
			var rsps = [];
			responses.forEach(response => {
				rsps = rsps.concat(response.json.results);
			});
			this.build(rsps, dayMeta);
		});
		


		/*
		this.props.onGetData && this.props.onGetData(start, end).then(response => {
			
			if(response.json){
				// Can either give us an array or a raw API response.
				response = response.json.results;
			}
			
			// Test:
			// response=this.rand();
			
			this.build(response, dayMeta);
		});*/
	}
	
	/*
	rand(){
		var set = [];
		var randCount = Math.floor((Math.random() * 30))+1;
		
		for(var i=0;i<randCount;i++){
			
			var randHour = Math.floor((Math.random() * 22));
			var randMins = Math.floor((Math.random() * 30));
			var randDur = Math.floor((Math.random() * 120));
			
			var start = new Date();
			start.setHours(randHour);
			start.setMinutes(randMins);
			var end = addMinutes(start, randDur);
			
			set.push({
				startUtc: start.toISOString(),
				endUtc: end.toISOString(),
				content: {
					type: 'Video',
					name: 'Test'
				}
			});
		}
		return set;
	}
	*/
	
	build(response, dayMeta){
		for(var i=0;i<dayMeta.length;i++){
			dayMeta[i].results = [];
		}
		
		// Handle the sorting of results into each day now:
		for(var i=0;i<response.length;i++){
			
			var entry = response[i];
			entry.startUtc = isoConvert(entry.startUtc);
			entry.endUtc = isoConvert(entry.endUtc);
			entry.startTimeUtc = isoConvert(entry.startTimeUtc);
			entry.estimatedEndTimeUtc = isoConvert(entry.estimatedEndTimeUtc);
			
			// Start time:
			var startUtc = entry.startUtc;
			
			for(var d=0;d<dayMeta.length;d++){
				// Is startUtc in between this days start/ ends?
				var day = dayMeta[d];
				

				if((day.start <= startUtc && day.end >= startUtc) || (day.start <= entry.startTimeUtc && day.end >= entry.startTimeUtc)){
					entry.minuteStart = Math.floor(((startUtc - day.start)/1000)/60);
					entry.minuteEnd = Math.floor(((entry.endUtc - day.start)/1000)/60);
					day.results.push(entry);
					break;
				}
			}
			
		}
		
		// Detect collisions in the results set
		for(var d=0;d<dayMeta.length;d++){
			var day = dayMeta[d];
			if(this.props.handleCollisions){
				this.organiseEntries(day);
			}else{
				day.results.sort((a,b) => (a.startUtc > b.startUtc) ? 1 : ((b.startUtc > a.startUtc) ? -1 : 0));
			}
		}

		this.setState({currentView: this.state.currentView});
	}
	
	/*
	* Returns set of things that time collided with entry from the given entry set.
	*/
	checkIfCollides(entry, entrySet){
		for(var i=0;i<entrySet.length;i++){
			
			var checkWith = entrySet[i];
			
			if(checkWith.startUtc < entry.endUtc && checkWith.endUtc > entry.startUtc){
				return true;
			}
			
		}
		
		return false;
	}
	
	collisionIteration(set, colIndex){
		var next = null;
		var output = [];
		
		// Add unless there is a collision:
		for(var i=0;i<set.length;i++){
			var entry = set[i];
			if(!entry){
				continue;
			}
			entry._column = colIndex; // col index
			
			if(this.checkIfCollides(entry, output)){
				// This entry collides - add to set of things to resolve in the second+ iteration.
				if(!next){
					next=[entry];
				}else{
					next.push(entry);
				}
			}else{
				// Doesn't collide - add as-is:
				output.push(entry);
			}
		}
		
		return {
			output,
			next
		};
	}
	
	organiseEntries(day) {
		var col = 0;
		var iter = this.collisionIteration(day.results, 0);
		var {output} = iter;
		
		while(iter.next){
			iter = this.collisionIteration(iter.next, ++col);
			output = output.concat(iter.output);
		}
		
		var width = 1/(col+1);
		
		// Finally build computed style:
		for(var i=0;i<output.length;i++){
			var entry = output[i];
			
			entry._computedStyle = {
				// NB: 14px offset is based on h3.time being set to 28px tall - this gets events lining up
				top: this.minutesToSize(entry.minuteStart - this.state.startMinutes, 14 - 1),
				// extra pixel to cover start / end lines
				height: this.minutesToSize(entry.minuteEnd - entry.minuteStart, 1),
				width: entry._column == 0 ? 'calc(' + (width * 100) + '% - 3.5rem)' : (width * 100) + '%',
				left: entry._column == 0 ? 'calc(' + ((entry._column/(col+1)) * 100) + '% + 3.5rem)' : ((entry._column/(col+1)) * 100) + '%',
			};
			
		}
		
		day.results = output;
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
			<div className="times" ref = {this.calendarRef}>
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
				{sortedEntries.map(entry => this.props.onRenderEntry(entry, entry._computedStyle))}
			</div>
		</div>;
		
	}
	
	render(){
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
				<Row className="calendar-body" >
					{this.state.currentView.map((viewInfo, index) => {
						var colClass = "calendar-body-col";

						if (index > 0) {
							colClass += ' bordered';
						}
						
						return <Col size={colSize} className={colClass} ref = {ref => this.calendarRef = ref}>
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