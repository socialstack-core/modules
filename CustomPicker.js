import DateWrapper from './DateWrapper';

var ShortDays = ['DAY', 'Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
var Months = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

function isValidDate(d) {
	return (d instanceof Date || d instanceof DateWrapper) && !isNaN(d);
}

function dateNow(local) {
	return new DateWrapper(Date.now(), local);
}

function createTableHeaderRow(table) {
	var thead = <thead />
	var tr = <tr />
	thead.props.children = [];
	tr.props.children = [];
	thead.props.children.push(tr);
	table.props.children.push(thead);

	return tr;
}

/**
 * Used with DateTimePicker to replace the browser-native date/time picker.
 * 
 * Useful for providing a site-themed picker that is consistent across browsers.
 */

export default class CustomPicker extends React.Component {

	constructor(props){
		super(props);
		this.state={
            showDate: null,
		};

		this.initialFocus = true;
		this.state.date = props.date || new DateWrapper();
		this.activeDayRef = React.createRef();

		this.mouseUp = this.mouseUp.bind(this);
		document.addEventListener('mouseup', this.mouseUp);
	}

	mouseUp(e) {
		clearTimeout(this.mouseUpTimeout);

		this.mouseUpTimeout = setTimeout(() => {
			if (!this.initialFocus && !this.inputFocused && !this.calendarFocused && !this.dayCellFocused) {
				this.props.onClose && this.props.onClose();
			}

			this.initialFocus = false;
		}, 100);
	}

	renderCustomDatePicker(date, props) {
		var { local, futureOnly, minDate, maxDate } = props;

		var rowCount = 6;
		var colCount = 7;
		var wMinDate = minDate ? new DateWrapper(minDate, local) : null;
		var wMaxDate = maxDate ? new DateWrapper(maxDate, local) : null;

		var dateShown = date;
		if (this.state.showDate) {
			dateShown = this.state.showDate;
		}

		var month = parseInt(dateShown.getMonth());
		var year = parseInt(dateShown.getFullYear());

		var tblOnClick = (e) => { this.calendarFocused = true; }
		var tblOnBlur  = (e) => { this.calendarFocused = false; }

		var table = <table tabindex={0} onClick={tblOnClick} onBlur={tblOnBlur} className='calendar-picker' />;
		table.props.children = [];

		var header_row = createTableHeaderRow(table);
		header_row.props.children.push(<td colSpan={colCount} className='bg-primary'>{Months[month] + ' ' + year}</td>);

		var prevBtnOnClick = (e) => {
			var showDate = new DateWrapper(this.state.showDate ?? date);
			showDate.setMonth(month-1);
			this.setState({showDate});
		}

		var nextBtnOnClick = (e) => {
			var showDate = new DateWrapper(this.state.showDate ?? date);
			showDate.setMonth(month+1);
			this.setState({showDate});
		}

		var nav_row = createTableHeaderRow(table);
		nav_row.props.children.push(<td colSpan={Math.floor(colCount/2)} onClick={prevBtnOnClick} className='btn-primary'><i className='fa fa-arrow-left' style={{margin: 0}} /></td>);
		nav_row.props.children.push(<td colSpan={colCount % 2}></td>);
		nav_row.props.children.push(<td colSpan={Math.floor(colCount/2)} onClick={nextBtnOnClick} className='btn-primary'><i className='fa fa-arrow-right' style={{margin: 0}} /></td>);

		var col_header_row = createTableHeaderRow(table);
		for (let hr = 1; hr <= colCount; hr++) {
			col_header_row.props.children.push(<td className='bg-primary'>{ShortDays[hr]}</td>);
		}

		var tbody = <tbody />;
		tbody.props.children = [];
		table.props.children.push(tbody);

		var day = 1;
		var cellDate = new DateWrapper([year, month, day], local);
		var dayZeroMatch = cellDate.getDay() === 0;

		while (!dayZeroMatch && isValidDate(cellDate)) {
			day--;
			cellDate = new DateWrapper([year, month, day], local);
			dayZeroMatch = cellDate.getDay() === 0;
		}

		for (let r = 1; r <= rowCount; r++) {
			var tr = <tr />
			tr.props.children = [];
			tbody.props.children.push(tr);

			for (let c = 0; c < colCount; c++) {
				cellDate = new DateWrapper([year, month, day], local);

				var dateNowStart = dateNow(local);
				dateNowStart.setHours(0, 0, 0, 0);

				var isSelectedMonth = cellDate.getMonth() === month;
				var isSelectedDay = isSelectedMonth && cellDate.getDate() === date.getDate() && cellDate.getMonth() === date.getMonth() && cellDate.getFullYear() === date.getFullYear();
				var isNotSelected = day != date.getDate() || month != date.getMonth() || year != date.getFullYear();

				var futureOnlyFail = (futureOnly && cellDate < dateNowStart);
				var minDateFail = (wMinDate && cellDate < wMinDate);
				var maxDateFail = (wMaxDate && cellDate > wMaxDate);
				var dateInRange = !(futureOnlyFail || minDateFail || maxDateFail);

				var cellClass = isSelectedMonth && dateInRange && !isSelectedDay ? 'btn-primary' : 'bg-secondary disabled';
				cellClass     = isSelectedDay ? 'cal-active' : cellClass;
				cellClass     = isSelectedMonth && !isSelectedDay && !dateInRange ? 'bg-primary cal-dark' : cellClass;

				var dayOnClick = null;
				if (isSelectedMonth && isNotSelected && dateInRange) {
					dayOnClick = (e) => {
						this.calendarFocused = false;
						this.dayCellFocused = true;

						date.setDate(1);
						date.setMonth(month);
						date.setYear(year);
						date.setDate(e.target.innerHTML);

						this.setState({date});
						props.onDateChange && props.onDateChange(date);
					};
				}

				var dayOnBlur = (e) => {
					this.dayCellFocused = false;
					this.calendarFocused = false;
				}

				var dayCell = <td tabindex={0} value={day} onClick={dayOnClick} onBlur={dayOnBlur} className={cellClass}>{cellDate.getDate()}</td>;
				tr.props.children.push(dayCell);

				if (isSelectedDay) {
					dayCell.ref = this.activeDayRef;
				}

				day++;
			}
		}

		return table;
	}

	componentDidUpdate(prevProps, prevState) {
		if (isValidDate(this.state.date)) {
			this.lastValidDate = new DateWrapper(this.state.date);
		}
	}

	componentWillUnmount() {
		document.removeEventListener('mouseup', this.mouseUp);
	}

	render(){
		return this.renderCustomDatePicker(this.state.date, this.props);
	}
	
}

// TODO: Implement a time picker similar to Chrome and Edge
