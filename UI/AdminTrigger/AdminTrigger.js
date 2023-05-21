import { useSession, useRouter } from 'UI/Session';
import Dropdown from 'UI/Dropdown';
import webRequest from 'UI/Functions/WebRequest';

export default function AdminTrigger(props) {
	
	const { session, setSession } = useSession();

	var { user, realUser, role } = session;
	
	// not logged in
	if (!user || !props.page) {
		return null;
	}
	
	var editUrl = '/en-admin/page/' + props.page.id + '/?context=' + encodeURIComponent(window.location.pathname);
	
	// impersonating?
	console.log("USER: ", user);
	console.log("REALUSER: ", realUser);
	var isImpersonating = realUser && (user.id != realUser.id);

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
		<i className="fal fa-cog"></i>
	</>;

	function endImpersonation() {
		return webRequest('user/unpersonate').then(response => {
			window.location.reload();
		});
	}
	
	return <>
		<div id="admin-trigger">
			<Dropdown isSmall title={`Administration`}
				label={triggerLabelJsx} variant="dark" align="Right" position="Top">
				<li className="admin-trigger__env admin-trigger__env--stage">
					<h6 className="dropdown-header">
						<i className="fal fa-fw fa-exclamation-triangle"></i> {`STAGE`}
					</h6>
				</li>
				<li className="admin-trigger__env admin-trigger__env--uat">
					<h6 className="dropdown-header">
						<i className="fal fa-fw fa-exclamation-triangle"></i> {`UAT`}
					</h6>
				</li>
				<li className="admin-trigger__env admin-trigger__env--prod">
					<h6 className="dropdown-header">
						<i className="fal fa-fw fa-exclamation-triangle"></i> {`PRODUCTION`}
					</h6>
				</li>

				{isImpersonating && <>
					<li>
						<button type="button" className="btn dropdown-item" onClick={() => endImpersonation()}>
							{`End impersonation`}
						</button>
					</li>
					<li>
						<hr className="dropdown-divider" />
					</li>
				</>}
				{editUrl && <>
					<li>
						<a href={editUrl} className="btn dropdown-item">
							<i className="fal fa-fw fa-edit"></i> {`Edit this page`}
						</a>
					</li>
					<li>
						<hr className="dropdown-divider" />
					</li>
				</>}
				<li>
					<a href="/en-admin" className="btn dropdown-item">
						{`Return to admin`}
					</a>
				</li>
			</Dropdown>
			{isImpersonating && <i className="fa fa-mask impersonating"></i>}
		</div>
		{props.children}
	</>;
};