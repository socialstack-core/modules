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
	return d.getFullYear() + '-' + padded(d.getMonth() + 1) + '-' + padded(d.getDate());
}

type DateInputType = DefaultInputType & {
	value?: Date | string;
	defaultValue?: Date | string;
	min?: Date | string;
	max?: Date | string;
}

// Registering 'date' as being available
declare global {
	interface InputPropsRegistry {
		'date': DateInputType;
	}
}

function toDateString(val?: Date | string): string | undefined {
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

const Date: React.FC<CustomInputTypeProps<"date">> = (props) => {
	const { field } = props;
	const { defaultValue, value, min, max, ...attribs } = field;

	useEffect(() => {
		const dateInput = props.inputRef;

		if (!dateInput) {
			return;
		}

		dateInput.addEventListener('click', clickHandler);

		return () => {
			dateInput.removeEventListener('click', clickHandler);
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

	return <Default
		type="date"
		config={
			{
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

export default Date;

window.inputTypes['date'] = Date;