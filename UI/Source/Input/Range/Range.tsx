import Default, { DefaultInputType } from 'UI/Input/Default';
import { useState } from "react";

type RangeInputType = DefaultInputType & {
	/** 
	 * minimum value (defaults to 1)
	 */
	min?: number,

	/** 
	 * maximum value (defaults to 100)
	 */
	max?: number,

	/**
	 * granularity (defaults to 5)
	 */
	step?: number,

	/**
	 * disable filled background (required when rendering 2 range controls as a dual range)
	 */
	disableFill?: boolean
};

// Registering 'range' as being available
declare global {
	interface InputPropsRegistry {
		'range': RangeInputType;
	}
}

const Range: React.FC<CustomInputTypeProps<"range">> = (props) => {
	const { field, helpFieldId, onInputRef, inputRef, validationFailure } = props;
	const { min, max, step, disableFill, onChange, className, ...attribs } = field;
	const [rangeRef, setRangeRef] = useState<HTMLInputElement | null>(null);

	/**
	 * overlays gradient to show filled area from 0-value
	 */
	function updateGradient(rangeValue: string) {
		const value = parseInt(rangeValue, 10);

		if (disableFill || isNaN(value)) {
			return;
		}

		const maxValue: number = !max ? 100 : max;
		const minValue: number = !min ? 1 : min;

		const percentage = (value - minValue) / (maxValue - minValue) * 100;
		rangeRef?.style.setProperty('--percentage', percentage + '%');
	}

	// TODO: preset background gradient based on initial value
	// useEffect(() => { updateGradient(value); }, []);

	let fieldMarkup: React.ReactNode;

	fieldMarkup = <>
		<Default type="range" config={{
			...props,
			onInputRef: (el: HTMLElement) => {
				setRangeRef(el as HTMLInputElement);
			}
		}} field={{
			...field,
			onInput: (e: React.InputEvent<HTMLInputElement>) => {
				updateGradient(e.currentTarget.value);
			}
		}} />
	</>;

	return fieldMarkup;
}

window.inputTypes['range'] = Range;
export default Range;