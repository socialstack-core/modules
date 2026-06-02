import * as dateTools from 'UI/Functions/DateTools';
import { useState, useRef } from 'react';
import Button from 'UI/Button';

var inputTypes = (globalThis as any).inputTypes = (globalThis as any).inputTypes || {};

inputTypes.ontypedatetime = function ({ id, className, type, inline, ...props }: any, _this: any) {
	return <DateTimePicker
		id={id || _this.fieldId}
		className={className || "form-control"}
		{...props}
	/>;
};

var zero59: number[] | null = null;
var zero23: number[] | null = null;
var days: string[] | null = null;
var months = dateTools.monthNames;
var years: number[] | null = null;

function pad(min: number, max: number, step?: number): number[] {
	if (!step) {
		step = 1;
	}
	var a: number[] = [];
	for (var i = min; i <= max; i += step) {
		a.push(i);
	}
	return a;
}

if (!zero59) {
	// setup
	zero59 = pad(0, 59);
	zero23 = pad(0, 23);
	years = pad(1800, 2200);
	days = [];
	for (var i = 0; i < 31; i++) {
		days.push(dateTools.ordinal((i + 1) as int));
	}
}

interface DateTimePickerProps {
	id?: string;
	className?: string;
	value?: string;
	defaultValue?: string;
	stayOpen?: boolean;
	roundMinutes?: number;
	local?: boolean;
	hideSeconds?: boolean;
	name?: string;
	type?: string;
	inline?: boolean;
}

const DateTimePicker: React.FC<DateTimePickerProps> = (props) => {

	function getCurrentDate(props: DateTimePickerProps, currentDate: Date | null): Date | null {
		if (props.value !== undefined) {
			return props.value ? dateTools.isoConvert(props.value) : null;
		}

		return currentDate || (props.defaultValue ? dateTools.isoConvert(props.defaultValue) : null);
	}

	function doubleDigit(num: number): string {
		if (num < 10) {
			return '0' + num;
		}
		return num.toString();
	}

	const hiddenRef = useRef<any>(null);
	const [edit, setEdit] = useState(props.stayOpen || false);
	const [date, setDate] = useState<Date>(() => {
		var d = getCurrentDate(props, null) || new Date();

		if (props.roundMinutes) {
			var local = props.local;
			var mins = local ? d.getMinutes() : d.getUTCMinutes();
			var roundMins = Math.ceil(mins / props.roundMinutes) * props.roundMinutes;
			if (roundMins != mins) {
				local ? d.setMinutes(roundMins) : d.setUTCMinutes(roundMins);
			}
		}

		return d;
	});

	function showUtc(date: Date | null): React.ReactNode {
		if (!date) {
			return <div className="no-date">No date selected</div>;
		}

		var timeStr = doubleDigit(date.getUTCHours()) + ':' +
			doubleDigit(date.getUTCMinutes());

		if (!props.hideSeconds) {
			timeStr += ':' + doubleDigit(date.getUTCSeconds());
		}

		var dayStr = dateTools.ordinal(date.getUTCDate() as int) + ' ' + dateTools.monthNames[date.getUTCMonth()] + ' ' + date.getUTCFullYear();

		return <span>
			<i className="fa fa-fw fa-clock" />
			<span className="selected-time">{timeStr}</span>
			<i className="fa fa-fw fa-calendar" />
			<span className="selected-date">{dayStr}</span>
		</span>;
	}

	function showLocal(date: Date | null): React.ReactNode {
		if (!date) {
			return <div className="no-date">No date selected</div>;
		}

		var timeStr = doubleDigit(date.getHours()) + ':' +
			doubleDigit(date.getMinutes());

		if (!props.hideSeconds) {
			timeStr += ':' + doubleDigit(date.getSeconds());
		}

		var dayStr = dateTools.ordinal(date.getDate() as int) + ' ' + dateTools.monthNames[date.getMonth()] + ' ' + date.getFullYear();

		return <span>
			<i className="fa fa-fw fa-clock" />
			<span className="selected-time">{timeStr}</span>
			<i className="fa fa-fw fa-calendar" />
			<span className="selected-date">{dayStr}</span>
		</span>;
	}

	function renderSelect(val: number, set: any[], name: string, className: string, onChange: (value: string, date: Date) => Date): React.ReactNode {
		return <select value={val} onChange={(e: React.ChangeEvent<HTMLSelectElement>) => {
			var d = getCurrentDate(props, date) || new Date();
			d = onChange(e.target.value, d);
			setDate(d);
		}} name={props.name + '__' + name}>
			{set.map((v, i) => {
				var num = typeof (v) == 'number';
				return <option value={num ? v : i} key={i}>{
					num ? doubleDigit(v) : v
				}</option>

			})}
		</select>;
	}

	function renderEdit(date: Date, local?: boolean): React.ReactNode {
		return <div className="date-time-picker">
			<span className="time-picker">
				<i className="fa fa-clock" />
				{renderSelect(local ? date.getHours() : date.getUTCHours(), zero23!, 'hours', 'small', (v, d) => {
					local ? d.setHours(parseInt(v)) : d.setUTCHours(parseInt(v));
					return d;
				})}
				{renderSelect(local ? date.getMinutes() : date.getUTCMinutes(), props.roundMinutes ? pad(0, 59, props.roundMinutes) : zero59!, 'minutes', 'small', (v, d) => {
					local ? d.setMinutes(parseInt(v)) : d.setUTCMinutes(parseInt(v));
					return d;
				})}
				{!props.hideSeconds && renderSelect(local ? date.getSeconds() : date.getUTCSeconds(), zero59!, 'seconds', 'small', (v, d) => {
					local ? d.setSeconds(parseInt(v)) : d.setUTCSeconds(parseInt(v));
					return d;
				})}
			</span>
			<span className="date-picker">
				<i className="fa fa-calendar" />
				{renderSelect((local ? date.getDate() : date.getUTCDate()) - 1, days!, 'days', 'small', (v, d) => {
					local ? d.setDate(parseInt(v)) : d.setUTCDate(parseInt(v) + 1);
					return d;
				})}
				{renderSelect(local ? date.getMonth() : date.getUTCMonth(), months, 'months', 'small', (v, d) => {
					local ? d.setMonth(parseInt(v)) : d.setUTCMonth(parseInt(v));
					return d;
				})}
				{renderSelect(local ? date.getFullYear() : date.getUTCFullYear(), years!, 'years', 'small', (v, d) => {
					local ? d.setFullYear(parseInt(v)) : d.setUTCFullYear(parseInt(v));
					return d;
				})}
			</span>
			{!props.stayOpen &&
				<Button variant="success" className="btn-done" onClick={(e: React.MouseEvent) => {
					e.preventDefault();
					e.stopPropagation();
					setEdit(false);
				}}>
					{`Update`}
				</Button>
			}
		</div>;
	}

	var currentDateValue = getCurrentDate(props, date);
	var local = props.local;

	return <div className="date-time-picker">
		{
			edit ? renderEdit(currentDateValue || new Date(), local) : (
				<>
					{local ? showLocal(currentDateValue) : showUtc(currentDateValue)} <Button variant="secondary" className="btn-change" onClick={(e: React.MouseEvent) => {
						e.preventDefault();
						e.stopPropagation();
						setEdit(true);
						setDate(currentDateValue || new Date());
					}}>
						{`Change`}
					</Button>
				</>
			)
		}
		<input type="hidden" name={props.name} ref={ref => {
			hiddenRef.current = ref;
			if (ref) {
				// @ts-ignore
				ref.onGetValue = (val: any, field: any) => {
					if (field != hiddenRef.current) {
						return;
					}
					var d = getCurrentDate(props, date);
					return d ? d.toISOString() : null;
				}
			}
		}} />
	</div>;
};

export default DateTimePicker;
