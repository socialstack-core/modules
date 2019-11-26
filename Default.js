import NavMenu from 'UI/NavMenu';
import MainMenu from 'Admin/MainMenu';

export default class Default extends React.Component{
	
	render(){
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