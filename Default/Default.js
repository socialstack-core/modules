import Loop from 'UI/Loop';
import Landing from 'Admin/Layouts/Landing';
import LoginForm from 'Admin/LoginForm';
import Tile from 'Admin/Tile';
import Alert from 'UI/Alert';
import logout from 'UI/Functions/Logout';
import getRef from 'UI/Functions/GetRef';
import logo from './logo.png';
import { useSession, useRouter } from 'UI/Session';

export default props => {
	
	const { session, setSession } = useSession();
	const { pageState, setPage } = useRouter();
	const [menuOpen, setMenuOpen ] = React.useState(false);
    const [userMenuOpen, setUserMenuOpen ] = React.useState(false); 
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
	
	return (
		<>
			<div className="admin-header row">
				<div className="admin-menu col-4">
					<button onClick={() => setMenuOpen(!menuOpen)}><i className="fa fa-bars" /></button>
				</div>
				<div className="logo col-4">
					<a href='/en-admin/'>{getRef(logo, {size: '80'})}</a>
				</div>
				<div className="logged-user col-4 dropdown" onClick={() => setUserMenuOpen(!userMenuOpen)}>
					    {user.fullname || user.username || user.email} {user.avatarRef ? getRef(user.avatarRef, {size: 32}) : null}
                        <div className={ "dropdown-menu dropdown-menu-right" + (userMenuOpen? " show" : "")} >
                            <a className="dropdown-item" href='/' >Return To Site</a>
                            <a className="dropdown-item" href={'#'} onClick={()=>logout('/en-admin/', setSession, setPage)}>
                            Logout
                           </a>
                        </div> 
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