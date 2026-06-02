interface ExpanderContentProps {
	/**
	 * optional additional classes
	 */
	className?: string,

	/**
	 * 
	 */
	children: React.ReactNode | React.ReactNode[];
}

/**
 * The Expander Content React component.
 * @param props React props.
 */
const Content: React.FC<React.PropsWithChildren<ExpanderContentProps>> = (props) => {
	const { children, className } = props;

	const componentClasses = ['ui-expander__content'];

	if (className?.length) {
		componentClasses.push(className);
	}

	return (
		<div className={componentClasses.join(' ')}>
			{children}
		</div>
	);
}

export default Content;
