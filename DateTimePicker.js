// Module import examples - none are required:
import omit from 'UI/Functions/Omit';
import Input from 'UI/Input';
import {monthNames, ordinal, isoConvert} from 'UI/Functions/DateTools';
import DateWrapper from './DateWrapper';
import CustomPicker from './CustomPicker';

var inputTypes = global.inputTypes = global.inputTypes || {};

inputTypes['ontypedatetime-local'] = function(props, _this){
	
	return <DateTimePicker 
		id={props.id || _this.fieldId}
		type='datetime-local'
		className={props.className || "form-control"}
		{...omit(props, ['id', 'className', 'type', 'inline'])}
	/>;
	
};

inputTypes.ontypedate = function(props, _this){
	
	return <DateTimePicker 
		id={props.id || _this.fieldId}
		type='date'
		className={props.className || "form-control"}
		{...omit(props, ['id', 'className', 'type', 'inline'])}
	/>;
	
};


function isValidDate(d) {
	return (d instanceof Date || d instanceof DateWrapper) && !isNaN(d);
}

function dateNow(local) {
	return new DateWrapper(Date.now(), local);
}

function daysInMonth(month, year) {
	return parseInt(new Date(year, parseInt(month)+1, 0).getDate());
}

function roundTime(date, step) {
	if (!step) {
		return date;
	}

	var mins = date.getMinutes();
	var secs = (mins * 60) + date.getSeconds();

	// round up always:
	var roundSecs = Math.ceil(secs/step) * step;
	if(secs !== roundSecs){
		var newMins = Math.floor(roundSecs /60);
		var newSecs = roundSecs % 60;
		date.setMinutes(newMins, newSecs, 0);
	}

	return date;
}

function clamp(val, min, max) {
	return val < min ? min : val > max ? max : val;
}

function clampDay(day, month, year) {
	return clamp(day, 1, daysInMonth(month, year));
}

function getMinDate(props) {
	var { min, minDate, futureOnly, showSeconds, step, local } = props;

	if (minDate) {
		min = new DateWrapper(minDate, local);
		min.setHours(0,0,0,0);
	}

	if (min) {
		min = roundTime(new DateWrapper(min, local), step);
	}

	var nowDate = dateNow(local);
	if (!showSeconds || nowDate.getSeconds() === 1) {
		nowDate.setSeconds(0,0);
	}
	nowDate = roundTime(nowDate, step);

	if (futureOnly === 'date') {
		nowDate.setHours(0,0,0,0);
	}

	if (min && futureOnly) {
		var highestMin = nowDate > min ? nowDate : min;
		return highestMin;
	} else if (min) {
		return new DateWrapper(min, local);
	} else if (futureOnly) {
		return new DateWrapper(nowDate, local);
	}

	return null;
}

function getMaxDate(props) {
	var { max, maxDate, step, local } = props;

	if (maxDate) {
		max = new DateWrapper(maxDate, local);

		if (step && step % 60 === 0) {
			var maxMins = 60 - (step / 60);
			max.setHours(23,maxMins,0,0)
		} else {
			max.setHours(23,59,59,0);
		}

		return max;
	}

	if (max) {
		return roundTime(new DateWrapper(max, local), step);
	}

	return null;
}

function clampDate(date, min, max) {
	var clamped = new DateWrapper(date);
	var wMinDate = min ? new DateWrapper(min) : null;
	var wMaxDate = max ? new DateWrapper(max) : null;

	clamped = clamp(clamped, wMinDate ?? clamped, wMaxDate ?? clamped);
	return clamped;
}

function dateValue(date, props) {
	var { type, showSeconds } = props;

	if (!date) {
		if (type === 'date') {
			return '0000-00-00';
		} else if (!showSeconds) {
			return '0000-00-00T00:00';
		}

		return '0000-00-00T00:00:00';
	}

	var year    = `${date.getFullYear()}`.padStart(2, '0');
	var month   = `${date.getMonth()+1}`.padStart(2, '0');
	var day     = `${date.getDate()}`.padStart(2, '0');
	var hours   = `${date.getHours()}`.padStart(2, '0');
	var minutes = `${date.getMinutes()}`.padStart(2, '0');
	var seconds = `${date.getSeconds()}`.padStart(2, '0');

	if (type === 'date') {
		return `${year}-${month}-${day}`;
	} else if (!showSeconds || date.getSeconds() === 0) {
		return `${year}-${month}-${day}T${hours}:${minutes}`;
	}

	return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
}

function processDate(date, props) {
	var { local, step } = props;

	var minD = getMinDate(props);
	var maxD = getMaxDate(props);
	var processed = new DateWrapper(date);
	processed = clampDate(date, minD, maxD);

	if (date) {
		// Apply the timezone offset if local is false - This keeps the value to UTC time without altering the user input
		var minutes = local ? date.getMinutes() : date.getMinutes() - date.getTimezoneOffset();

		processed.setHours(date.getHours(), minutes, date.getSeconds());
		processed = clampDate(processed, minD, maxD);
	}

	// Only round the time if step is an integer when converted to minutes (The roundTime function doesn't account for the step basis validation otherwise)
	if (props.autoStep && step % 60 === 0) {
		processed = roundTime(processed, step);
	}

	return processed;
}

export default class DateTimePicker extends React.Component {
	
	constructor(props){
		super(props);
		this.state={
			edit: this.props.stayOpen || false
		};

		this.state.date = processDate(this.currentDate(props), props);
		this.inputRef = React.createRef();

		if (props.step && props.step % 60 !== 0) {
			// Seconds must be shown if step isn't an integer when converted to minutes
			props.showSeconds = true;
		}

		// Update the minimum value every minute if the time should be in the future
		if (props.futureOnly && props.futureOnly !== 'date') {
			var now = dateNow(props.local);
			var secondsUntilNextMinute = 60 - now.getSeconds();

			var updateMinValue = () => {
				if (this.inputRef.current) {
					this.inputRef.current.min = dateValue(getMinDate(props), props);
				}
			}

			setTimeout(() => {
				updateMinValue();
				setInterval(updateMinValue, 60000);
			}, secondsUntilNextMinute * 1000);
		}
	}
	
	currentDate(props){
		var {value, defaultValue, local} = props;
		
		if(value !== undefined){
			return value ? new DateWrapper(isoConvert(value), local) : null;
		}
		
		var {date} = this.state;
		
		if(date && isValidDate(date)){
			return date;
		}

		if (date && !isValidDate(date)) {
			return this.lastValidDate ?? new DateWrapper(null, local)
		}

		return defaultValue ? new DateWrapper(isoConvert(defaultValue), local) : new DateWrapper(null, local);
	}

	componentDidUpdate(prevProps, prevState) {
		if (isValidDate(this.state.date)) {
			this.lastValidDate = new DateWrapper(this.state.date, this.props.local);
		}
	}

	renderDateInput(date, props) {
		var { type, local, customPicker, readOnly, step } = props;

		var minValue = dateValue(getMinDate(props), props);
		var maxValue = dateValue(getMaxDate(props), props);
		var currDate = this.currentDate(this.props);

		var inputValue = dateValue(date, props);

		var onClick = (e) => {
			if (customPicker) {
				e.preventDefault();
				this.initialFocus = true;
				this.setState({showCustomPicker: true});
			}
		}

		var onKeyDown = (e) => {
			if (customPicker && (e.key === "Enter" || e.key === " ")) {
				e.preventDefault()
			}
		};

		var onChange = (e) => {
			if (e.target.value === '') {
				var date = processDate(new DateWrapper(null, local), props);
				this.setState({date});
			}
		}

		var onBlur = (e) => {
			var newDate = processDate(new DateWrapper(e.target.value, local), props);

			if (currDate !== newDate) {
				this.setState({date: newDate});
			}
		}

		return <input
			ref={this.inputRef}
			name="datetime"
			className='date-time-picker__internal'
			type={type}
			readonly={readOnly}
			value={inputValue}
			min={minValue}
			max={maxValue}
			step={type === 'date' ? false : step}
			onKeyDown={onKeyDown}
			onClick={onClick}
			onChange={onChange}
			onBlur={onBlur}
		/>;
	}

	renderReadonly(date) {
		return this.renderDateInput(date, {readOnly: true, ...this.props});
	}

	renderEdit(date){
		return <>
			{this.renderDateInput(date, this.props)}
			{this.state.showCustomPicker &&
				<CustomPicker
					date={date} {...this.props}
					onClose={() => {this.setState({showCustomPicker: false})}}
					onDateChange={(date) => this.setState({date})} 
				/>
			}
		</>;
	}

	renderConfirmButton() {

		var confirmOnChange = (e) => {
			e.preventDefault();
			e.stopPropagation();
			this.setState({edit: false});
		}

		return <button className="btn btn-primary btn-change" style={{padding: 0, borderRadius: '50%', fontSize: '12px'}} onClick={confirmOnChange}>
			<i className='fa fa-check' />
		</button>
	}

	renderEditButton() {
		var date = this.currentDate(this.props);

		var editOnChange = (e) => {
			e.preventDefault();
			e.stopPropagation();
			this.setState({edit: true, date: date || new DateWrapper()});
		};

		return <button className="btn btn-primary btn-change" style={{padding: 0, borderRadius: '50%', fontSize: '12px'}} onClick={editOnChange}>
			<i className='fa fa-pencil' />
		</button>
	}

	render(){
		var { type } = this.props;
		var date = this.currentDate(this.props);

		return <div className="date-time-picker">
			{this.state.edit ?
					<>
						{this.renderEdit(date)}
						{!this.props.stayOpen && this.renderConfirmButton()}
					</>
				:
					<>
						{this.renderReadonly(date)}
						{!this.props.stayOpen && this.renderEditButton()}
					</>
			}
			<input type="hidden" name={this.props.name} ref={ref => {
				this.ref = ref;
				if(ref){
					ref.onGetValue = (val, field) => {
						if(field != this.ref){
							return;
						}					
						var date = this.currentDate(this.props);
						if (date && type === 'date') {
							date.setSeconds(0);
							date.setMinutes(0);
							date.setHours(0);
						}
						return date ? date.toISOString() : null;
					}
				}
			}}/>
		</div>;
		
	}
	
}

DateTimePicker.propTypes = {
	stayOpen: 'bool',
	local: 'bool',
	autoStep: 'bool',
	showSeconds: 'bool',
};

DateTimePicker.defaultProps = {
	stayOpen: true,
	local: true,
	autoStep: false,
	showSeconds: false,
};
