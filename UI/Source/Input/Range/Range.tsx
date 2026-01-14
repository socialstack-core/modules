import Default, { DefaultInputType } from 'UI/Input/Default';
import { useRef } from "react";

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
	const rangeRef = useRef();

	/**
	 * overlays gradient to show filled area from 0-value
	 */
	function updateGradient(rangeValue) {

		if (disableFill) {
			return;
		}

		const percentage = (rangeValue - min) / (max - min) * 100;
		rangeRef.current?.style.setProperty('--percentage', percentage + '%');
	}

	// TODO: preset background gradient based on initial value
	//updateGradient(value);

	let fieldMarkup: React.ReactNode;

	// TODO: ensure ref / onInput are supported
	fieldMarkup = <>
		<Default type="range" config={props} field={field} ref={rangeRef} onInput={(e) => {
			updateGradient(e.target.value);
		}} />
	</>;

	return fieldMarkup;
}

window.inputTypes['range'] = Range;
export default Range;