import Loop from 'UI/Loop';
import Landing from 'Admin/Layouts/Landing';
import LoginForm from 'Admin/LoginForm';
import Tile from 'Admin/Tile';
import Alert from 'UI/Alert';
import logout from 'UI/Functions/Logout';
import getRef from 'UI/Functions/GetRef';
import logo from './logo.png';
import { useSession, useRouter, useTheme } from 'UI/Session';
import Dropdown from 'UI/Dropdown';

export default props => {
	
	const { session, setSession } = useSession();
	const { pageState, setPage } = useRouter();
	const [menuOpen, setMenuOpen ] = React.useState(false);
	const { adminLogoRef } = useTheme();
	var { url } = pageState;
	
	
	if(session.loadingUser){
		return <Landing>
			Logging in..
		</Landing>;
	}
	
	var {user, role} = session;
	
	if(!user){
		// Login page
		return <Landing>
			<Tile>
				<LoginForm noRedirect />
			</Tile>
		</Landing>;
	}
	
	if(!role || !role.canViewAdmin){
		return <Landing>
			<Tile>
				<p>
					Hi {user.firstName} - you'll need to ask an existing admin to grant you permission to login here.
				</p>
				<a href={'#'} className="btn btn-secondary" onClick={()=>logout('/en-admin/', setSession, setPage)}>
					Logout
				</a>
			</Tile>
		</Landing>;
	}

	var dropdownLabelJsx = <>
			{user.fullname || user.username || user.email} <span className="avatar">{user.avatarRef ? getRef(user.avatarRef, {size: 32}) : null}</span>
		</>;
	
	return (
		<>
			<div className="admin-header row">
				<div className="admin-menu col-4">
					<button onClick={() => setMenuOpen(!menuOpen)}><i className="fa fa-bars" /></button>
				</div>
				<div className="logo col-4">
					<a href='/en-admin/'>{getRef(adminLogoRef || logo, {attribs: {height: '38'}})}</a>
				</div>
				<div className="user col-4">
					<Dropdown className="logged-user" label={dropdownLabelJsx} variant="link" align="Right">
						<li>
							<a href="/" className="btn dropdown-item">
								Return to site
							</a>
						</li>
						<li>
							<hr class="dropdown-divider" />
						</li>
					    <li>
							<button type="button" className="btn dropdown-item" onClick={() => logout('/en-admin/', setSession, setPage)}>
								Logout
							</button>
						</li>
					</Dropdown>
				</div>
			</div>
			{props.children}
			{menuOpen && 
			<div className="admin-menu-open" onClick={() => setMenuOpen(false)}>
				<div className="admin-drawer">
					<Loop over='adminnavmenuitem/list' filter={{ sort: { field: 'Title' } }} asUl>
						{item =>
							<a href={item.target} className={
								item.target == '/en-admin/' ?
									(url == item.target ? 'active' : '') :
									(url.startsWith(item.target) ? 'active' : '')}>
									{getRef(item.iconRef, { className: 'fa-fw' })}
									{item.title}
							</a>
						}
					</Loop>
				</div>
			</div>
			}
		</>
	);
}