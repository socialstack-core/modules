import NavMenu from 'UI/NavMenu';
import MainMenu from 'Admin/MainMenu';
import Landing from 'Admin/Pages/Landing';
import LoginForm from 'Admin/LoginForm';
import Tile from 'Admin/Tile';
import Alert from 'UI/Alert';
import logout from 'UI/Functions/Logout';
import getRef from 'UI/Functions/GetRef';
import logo from './logo.png';

export default class Default extends React.Component{
	
	render(){
		var { app } = this.context;
		
		if(app.state.loadingUser){
			return <Landing>
				Logging in..
			</Landing>;
		}
		
		var {user, role} = app.state;
		
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
					<a href={'#'} className="btn btn-secondary" onClick={()=>{
						logout('/en-admin/', this.context);
					}}>
						Logout
					</a>
				</Tile>
			</Landing>;
		}
		
		
		
		return (
			<>
				<div className="admin-header row">
					<div className="admin-menu col-4">
						<i className="fa fa-bars" />
					</div>
					<div className="logo col-4">
						{getRef(logo, {size: '80'})}
					</div>
					<div className="logged-user col-4">
						{user.fullname || user.username || user.email} {user.avatarRef ? getRef(user.avatarRef, {size: 32}) : null}
					</div>
				</div>
				{this.props.children}
			</>
		);
	}
	
}