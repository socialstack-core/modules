export default class Login extends React.Component{
    render(){
        return(
            <div className="container-fluid h-100 pages-login">
				<div className="row h-100">
					<div className="col-12 admin-main-area">
						
						{this.props.children}
						
					</div>
				</div>
			</div>
        );
    }
}