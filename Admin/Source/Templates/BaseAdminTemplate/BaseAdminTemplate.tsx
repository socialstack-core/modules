import Loop from 'UI/Loop';
import Landing from 'Admin/Layouts/Landing';
import LoginForm from 'Admin/LoginForm';
import Tile from 'Admin/Tile';
import logout from 'UI/Functions/Logout';
import { useSession } from 'UI/Session';
import { useRouter } from 'UI/Router';
import Dropdown, { DropdownItem } from 'UI/Dropdown';
import { useState } from 'react';
import store from 'UI/Functions/Store';
import Modal from 'UI/Modal';
import Icon, { IconRef } from 'UI/Icon';
import Table from 'UI/Table';
import Image from 'UI/Image';
import Alert from 'UI/Alert';
import userApi, { User } from 'Api/User';
import adminNavMenuApi from 'Api/AdminNavMenuItem';
import Header from "Admin/Header";
import AdminNavMenu from "Admin/AdminNavMenu";
import Link from "UI/Link";


const BaseAdminTemplate: React.FC<React.PropsWithChildren> = (props: React.PropsWithChildren) => {
	
	// ==================
	// Local Functions
	// ==================
	function updateSchemeVars(scheme : string) {
		var html = window.SERVER ? undefined : document.querySelector("html");

		if (!html) {
			return;
		}

		html.setAttribute("data-theme-variant", scheme)
	}

	function updateScheme(scheme : string) {
		updateSchemeVars(scheme);
		store.set('colour_scheme', scheme);
		setColourScheme(scheme);
	}
	
	// ==================
	// Local State
	// ==================
	const [navOpen, setNavOpen] = useState(false);
	const [colourScheme, setColourScheme] = useState(() => {
		// TEMP: enforce light theme
		//var scheme: string = store.get('colour_scheme') || 'auto';
		var scheme: string = 'light';
		updateSchemeVars(scheme);

		return scheme;
	});
	
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
			<div className={'no-view-admin'}>
				<div className={'inner'}>
					<Alert variant={'warning'}>
						<p>{`Hello ${user.fullName || user.username || user.email}, you don't have permission to view this area`}</p>
					</Alert>
					<Link variant={'primary'} href={'/'}>{`Back to website`}</Link>
				</div>
			</div>
		)
	}
	
	
	return (
		<div className={'admin-page'}>
			<Header 
				navOpen={navOpen}
				setNavOpen={setNavOpen}
			/>
			<div className={'main-container'}>
				<AdminNavMenu navOpen={navOpen} />
				<main>
					<div className={'main-inner'}>
						{props.children}
					</div>
				</main>
			</div>	
		</div>
	)
}

export default BaseAdminTemplate;