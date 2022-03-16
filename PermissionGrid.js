import webRequest from 'UI/Functions/WebRequest';
import Input from 'UI/Input';
import Modal from 'UI/Modal';
import Form from 'UI/Form';
import Alert from 'UI/Alert';

/**
 * A grid of capabilities and the roles they're active on
 */
export default class PermissionGrid extends React.Component {
	
	constructor(props){
		super(props);
		this.state={
			roles: [],
			capabilities: [],
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
			this.setState(permissionInfo.json);
		});
	}

	renderList() {
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

		if(!this.state.grants || !this.state.capabilities){
			return null;
		}
		
		return [
			<Input type='hidden' inputRef={ref => {
				this.ref = ref;
				if(ref){
					ref.onGetValue = () => {
						return JSON.stringify(this.state.grants);
					};
				}
			}} label={`Grants`} name={this.props.name} />,
            <div className={'permission-grid'}>
				<table className="table">
					<tr>
						<th>Capability</th>
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
					{this.state.capabilities.map(cap => {
						
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