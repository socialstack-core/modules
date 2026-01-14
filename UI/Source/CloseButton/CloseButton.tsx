const CLOSE_PREFIX : string = 'btn-close';

/**
 * Props for the Header component.
 */
interface CloseButtonProps {
	/**
	 * set true to disable button
	 */
	isDisabled?: boolean,
	/**
	 * set true to display light version
	 */
	isLight?: boolean,
	/**
	 * set true to reduce size
	 */
	isSmall?: boolean,
	/**
	 * Override the word 'close'
	 */
	label?: string,
	/**
	 * optional additional classname(s)
	 */
	className?: string,
	/**
	*	onClick handler
	*/
	callback: (event: React.MouseEvent<HTMLButtonElement>) => void
}

/**
 * Bootstrap CloseButton component
 */
const CloseButton: React.FC<CloseButtonProps> = (props) => {
	var { isDisabled, isLight, isSmall, label, callback, className } = props;

	var btnCloseClass : string[] = [CLOSE_PREFIX];

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

export default CloseButton;