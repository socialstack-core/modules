import NavMenu from 'UI/NavMenu';
import Canvas from 'UI/Canvas';
import getRef from 'UI/Functions/GetRef';
import logo from './logo.svg';

export default class MainMenu extends React.Component {
	render() {
		return (
			<div className="main-menu">
				<img className="logo" src={logo} alt="Logo" />
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
			</div>
		);
	}
}
