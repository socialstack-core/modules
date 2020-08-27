import NavMenu from 'UI/NavMenu';
import MainMenu from 'Admin/MainMenu';
import Landing from 'Admin/Pages/Landing';
import LoginForm from 'Admin/LoginForm';
import Tile from 'Admin/Tile';
import Alert from 'UI/Alert';

export default class Default extends React.Component{
	
	render(){
		
		if(global.app.state.loadingUser){
			return <Landing>
				Logging in..
			</Landing>;
		}
		
		var {user, role} = global.app.state;
		
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
					<Alert>
						Hi {user.firstName} - you'll need to ask an existing admin to grant you permission to login here.
					</Alert>
				</Tile>
			</Landing>;
		}
		
		return (
			<div className="container-fluid h-100 pages-default">
				<div className="row h-100">
					<div className="admin-aside">
						<MainMenu />
					</div>
					<div className="admin-main-area">
						{this.props.children}
					</div>
				</div>
			</div>
		);
	}
	
}