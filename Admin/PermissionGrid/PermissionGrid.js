import webRequest from 'UI/Functions/WebRequest';
import Input from 'UI/Input';
import Modal from 'UI/Modal';

/**
 * A grid of capabilities and the roles they're active on
 */
export default class PermissionGrid extends React.Component {
	
	constructor(props){
		super(props);
		this.state={
			roles: [],
			capabilities: [],
			filter: '',
			filteredCapabilities: [],
			editingCell: null
		};
	}
	
	componentDidMount(){
		this.load(this.props);
	}
	
	componentWillReceiveProps(props){
		this.load(props);
	}
	
	load(props){
		webRequest('permission/list').then(permissionInfo => {
			
			if(props.editor){
				var grants = {};
				try{
					grants = JSON.parse(this.props.value || this.props.defaultValue);
					
					if(!grants || Array.isArray(grants)){
						grants = {};
					}
				}catch(e){
					console.log("Bad grant json: ", e);
				}
				
				this.setState({
					grants
				});
			}
			
			// Has .capabilities and .roles
			permissionInfo.json.filteredCapabilities = permissionInfo.json.capabilities;
			this.setState(permissionInfo.json);
		});
	}

	updateFilter(filter) {
		this.setState({
			filter: filter,
			filteredCapabilities: this.state.capabilities.filter((capability) => capability.key.toLowerCase().includes(filter.toLowerCase()))
		});
	}

	clearFilter() {
		this.setState({
			filter: '',
			filteredCapabilities: this.state.capabilities
		});
	}

	renderFilter() {
		return <>
			<th>
				<div className="admin_permission-grid__filter">
					<label htmlFor="permission_filter" className="col-form-label">
						{`Capability`}
					</label>
					<div className="admin_permission-grid__filter-field input-group">
						<input type="text" className="form-control" id="permission_filter" placeholder={`Filter by`}
							value={this.state.filter} onKeyUp={(e) => this.updateFilter(e.target.value)} />
						<button className="btn btn-outline-secondary" type="button" onClick={() => this.clearFilter()}>
							{`Clear`}
						</button>
					</div>
				</div>
			</th>
		</>;
	}

	renderList() {
		if (!this.state.capabilities) {
			return null;
		}

		this.state.filteredCapabilities.sort(function(a, b) {
			if (a.key < b.key) return -1;
			if (a.key > b.key) return 1;
			return 0;
		});

		let noMatches = (!this.state.filteredCapabilities || this.state.filteredCapabilities.length == 0) && this.state.capabilities.length > 0;

        return (
            <div className={'admin_permission-grid'}>
				<table className="table table-striped">
					<thead>
						<tr>
							{this.renderFilter()}
							{this.state.roles.map(role => {
								return (
									<th>
										{role.name}
									</th>
								);
							})}
						</tr>
					</thead>
					<tbody>
						{noMatches && <>
							<tr>
								<td colspan={this.state.roles.length + 1}>
									<span className="admin_permission-grid--nomatch">
										{`No matching capabilities`}
									</span>									
								</td>
							</tr>
						</>}

						{this.state.filteredCapabilities.map(cap => {
							var map = {};

							if (cap.grants) {
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

										if (!grant) {
											return (<td>
												<i className='fa fa-minus-circle' style={{ color: 'red' }} />
											</td>);
										}

										if (grant.ruleDescription && grant.ruleDescription.length) {
											return (
												<td>
													<i className='fa fa-check' style={{ color: 'orange' }} />
													<p style={{ fontSize: 'smaller' }}>
														{grant.ruleDescription}
													</p>
												</td>
											);
										}

										return (
											<td>
												<i className='fa fa-check' style={{ color: 'green' }} />
											</td>
										);
									})}
								</tr>
							);
						})}
					</tbody>
					{!noMatches && <>
						<tfoot>
							<tr>
								<td colspan={this.state.roles.length + 1}>
								abc
									{!this.state.filter || this.state.filter.length == 0 && <>
										x
										{`Displaying ${this.state.capabilities.length} capabilities`}
									</>}
									{this.state.filter && this.state.filter.length > 0 && <>
										xx
										{`Displaying ${this.state.filteredCapabilities.length} of ${this.state.capabilities.length} capabilities`}
									</>}
								</td>
							</tr>
						</tfoot>
					</>}
				</table>
            </div>
        );
	}

	renderCell(grantInfo){

		if(grantInfo.value === false)
		{
			// red x
			return <i className='fa fa-minus-circle' style={{color: 'red'}}/>;
		}
		else if(typeof grantInfo.value === 'string')
		{
			// text assumed:
			return <div>
				<i className='fa fa-check' style={{color: 'orange'}}/>
				<p style={{fontSize: 'smaller'}}>
					{grantInfo.value}
				</p>
			</div>
		}
		
		//tick
		return <i className='fa fa-check' style={{color: 'green'}}/>;
	}
	
	getGrantInfo(capability) {
		var content = this.content();
		var capGrant = null; // Not granted is the default
		if(capability.grants){
			var grantSet = capability.grants;
			for(var i=0;i<grantSet.length;i++){
				if(grantSet[i].role.key == content.key){
					capGrant = grantSet[i];
					break;
				}
			}
			
		}
		
		var current = {
			inherited: true
		};
		
		if(!capGrant){
			// Inherited grant is x:
			current.value = false;
		}else if(capGrant.ruleDescription && capGrant.ruleDescription.length){
			current.value = capGrant.ruleDescription;
		}else{
			// Inherited is a tick:
			current.value = true;
		}
		
		if(this.state.grants && this.state.grants[capability.key] !== undefined){
			current.inherited = false;
			current.value = this.state.grants[capability.key];
		}
		
		return current;
	}
	
	content(){
		return this.props.currentContent || {};
	}
	
	renderEditMode(){

		if(!this.state.grants || !this.state.filteredCapabilities){
			return null;
		}
		
		this.state.filteredCapabilities.sort(function(a, b) {
			if (a.key < b.key) return -1;
			if (a.key > b.key) return 1;
			return 0;
		});

		let noMatches = (!this.state.filteredCapabilities || this.state.filteredCapabilities.length == 0) && this.state.capabilities.length > 0;

		return [
			<Input type='hidden' inputRef={ref => {
				this.ref = ref;
				if(ref){
					ref.onGetValue = () => {
						return JSON.stringify(this.state.grants);
					};
				}
			}} label={`Grants`} name={this.props.name} />,
			<div className={'admin_permission-grid'}>
				<table className="table table-striped">
					<thead>
					<tr>
						{this.renderFilter()}
						{this.state.roles.map(role => {
							if(this.content().id == role.id) {
								return (
									<th>
										{role.name}
									</th>
								);
							}
						})}
					</tr>
					</thead>
					<tbody>
						{noMatches && <>
							<tr>
								<td colspan={2}>
									<span className="admin_permission-grid--nomatch">
										{`No matching capabilities`}
									</span>
								</td>
							</tr>
						</>}

					{this.state.filteredCapabilities.map(cap => {
						
						// Is it overriden? If yes, we have a custom value (otherwise it's inherited).
						var grantInfo = this.getGrantInfo(cap);
						
						return (
							<tr>
								<td>
									{cap.key}
								</td>
								<td onClick = {() => {
									this.setState({
										editingCell: {
											key: cap.key,
											grantInfo
										},
										dropdownType: this.dropdownType(grantInfo)
									});
								}}>
									{this.renderCell(grantInfo)}
								</td>
							</tr>);
					})}
					</tbody>
					{!noMatches && <>
						<tfoot>
							<tr>
								<td colspan="2">
									{this.state.filter.length == 0 && <>
										{`Displaying ${this.state.capabilities.length} capabilities`}
									</>}
									{this.state.filter.length > 0 && <>
										{`Displaying ${this.state.filteredCapabilities.length} of ${this.state.capabilities.length} capabilities`}
									</>}
								</td>
							</tr>
						</tfoot>
					</>}
				</table>

				{this.state.editingCell && this.renderEditModal()}
			</div>
		];
	}
	
	dropdownType(grantInfo){
		if(grantInfo.inherited){
			return "inherited";
		}
		
		if(grantInfo.value === true){
			return "always";
		}
		
		if(grantInfo.value === false){
			return "never";
		}
		
		return "custom";
	}
	
	renderEditModal(){
		var cell = this.state.editingCell;
		var { grantInfo } = cell;
		var content = this.content();
		return <Modal title = {cell.key + " for " + content.name} visible = {true} isExtraLarge onClose = {() => {
			this.setState({editingCell: null});
		}}>
			<Input 
				label = "Rule"
				type = "select"
				name = "rule" 
				onChange = {(e) => {
					this.setState({dropdownType: e.target.value});
				}}
				value={this.state.dropdownType}
				defaultValue={this.state.dropdownType}
			>
				[
					<option value = {"inherited"}>
						Inherited
					</option>,
					<option value = {"never"}>
						Always denied
					</option>,
					<option value = {"always"}>
						Always granted
					</option>,
					<option value = {"custom"}>
						Custom rule
					</option>
				]
			</Input>
			{this.state.dropdownType == 'custom' && <Input inputRef={crRef => this.customRuleRef = crRef} defaultValue = {typeof grantInfo.value === 'string' ? grantInfo.value : ''} validate = {['Required']} label = "Custom Rule" type = "text" name = "customRule"/>}
			<Input type="button" onClick={(e) => {
				e.preventDefault();
				
				// Apply the change to grants now.
				var type = this.state.dropdownType;
				
				if(type === "inherited"){
					delete this.state.grants[cell.key];
				}else if(type === "never"){
					this.state.grants[cell.key] = false;
				}else if(type === "always"){
					this.state.grants[cell.key] = true;
				}else if(type === "custom"){
					this.state.grants[cell.key] = this.customRuleRef.value || "";
				}
				
				this.setState({
					editingCell: null
				});
				
			}}>
				Apply
			</Input>
		</Modal>
	}
	
	render() {
		if(this.props.editor) {
			return this.renderEditMode();
		}

		return this.renderList();
    }
	
}