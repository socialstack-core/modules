/**
 * All available sizes (type prop) for a container.
 */
type ContainerSize = 'sm' | 'md' | 'lg' | 'xl';

/**
 * Props for the Container component.
 */
interface ContainerProps extends React.HTMLAttributes<HTMLDivElement> {
	/**
	 * Optional extra class name(s)
	 */
	className?: string

	/**
	 * Container type
	 */
	type?: ContainerSize

	/*
	* True if you want to hide this container.
	*/
	hidden?: boolean,

	/**
	 * Due to children not being exported as
	 * a property need to manually add it to
	 * support the children property.
	 */
	children?: React.ReactNode | undefined
}

/**
 * A specific width container for content.
 */
const Container: React.FC<ContainerProps> = ({ hidden, type, className, children, ...props}) => {
	if (hidden) {
		return;
	}

	return <div className={"container" + (type ? "-" + type : '') + " " + (className || '')} {...props}>
		{children}
	</div>;
}

export default Container;