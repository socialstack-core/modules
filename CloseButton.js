const CLOSE_PREFIX = 'btn-close';

/**
 * Bootstrap CloseButton component
 * @param {boolean} isDisabled	- set true to disable button
 * @param {boolean} isLight		- set true to display light version
 * @param {string} label		- accessibility label
 * @param {function} callback	- onClick handler
 */
export default function CloseButton(props) {
	var { isDisabled, isLight, label, callback } = props;

	var btnCloseClass = [CLOSE_PREFIX];

	if (isLight) {
		btnCloseClass.push(CLOSE_PREFIX + '-white');
	}

	label = label || `Close`;

	return (<>
		<button type="button" className={btnCloseClass.join(' ')} disabled={isDisabled ? true : undefined} aria-label={label} onClick={callback}></button>
	</>);
}

CloseButton.propTypes = {
	isDisabled: 'bool',
	isLight: 'bool',
	label: 'string'
};

CloseButton.defaultProps = {
	isDisabled: false,
	isLight: false
}

CloseButton.icon = 'times';
