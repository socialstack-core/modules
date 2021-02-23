import NavMenu from 'UI/NavMenu';
import Canvas from 'UI/Canvas';
import webRequest from 'UI/Functions/WebRequest';
import getRef from 'UI/Functions/GetRef';
import logout from 'UI/Functions/Logout';

export default class MainMenu extends React.Component {
	render() {
		var { url } = this.context.app.state;
		
		return (
			<div className="main-menu">
				<div className="logo" />
				<NavMenu id={'admin_primary'} filter={{ sort: { field: 'BodyJson' } }} asUl>
					{item =>
						<a href={item.target} className={
							item.target == '/en-admin/' ?
								(url == item.target ? 'active' : '') :
								(url.startsWith(item.target) ? 'active' : '')}>
							{getRef(item.iconRef, { className: 'fa-fw' })}
							<Canvas>{item.bodyJson}</Canvas>
						</a>
					}
				</NavMenu>
				<ul className="loop">
					<li className="loop-item">
						<a href={'#'} onClick={()=>{
							logout('/en-admin/', this.context);
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
