import Container from 'UI/Container';
import NavMenu from 'UI/NavMenu'; 
import getRef from 'UI/Functions/GetRef'; 
import {SessionConsumer} from 'UI/Session';

export default class Header extends React.Component{
	
	constructor(props) {
		super(props);
		this.toggleNavbar = this.toggleNavbar.bind(this);
		this.state = {
			collapsed: true,
		};
	}

	toggleNavbar() {
		this.setState({
			collapsed: !this.state.collapsed,
		});
	}

	render(){
		return <SessionConsumer>
			{session => this.renderIntl(session)}
		</SessionConsumer>
	}

	renderIntl(session) {
		const collapsed = this.state.collapsed;
		const classOne = collapsed ? 'collapse navbar-collapse' : 'collapse navbar-collapse show';
		const classTwo = collapsed ? 'navbar-toggler navbar-toggler-right collapsed' : 'navbar-toggler navbar-toggler-right';
		
		return (
			<nav className="navbar header navbar-expand-lg transparent-nav">
				<Container>
					<a className="navbar-brand" href="/">{getRef(this.props.logoRef)}</a>
					<button onClick={this.toggleNavbar} className={`${classTwo}`} type="button" data-toggle="collapse" data-target="#navbarResponsive" aria-controls="navbarResponsive" aria-expanded="false" aria-label="Toggle navigation">
						<span className="fa fa-bars" />
					</button>
					<div className={`${classOne}`} id="navbarResponsive">
						<NavMenu id={(this.props.varyForUsers && session.user) ? 'primary_loggedin' : 'primary'} inline/>
					</div>
				</Container>
			</nav>
		);
	}
}

Header.propTypes = {
	logoRef: 'image',
	varyForUsers: 'checkbox'
};