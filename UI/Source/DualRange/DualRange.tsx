import {useEffect, useId, useState } from "react";
import Button from 'UI/Button';

/**
 * Props for the DualRange component.
 */
interface DualRangeProps {
	label: string,
	min?: number,
	max?: number,
	steps?: number,
	defaultFrom?: number,
	defaultTo?: number,
	numberFormat?: Intl.NumberFormat,

	/**
	 * set true to update values as drag handles are moved
	 * set false to show an additional update button (as per Amazon)
	 */
	live?: boolean,

	/**
	 * function to call when values have changed
	 * @param min
	 * @param max
	 * @returns
	 */
	onChange: (min: number, max: number) => void,

	/**
	 * label used for update button (only shown if live = false); defaults to "Go"
	 * NB: keep this text short
	 */
	updateLabel?: string,

	/**
	 * label used for reset price range button; defaults to "Reset price range"
	 */
	resetLabel?: string,

	/**
	 * set true to ensure handles never pass the left/right edges of the range
	 */
	aligned?: boolean
}

const DEFAULT_MIN_RANGE = 0;
const DEFAULT_MAX_RANGE = 100;
const DEFAULT_STEPS = 20;

/**
 * The DualRange React component.
 * @param props React props.
 */
const DualRange: React.FC<DualRangeProps> = (props) => {
	const { label, min, max, defaultFrom, defaultTo, numberFormat, live, onChange, aligned } = props;
	const updateLabel = props.updateLabel?.length ? props.updateLabel : `Go`;
	const resetLabel = props.resetLabel?.length ? props.resetLabel : `Reset price range`;

	const minValue = min || DEFAULT_MIN_RANGE;
	const maxValue = max || DEFAULT_MAX_RANGE;
	const steps = props.steps || DEFAULT_STEPS;

	const [fromValue, setFromValue] = useState(defaultFrom || minValue);
	const [toValue, setToValue] = useState(defaultTo || maxValue);
	
	useEffect(() => {
		if (live) {
			onChange(fromValue, toValue);
		}
	}, [fromValue, toValue]);

	const id = useId();
	const fromId = `from_${id}`;
	const toId = `to_${id}`;
	const labelFromId = `lfrom_${id}`;
	const labelToId = `lto_${id}`;

	const rangeDistance = maxValue - minValue;
	const fromPosition = Number(fromValue) - minValue;
	const toPosition = Number(toValue) - minValue;

	const showReset = (fromValue > minValue) || (toValue < maxValue);

	const rangeBackground = `linear-gradient(
      to right,
      var(--range-track-background) 0%,
      var(--range-track-background) ${(fromPosition) / (rangeDistance) * 100}%,
      var(--range-track-fill) ${((fromPosition) / (rangeDistance)) * 100}%,
      var(--range-track-fill) ${(toPosition) / (rangeDistance) * 100}%, 
      var(--range-track-background) ${(toPosition) / (rangeDistance) * 100}%, 
      var(--range-track-background) 100%)`;

	function changeFromSlider(e: React.FormEvent<HTMLInputElement>) {
		const newValue = stepToValue(parseInt((e.target as HTMLInputElement).value, 10));
		setFromValue(newValue > toValue ? toValue : newValue);
	}

	function changeToSlider(e: React.FormEvent<HTMLInputElement>) {
		const newValue = stepToValue(parseInt((e.target as HTMLInputElement).value, 10));
		setToValue(newValue < fromValue ? fromValue : newValue);
	}

	function resetRange() {
		setFromValue(minValue);
		setToValue(maxValue);
		onChange(minValue, maxValue);
	}

	var rangeClasses = ['ui-dual-range'];

	if (aligned) {
		rangeClasses.push('ui-dual-range--aligned');
	}

	function valueToStep(value: number) {
		var stepValue = (maxValue - minValue) / steps;
		return Math.round((value - minValue) / stepValue);
	}

	function stepToValue(step: number) {
		var stepValue = (maxValue - minValue) / steps;
		return minValue + (step * stepValue);
	}

	return (
		<div className={rangeClasses.join(' ')}>
			<label id={id} htmlFor={fromId}>
				{label}
			</label>
			<div role="group" aria-labelledby={id} className="ui-dual-range__internal">
				<div className="ui-dual-range__values">
					<label htmlFor={fromId} id={labelFromId}>
						{numberFormat ? numberFormat.format(fromValue) : fromValue}
					</label>
					<span>
						&mdash;
					</span>
					<label id={labelToId}>
						{numberFormat ? numberFormat.format(toValue) : toValue}
					</label>
				</div>

				<div className="ui-dual-range__from-to">
					<div className="ui-dual-range__gradient" style={{ 'background': rangeBackground }} />

					<input type="range" className="ui-dual-range__from" id={fromId}
						min="0" max={steps} step="1" value={valueToStep(fromValue)}
						aria-valuemin={minValue} aria-valuemax={toValue} aria-valuenow={fromValue} aria-labelledby={`${id} ${labelFromId}`}
						onInput={changeFromSlider} />

					<input type="range" className="ui-dual-range__to" id={toId}
						min="0" max={steps} step="1" value={valueToStep(toValue)}
						aria-valuemin={fromValue} aria-valuemax={maxValue} aria-valuenow={toValue} aria-labelledby={`${id} ${labelToId}`}
						onInput={changeToSlider} />

					{!live && <>
						<Button xs 
							onClick={(ev) => {
								onChange(fromValue, toValue);
								(ev.target as HTMLButtonElement).blur();
							}} 
							className="ui-dual-range__update"
						>
							{updateLabel}
						</Button>
					</>}
				</div>
				{showReset && <>
					<Button xs outlined onClick={() => resetRange()} className="ui-dual-range__reset">
						{resetLabel}
					</Button>
				</>}
			</div>
		</div>
	);
}

export default DualRange;