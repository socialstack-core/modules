import Landing from 'Admin/Layouts/Landing';
import LoginForm from 'Admin/LoginForm';
import Tile from 'Admin/Tile';
import { useSession } from 'UI/Session';
import { useState } from 'react';
import store from 'UI/Functions/Store';
import Alert from 'UI/Alert';
import AdminHeader from "Admin/Header";
import Link from "UI/Link";
import AdminPage from "Admin/AdminPage";


const BaseAdminTemplate: React.FC<React.PropsWithChildren> = (props: React.PropsWithChildren) => {
	const { children } = props;
		
	// =================
	// Hooks State
	// =================
	const { session } = useSession();
	const { user, role } = session;
	
	// ================================= //
	// /!\ No hooks after this point /!\ //
	// ================================= //
	
	// ----------------------------------
	// When a user isn't logged in, show the login 
	// form
	// ----------------------------------
	if (!user) {
		return (
			<Landing>
				<Tile>
					<LoginForm noRedirect />
				</Tile>
			</Landing>
		);
	}
	
	if (!role?.canViewAdmin) {
		return (
			<Landing>
				<Tile>
					<Alert variant="warning">
						<p>
							{`Hello ${user.fullName || user.username || user.email}, you don't have permission to view this area`}
						</p>
					</Alert>
					<Link variant="primary" href="/">
						{`Back to website`}
					</Link>
				</Tile>
			</Landing>
		);
	}

	return <>
		<div className="admin-page">
			<AdminHeader />
			<AdminPage>
				{children}
			</AdminPage>
		</div>
	</>;
}

export default BaseAdminTemplate;