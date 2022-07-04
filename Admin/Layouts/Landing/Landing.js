export default class Landing extends React.Component{
	
	render(){
		return (
			<div id="content-root" className="body landing">
				<div className="main_container fullsize">
					<div className="landing_page fullsize">
						<div className="landing_panel">
							{this.props.children}
						</div>
					</div>
				</div>
			</div>
		);
	}
}