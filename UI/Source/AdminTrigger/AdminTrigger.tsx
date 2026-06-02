import { useSession } from 'UI/Session';
import Dropdown, { DropdownItem } from 'UI/Dropdown';
import Icon from 'UI/Icon';
import userApi from 'Api/User';
import { Page } from 'Api/Page';

/**
 * Props for the admin trigger menu component.
 */
interface AdminTriggerProps {
	page?:Page
};

/**
 * Admin menu
 * @param props
 * @returns
 */
const AdminTrigger: React.FC<React.PropsWithChildren<AdminTriggerProps>> = props => {
	
	const { session, setSession } = useSession();
	var { user, realuser, role } = session;
	
	// not logged in
	if (!user || !props.page) {
		return null;
	}
	
	var editUrl = '/en-admin/page/' + props.page.id + '/?context=' + encodeURIComponent(window.location.pathname);
	
	// impersonating?
	var isImpersonating = realuser && (user.id != realuser.id);

	// not an admin
	if ((!role || !role.canViewAdmin) && !isImpersonating) {
		return <>
			{props.children}
		</>;
	}

	/*
	// viewing admin page
	if (url.startsWith('/en-admin')) {
		return <>
			{props.children}
		</>;
	}
    */

	var triggerLabelJsx = <>
		<Icon type="fa-cog" light />
	</>;

	function endImpersonation(e: React.MouseEvent<HTMLButtonElement>) {
		userApi.unpersonate(setSession).then(response => {
			window.location.reload();
		});
	}

	var ddItems: DropdownItem[] = [];

	if (isImpersonating) {
		ddItems.push({
			onClick: endImpersonation,
			text: `End impersonation`,
			icon: <Icon type="fa-mask" />
		}, {
			divider: true
		});
	}

	if (editUrl) {
		ddItems.push({
			href: editUrl,
			text: `Edit this page`,
			icon: <Icon type="fa-edit" light />
		}, {
			divider: true
		});
	}

	ddItems.push({
		href: '/en-admin',
		text: `Return to admin`
	});

	return <>
		<div id="admin-trigger">
			<Dropdown isSmall title={`Administration`}
				label={triggerLabelJsx} variant="dark" align="Right" position="Top" items={ddItems} />
			{isImpersonating && <Icon type="fa-mask" />}
		</div>
		{props.children}
	</>;
};

export default AdminTrigger;