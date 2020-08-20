import Row from 'UI/Row';
import Col from 'UI/Column';
import Container from 'UI/Container';
import { localToUtc, addDays, isoConvert, ordinal, shortMonthNames, monthNames } from 'UI/Functions/DateTools';
import Loading from 'UI/Loading';

export default class CalendarCompact extends React.Component {
	
	constructor(props){
		super(props);
		
		var spacing = 30; // In minutes
		var start = 18; // In # of spaces (18 = 9am).
		var end = 36; // 6pm
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
	
	componentDidMount(){
		this.load();
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
	
	load(){
		var { days } = this.props;
		
		if(!days){
			days = 3;
		}
		
		var sliceStart = this.dayStartUtc(0);
		
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
		
		this.setState({currentView: dayMeta});
		
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
	
	minutesToSize(mins){
		return (mins * 5) + 'px';
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
					top: this.minutesToSize(entry.minuteStart - this.state.startMinutes),
					height: this.minutesToSize(entry.minuteEnd - entry.minuteStart)
				}))}
			</div>
		</div>;
		
	}
	
	render(){
		
		var { days, showNav } = this.props;
		
		if(!days){
			days = 3;
		}
		
		var colSize = 12/days;
		
		return <div className="calendar-compact">
			<Container>
				<Row className="calendar-header">
					{this.state.currentView.map((viewInfo,index) => {
						
						return <Col size={colSize} className="calendar-header-col">
							{index == 0 && showNav && <button type="button" className="btn btn-link previous">
									<i className="far fr-chevron-left"></i>
									<span className="sr-only">Previous</span>
							</button>
							}
							<h2 className="calendar-column-title">
								{shortMonthNames[viewInfo.start.getMonth()] + ' ' + viewInfo.start.getDate() + ' ' + viewInfo.start.getFullYear()}
							</h2>
							{index == this.state.currentView.length - 1 && showNav && <button type="button" className="btn btn-link next">
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

CalendarCompact.propTypes = {
};

CalendarCompact.defaultProps = {
};