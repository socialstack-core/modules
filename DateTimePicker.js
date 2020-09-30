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
			edit: this.props.stayOpen || false
		};
		
		this.state.date=this.currentDate(props) || new Date();
		
		if(!zero59){
			setup();
		}
	}
	
	currentDate(props){
		var {value, defaultValue} = props;
		
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
			return <div className = "no-date">No date selected</div>;
		}
		
		var timeStr = this.doubleDigit(date.getUTCHours()) + ':' + 
			this.doubleDigit(date.getUTCMinutes());

		if(!this.props.hideSeconds){
			timeStr += ':' + this.doubleDigit(date.getUTCSeconds());
		}
		
		var dayStr = dateTools.ordinal(date.getUTCDate()) + ' ' + dateTools.monthNames[date.getUTCMonth()] + ' ' + date.getUTCFullYear();
		
		return <span>
			<i className="fa fa-fw fa-clock" />
			<span className="selected-time">{timeStr}</span>
			<i className="fa fa-fw fa-calendar" />
			<span className="selected-date">{dayStr}</span>			
		</span>;
	}
	
	showLocal(date){
		if(!date){
			return <div className = "no-date">No date selected</div>;
		}
		
		var timeStr = this.doubleDigit(date.getHours()) + ':' + 
			this.doubleDigit(date.getMinutes());

		if(!this.props.hideSeconds){
			timeStr += ':' + this.doubleDigit(date.getSeconds());
		}
		
		var dayStr = dateTools.ordinal(date.getDate()) + ' ' + dateTools.monthNames[date.getMonth()] + ' ' + date.getFullYear();
		
		return <span>
			<i className="fa fa-fw fa-clock" />
			<span className="selected-time">{timeStr}</span>
			<i className="fa fa-fw fa-calendar" />
			<span className="selected-date">{dayStr}</span>			
		</span>;
	}
	
	renderSelect(val, set, name, className, onChange){
		
		return <select value={val} onChange={e => {
			var date = this.currentDate(this.props) || new Date();
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
	
	renderEdit(date, local){
		
		return <div className="date-time-picker">
			<span className="time-picker">
				<i className="fa fa-clock"/>
					{this.renderSelect(local ? date.getHours() : date.getUTCHours(), zero23, 'hours', 'small', (v, d) => {
						local ? d.setHours(parseInt(v)) : d.setUTCHours(parseInt(v));
						return d;
					})}
					{this.renderSelect(local ? date.getMinutes() : date.getUTCMinutes(), zero59, 'minutes', 'small', (v, d) => {
						local ? d.setMinutes(parseInt(v)) : d.setUTCMinutes(parseInt(v));
						return d;
					})}
					{!this.props.hideSeconds && this.renderSelect(local ? date.getSeconds() : date.getUTCSeconds(), zero59, 'seconds', 'small', (v, d) => {
						local ? d.setSeconds(parseInt(v)) : d.setUTCSeconds(parseInt(v));
						return d;
					})}
			</span>
			<span className="date-picker">
				<i className="fa fa-calendar"/>
					{this.renderSelect((local ? date.getDate() : date.getUTCDate()) - 1, days, 'days', 'small', (v, d) => {
						local ? d.setDate(parseInt(v)) : d.setUTCDate(parseInt(v) + 1);
						return d;
					})}
					{this.renderSelect(local ? date.getMonth() : date.getUTCMonth(), months, 'months', 'small', (v, d) => {
						local ? d.setMonth(parseInt(v)) : d.setUTCMonth(parseInt(v));
						return d;
					})}
					{this.renderSelect(local ? date.getFullYear() : date.getUTCFullYear(), years, 'years', 'small', (v, d) => {
						local ? d.setFullYear(parseInt(v)) : d.setUTCFullYear(parseInt(v));
						return d;
					})}
			</span>
			{!this.props.stayOpen &&
				<button className="btn btn-success btn-done" onClick={e => {
					e.preventDefault();
					e.stopPropagation();
					this.setState({edit: false});
				}}>
					Update
				</button>
			}
		</div>;
		
	}
	
	render(){
		var date = this.currentDate(this.props);
		var {local} = this.props;
		
		return <div className="date-time-picker">
			{
				this.state.edit ? this.renderEdit(date, local) : (
					<>
						{local ? this.showLocal(date) : this.showUtc(date)} <button className="btn btn-secondary btn-change" onClick={e => {
							e.preventDefault();
							e.stopPropagation();
							this.setState({edit: true, date: date || new Date()});
						}}>
							Change
						</button>
					</>
				)
			}
			<input type="hidden" name={this.props.name} ref={ref => {
				this.ref = ref;
				if(ref){
					ref.onGetValue = (val, field) => {
						if(field != this.ref){
							return;
						}					
						var date = this.currentDate(this.props);
						return date ? date.toISOString() : null;
					}
				}
			}}/>
		</div>;
		
	}
	
}