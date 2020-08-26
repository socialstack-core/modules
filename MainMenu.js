import NavMenu from 'UI/NavMenu';
import Canvas from 'UI/Canvas';
import webRequest from 'UI/Functions/WebRequest';
import getRef from 'UI/Functions/GetRef';

export default class MainMenu extends React.Component {
	render() {
		return (
			<div className="main-menu">
				<div className="logo" />
				<NavMenu id={'admin_primary'} asUl>
					{item =>
						<a href={item.target} className={
							item.target == '/en-admin/' ?
								(window.location.pathname == item.target ? 'active' : '') :
								(window.location.pathname.startsWith(item.target) ? 'active' : '')}>
							{getRef(item.iconRef, { className: 'fa-fw' })}
							<Canvas>{item.bodyJson}</Canvas>
						</a>
					}
				</NavMenu>
				<ul className="loop">
					<li className="loop-item">
						<a href={'#'} onClick={()=>{
							webRequest('user/logout').then(() => {
								global.app.setState({ user: null, realUser: null, company: null });
								global.pageRouter.go('/en-admin/');
							}).catch(e => {
								global.app.setState({ user: null, realUser: null, company: null });
								global.pageRouter.go('/en-admin/');
							})
						}}>
							<i className="fa fa-door-open fa-fw"></i>
							Logout
						</a>
					</li>
				</ul>
			</div>
		);
	}
}
