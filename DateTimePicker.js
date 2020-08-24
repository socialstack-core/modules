// Module import examples - none are required:
import omit from 'UI/Functions/Omit';
import dateTools from 'UI/Functions/DateTools';
var eventHandler = global.events.get('UI/Input');

eventHandler.ontypedatetime = function(props, _this){
	
	return <DateTimePicker 
		id={props.id || _this.fieldId}
		className={props.className || "form-control"}
		{...omit(props, ['id', 'className', 'type', 'inline'])}
	/>;
	
};

var zero59 = null;
var zero23 = null;
var days = null;
var months = dateTools.monthNames;
var years = null;

function setup(){
	zero59 = pad(0,59);
	zero23 = pad(0,23);
	years = pad(1800, 2200);
	days = [];
	for(var i=0;i<31;i++){
		days.push(dateTools.ordinal(i+1));
	}
}

function pad(min,max){
	var a = [];
	for(var i=min;i<=max;i++){
		a.push(i);
	}
	return a;
}

export default class DateTimePicker extends React.Component {
	
	constructor(props){
		super(props);
		this.state={
			edit: false
		};
		
		if(!zero59){
			setup();
		}
	}
	
	currentDate(){
		var {value, defaultValue} = this.props;
		
		if(value !== undefined){
			return value ? dateTools.isoConvert(value) : null;
		}
		
		var {date} = this.state;
		
		if(date){
			return date;
		}
		
		return defaultValue ? dateTools.isoConvert(defaultValue) : null;
	}
	
	doubleDigit(num){
		// Min of 2 digits (year also passes through here)
		if(num < 10){
			return '0' + num;
		}
		
		return num.toString();
	}
	
	showUtc(date){
		if(!date){
			return 'No date selected';
		}
		
		var timeStr = this.doubleDigit(date.getUTCHours()) + ':' + 
			this.doubleDigit(date.getUTCMinutes()) + ':' + 
			this.doubleDigit(date.getUTCSeconds());
		
		var dayStr = dateTools.ordinal(date.getUTCDate()) + ' ' + dateTools.monthNames[date.getUTCMonth()] + ' ' + date.getUTCFullYear();
		
		return <span>
			<i className="fa fa-clock"/>
			{timeStr}
			<i className="fa fa-calendar"/>
			{dayStr}
		</span>;
	}
	
	renderSelect(val, set, name, className, onChange){
		
		return <select value={val} onChange={e => {
			var date = this.currentDate() || new Date();
			date = onChange(e.target.value, date);
			this.setState({date});
		}} name={this.props.name + '__' + name}>
			{set.map((v, i) => {
				var num = typeof(v) == 'number';
				return <option value={num ? v : i}>{
					num ?  this.doubleDigit(v) : v
				}</option>
				
			})}
		</select>
		
	}
	
	renderEdit(date){
		return <div className="date-time-picker">
			<i className="fa fa-clock"/>
				{this.renderSelect(date.getUTCHours(), zero23, 'hours', 'small', (v, d) => {
					d.setUTCHours(parseInt(v));
					return d;
				})}
				{this.renderSelect(date.getUTCMinutes(), zero59, 'minutes', 'small', (v, d) => {
					d.setUTCMinutes(parseInt(v));
					return d;
				})}
				{this.renderSelect(date.getUTCSeconds(), zero59, 'seconds', 'small', (v, d) => {
					d.setUTCSeconds(parseInt(v));
					return d;
				})}
			<i className="fa fa-calendar"/>
				{this.renderSelect(date.getUTCDate() - 1, days, 'days', 'small', (v, d) => {
					d.setUTCDate(parseInt(v) + 1);
					return d;
				})}
				{this.renderSelect(date.getUTCMonth(), months, 'months', 'small', (v, d) => {
					d.setUTCMonth(parseInt(v));
					return d;
				})}
				{this.renderSelect(date.getUTCFullYear(), years, 'years', 'small', (v, d) => {
					d.setUTCFullYear(parseInt(v));
					return d;
				})}
			<button className="btn btn-secondary" onClick={e => {
				e.preventDefault();
				e.stopPropagation();
				this.setState({edit: false});
			}}>
				Done
			</button>
		</div>;
		
	}
	
	render(){
		var date = this.currentDate();
		
		if(this.state.edit){
			return this.renderEdit(date);
		}
		
		return <div className="date-time-picker">
			{this.showUtc(date)} <button className="btn btn-secondary" onClick={e => {
				e.preventDefault();
				e.stopPropagation();
				this.setState({edit: true, date: date || new Date()});
			}}>
				Change
			</button>
			<input type="hidden" name={this.props.name} ref={ref => {
				this.ref = ref;
				if(ref){
					ref.onGetValue = (val, field) => {
						if(field != this.ref){
							return;
						}					
						var date = this.currentDate();
						return date ? date.toISOString() : null;
					}
				}
			}}/>
		</div>;
		
	}
	
}