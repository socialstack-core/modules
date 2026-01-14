import Default, { DefaultInputType } from 'UI/Input/Default';
import { isoConvert } from 'UI/Functions/DateTools';
import { useEffect } from 'react';

function padded(time: number) {
	if (time < 10) {
		return '0' + time;
	}
	return time;
}

function dateFormatStr(d: Date) {
	return d.getFullYear() + '-' + padded(d.getMonth() + 1) + '-' + padded(d.getDate()) + 'T' + padded(d.getHours()) + ':' + padded(d.getMinutes());
}

type DateTimeInputType = DefaultInputType & {
	value?: Date | string;
	defaultValue?: Date | string;
	min?: Date | string;
	max?: Date | string;
	roundMinutes?: number;
}

// Registering 'datetime-local' as being available
declare global {
	interface InputPropsRegistry {
		'datetime-local': DateTimeInputType;
		'datetime': DateTimeInputType;
		'date': DateTimeInputType;
		'time': DateTimeInputType;
	}
}

function toDateString(val?: Date | string) : string | undefined {
	if (!val) {
		return undefined;
	}

	if (typeof val == 'string') {
		return val;
	} else {
		// It's a date
		return dateFormatStr(isoConvert(val));
	}
}

const Datetime: React.FC<CustomInputTypeProps<"datetime-local">> = (props) => {
	const { field } = props;
	const { defaultValue, value, min, max, roundMinutes, ...attribs } = field;

	useEffect(() => {
		const datetimeInput = props.inputRef;

		if (!datetimeInput) {
			return;
		}

		datetimeInput.addEventListener('click', clickHandler);

		return () => {
			datetimeInput.removeEventListener('click', clickHandler);
		};
	}, [props.inputRef]);

	const clickHandler = (e) => {
		// open the native browser picker
		if ('showPicker' in HTMLInputElement.prototype) {
			try {
				e.target.showPicker();
			} catch (error) {
				console.error("Datepicker could not be shown", error);
			}
		}
	};

	let defaultStr = toDateString(defaultValue);
	let valueStr = toDateString(value);
	let minStr = toDateString(min);
	let maxStr = toDateString(max);

	// Add onGetValue for converting the local time into utc
	const onInputRef = (r: HTMLElement) => {
		props.onInputRef && props.onInputRef(r);

		if (r) {
			(r as any).onGetValue = (v: string) => {
				if (v == "") {
					return null;
				}
				return new Date(Date.parse(v));
			};
		}
	};

	let onChange = props.onChange;

	if (roundMinutes && roundMinutes > 0) {
		onChange = (e: React.ChangeEvent) => {
			const ele = e.target as HTMLInputElement;

			var [hours, minutes] = ele.value.slice(-4).split(':');
			var hoursNum = parseInt(hours);
			var minutesNum = parseInt(minutes);

			var time = (hoursNum * 60) + minutesNum;

			var rounded = Math.round(time / roundMinutes) * roundMinutes;

			ele.value = ele.value.slice(0, -4) + Math.floor(rounded / 60) + ':' + padded(rounded % 60);

			props.onChange && props.onChange(e);
		};
	}
	
	return <Default
		type="datetime-local"
		config={
			{
				onChange,
				onInputRef
			} as CustomInputTypePropsBase
		}
		key={field.name}
		field={
			{
				...attribs,
				value: defaultStr || valueStr,
				defaultValue: defaultStr || valueStr,
				min: minStr,
				max: maxStr,
				key: field.name
			} as DefaultInputType
		}
	/>;
};

export default Datetime;

window.inputTypes['datetime-local'] = Datetime;
window.inputTypes['time'] = Datetime;
window.inputTypes['datetime'] = Datetime;