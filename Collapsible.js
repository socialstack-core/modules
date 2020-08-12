export default class Collapsible extends React.Component {
	
	constructor(props){
		super(props);
		this.state={
		};
	}
	
	render(){
		return <details className="collapsible">
			<summary className="collapsible-summary">
				<h4 className="collapsible-title">
					{this.props.title}
				</h4>
				<div className="collapsible-icon">
					<i className="far fa-chevron-down"></i>
				</div>
			</summary>
			<div className="collapsible-content">
				{this.props.children}
			</div>
		</details>;
	}
	
}
