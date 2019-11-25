import NavMenu from 'UI/NavMenu';
import Canvas from 'UI/Canvas';
import getRef from 'UI/Functions/GetRef';

export default class MainMenu extends React.Component {
	render() {
		return (
			<div className="main-menu">
				<NavMenu id={'admin_primary'} asUl>
					{item => <a href={item.target}>
							{getRef(item.iconRef)}
							<Canvas>{item.bodyJson}</Canvas>
						</a>
					}
				</NavMenu>
			</div>
		);
	}
}