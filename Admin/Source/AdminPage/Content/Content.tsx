/**
 * Props for the Content component.
 */
interface AdminPageContentProps {
	/**
	 * true if side padding should be omitted
	 */
	noPadding?: boolean,

	/**
	 * true if vertical spacing between child components should be omitted
	 */
	noSpacing?: boolean,

	/**
	 * optional additonal classnames
	 */
	className?: string,

	children?: React.ReactNode
}

/**
 * The Content React component.
 * @param props React props.
 */
const AdminPageContent: React.FC<React.PropsWithChildren<AdminPageContentProps>> = (props) => {
	const { noPadding, noSpacing, className, children } = props;
	const contentClasses = ["admin-page__content"];

	if (noPadding) {
		contentClasses.push("admin-page__content--no-padding");
	}

	if (noSpacing) {
		contentClasses.push("admin-page__content--no-spacing");
	}

	if (className?.length) {
		contentClasses.push(className);
	}

	return (
		<div className={contentClasses.join(' ')}>
			{children}
		</div>
	);
}

// required for preact (without preact/compat layer) support
AdminPageContent.displayName = "AdminPageContent";

export default AdminPageContent;
