import webRequest from 'UI/Functions/WebRequest';

/**
 * A grid of capabilities and the roles they're active on
 */
export default class PermissionGrid extends React.Component {
	
	constructor(props){
		super(props);
		this.state={};
	}
	
	componentDidMount(){
		this.load(this.props);
	}
	
	componentWillReceiveProps(props){
		this.load(props);
	}
	
	load(props){
		webRequest('permission/list').then(permissionInfo => {
			// Has .capabilities and .roles
			this.setState(permissionInfo.json);
			console.log(this.state);
		});
	}
	
	render() {
		if(!this.state.capabilities){
			return null;
		}
		
        return (
            <div className={'permission-grid'}>
				<table className="table">
					<tr>
						<th>Capability</th>
						{this.state.roles.map(role => {
							return (
								<th>
									{role.name}
								</th>
							);
						})}
					</tr>
					{this.state.capabilities.map(cap => {
						
						var map = {};
						
						if(cap.grants){
							cap.grants.forEach(grant => {
								map[grant.role.key] = grant;
							});
						}
						
						
						return (
							<tr>
								<td>
									{cap.key}
								</td>
								{this.state.roles.map(role => {
									
									var grant = map[role.key];
									
									if(!grant){
										return (<td>
											<i className='fa fa-minus-circle' style={{color: 'red'}}/>
										</td>);
									}
									
									if(grant.ruleDescription && grant.ruleDescription.length){
										return (
											<td>
												<i className='fa fa-check' style={{color: 'orange'}}/>
												<p style={{fontSize: 'smaller'}}>
													{grant.ruleDescription}
												</p>
											</td>
										);
									}
									
									return (
										<td>
											<i className='fa fa-check' style={{color: 'green'}}/>
										</td>
									);
								})}
							</tr>
						);
					})}
				</table>
            </div>
        );
    }
	
}