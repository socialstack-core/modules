export default class Collapsible extends React.Component {
	
	constructor(props){
		super(props);
		this.state={
			collapsed: this.props.defaultCollapsed===undefined ? true : this.props.defaultCollapsed
		};
	}
	
	render(){
		
		return <div className="collapsible">
			<h2 onClick={() => {
				this.setState({collapsed: !this.state.collapsed});
			}}>
				{this.props.title}
			</h2>
			{!this.state.collapsed && (
				<div className="collapsible-body">
					{this.props.children}
				</div>
			)}
		</div>;
		
	}
	
}
