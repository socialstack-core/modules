/**
 * Props for the Content Wrapper component.
 */
interface AdminPageContentWrapperProps {
	className?: string,
	children?: React.ReactNode
}

/**
 * The Content Wrapper React component.
 * @param props React props.
 */
const AdminPageContentWrapper: React.FC<React.PropsWithChildren<AdminPageContentWrapperProps>> = (props) => {
	const { className, children } = props;

	const contentWrapperClasses = ['admin-page__content-wrapper'];

	if (className?.length) {
		contentWrapperClasses.push(className);
	}

	return (
		<main id="site_content_wrapper" className={contentWrapperClasses.join(' ')}>
			{children}
		</main>
	);
}

// required for preact (without preact/compat layer) support
AdminPageContentWrapper.displayName = "AdminPageContentWrapper";

export default AdminPageContentWrapper;
