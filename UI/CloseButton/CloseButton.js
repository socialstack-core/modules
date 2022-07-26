const CLOSE_PREFIX = 'btn-close';

/**
 * Bootstrap CloseButton component
 * @param {boolean} isDisabled	- set true to disable button
 * @param {boolean} isLight		- set true to display light version
 * @param {boolean} isSmall		- set true to reduce size
 * @param {string} label		- accessibility label
 * @param {string} className	- optional additional classname(s)
 * @param {function} callback	- onClick handler
 */
export default function CloseButton(props) {
	var { isDisabled, isLight, isSmall, label, callback, className } = props;

	var btnCloseClass = [CLOSE_PREFIX];

	if (isLight) {
		btnCloseClass.push(CLOSE_PREFIX + '-white');
	}

	if (isSmall) {
		btnCloseClass.push(CLOSE_PREFIX + '--sm');
    }

	if (className) {
		btnCloseClass.push(className);
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
