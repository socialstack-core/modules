import NavMenu from 'UI/NavMenu';
import MainMenu from 'Admin/MainMenu';
import Landing from 'Admin/Pages/Landing';
import LoginForm from 'Admin/LoginForm';
import Tile from 'Admin/Tile';
import Alert from 'UI/Alert';
import logout from 'UI/Functions/Logout';

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
			<div className="container-fluid h-100 pages-default">
				<div className="row h-100">
					<div className="col-2 admin-aside">
						<MainMenu />
					</div>
					<div className="col-10 admin-main-area">
						{this.props.children}
					</div>
				</div>
			</div>
		);
	}
	
}