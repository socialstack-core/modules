/**
 * Props for the Footer component.
 */
type FooterProps = React.PropsWithChildren<{
	className?: string,
	children?: React.ReactNode
}>;

type FooterBulkActionsProps = React.PropsWithChildren<{
	children?: React.ReactNode
}>;

type FooterCallsToActionProps = React.PropsWithChildren<{
	children?: React.ReactNode
}>;

/**
 * The Footer React component.
 * @param props React props.
 */
const FooterRoot: React.FC<React.PropsWithChildren<FooterProps>> = (props) => {
	const { className, children } = props;
	let bulkActionsNode: React.ReactNode = null;
	let callsToActionNode: React.ReactNode = null;

	const flattenChildren = (children: React.ReactNode): any[] => {
		if (children == null) return [];
		return Array.isArray(children) ? children.flat() : [children];
	};

	const isType = (child: any, component: any) => {
		return child?.type === component || child?.type?.displayName === component.displayName;
	};

	const allChildren = flattenChildren(children);

	allChildren.forEach(child => {
		if (child && typeof child === 'object') {
			if (isType(child, FooterBulkActions)) {
				bulkActionsNode = child.props.children;
			} else if (isType(child, FooterCallsToAction)) {
				callsToActionNode = child.props.children;
			}
		}
	});

	const filteredChildren = allChildren.filter(child => {
		return !(child && typeof child === 'object' && (isType(child, FooterBulkActions) || isType(child, FooterCallsToAction)));
	});

	if (callsToActionNode == null) {
		callsToActionNode = filteredChildren;
	}

	const footerClasses = ['admin-page__footer'];

	if (className?.length) {
		footerClasses.push(className);
	}

	return <>
		<footer className={footerClasses.join(' ')}>
			{bulkActionsNode && <>
				<span className="admin-page__footer-bulk-actions">
					{bulkActionsNode}
				</span>
			</>}
			<span className="admin-page__footer-cta">
				{callsToActionNode}
			</span>
		</footer>
	</>;
}

function FooterBulkActions(props: FooterBulkActionsProps) {
	const { children } = props;

	return children;
}

function FooterCallsToAction(props: FooterCallsToActionProps) {
	const { children } = props;

	return children;
}

// required for preact (without preact/compat layer) support
FooterBulkActions.displayName = "FooterBulkActions";
FooterCallsToAction.displayName = "FooterCallsToAction";

FooterRoot.BulkActions = FooterBulkActions;
FooterRoot.CallsToAction = FooterCallsToAction;

const Footer = FooterRoot;
export default Footer;