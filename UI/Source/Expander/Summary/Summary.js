const COMPONENT_PREFIX = 'ui-expander__summary';

export default function Summary(props) {
	const { label, children, className } = props;

	let componentClasses = [COMPONENT_PREFIX];

	if (className) {
		componentClasses.push(className);
	}

	return (
		<summary className={componentClasses.join(' ')}>
			{children || label}
		</summary>
	);
}

Summary.propTypes = {
	label: 'string'
};

Summary.defaultProps = {
}
