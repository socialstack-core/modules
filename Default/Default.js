import Loop from 'UI/Loop';
import Landing from 'Admin/Layouts/Landing';
import LoginForm from 'Admin/LoginForm';
import Tile from 'Admin/Tile';
import Alert from 'UI/Alert';
import logout from 'UI/Functions/Logout';
import getRef from 'UI/Functions/GetRef';
import logo from './logo.png';

export default class Default extends React.Component{
	
	constructor(props){
		super(props);
		this.state={};
	}
	
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
		
		var { url } = this.context.app.state;
		
		return (
			<>
				<div className="admin-header row">
					<div className="admin-menu col-4">
						<button onClick={() => {this.setState({menuOpen: !this.state.menuOpen})}}><i className="fa fa-bars" /></button>
					</div>
					<div className="logo col-4">
						<a href='/en-admin/'>{getRef(logo, {size: '80'})}</a>
					</div>
					<div className="logged-user col-4">
						{user.fullname || user.username || user.email} {user.avatarRef ? getRef(user.avatarRef, {size: 32}) : null}
					</div>
				</div>
				{this.props.children}
				{this.state.menuOpen && 
				<div className="admin-menu-open" onClick={() => {this.setState({menuOpen: false})}}>
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
	
}