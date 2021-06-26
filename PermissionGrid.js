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
			grants: [],
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

	renderCell(currentGrantRule, grantJsonRuleValue){

		if((grantJsonRuleValue !== undefined && (grantJsonRuleValue === false || grantJsonRuleValue == 'false')) || (grantJsonRuleValue === undefined && (currentGrantRule === null || currentGrantRule === undefined || currentGrantRule === false))){
			// red x
			return <i className='fa fa-minus-circle' style={{color: 'red'}}/>;
		}
		else if((grantJsonRuleValue !== undefined && typeof(grantJsonRuleValue) === 'string' && grantJsonRuleValue != 'true' && grantJsonRuleValue != 'false') || (grantJsonRuleValue === undefined && currentGrantRule.ruleDescription && currentGrantRule.ruleDescription.length)) {
			
			var ruleDescription = grantJsonRuleValue || currentGrantRule.ruleDescription;
			
			// text assumed:
			return <div>
				<i className='fa fa-check' style={{color: 'orange'}}/>
				<p style={{fontSize: 'smaller'}}>
					{ruleDescription}
				</p>
			</div>
		}
		
		//tick
		return <i className='fa fa-check' style={{color: 'green'}}/>;
	}

	currentCellValue(currentGrantRule, grantJsonRuleValue){
		if((grantJsonRuleValue !== undefined && grantJsonRuleValue === false || grantJsonRuleValue == 'false') || (grantJsonRuleValue === undefined && (currentGrantRule === null || currentGrantRule === undefined || currentGrantRule === false))){
			// red x
			return false
		}else if((grantJsonRuleValue !== undefined && typeof(grantJsonRuleValue) === 'string' && grantJsonRuleValue != 'true' && grantJsonRuleValue != 'false') || (grantJsonRuleValue === undefined && currentGrantRule.ruleDescription && currentGrantRule.ruleDescription.length)) {			
			// text assumed:
			return grantJsonRuleValue || currentGrantRule.ruleDescription;
		}
		
		//tick
		return true;
	}
	
	
	renderEditMode(){

		if(!this.state.capabilities){
			return null;
		}

		var initialJson = this.state.updatedJson || this.props.value || this.props.defaultValue || "{}";
		
		initialJson = JSON.parse(initialJson);

		var currentEdit = null
		if(this.state.editingCell) {
			currentEdit = this.currentCellValue(this.state.editingCell.currentGrantRule, this.state.editingCell.currentJsonGrantRule)
		}

		return [
			<Input type='hidden' inputRef={ref => {
				this.ref = ref;
				if(ref){
					ref.onGetValue = () => {
						return JSON.stringify(this.state.grants);
					};
				}
			}} label={`Grants`} />,
            <div className={'permission-grid'}>
				<table className="table">
					<tr>
						<th>Capability</th>
						{this.state.roles.map(role => {
							if(this.props.currentContent.id == role.id) {
								return (
									<th>
										{role.name}
									</th>
								);
							}
						})}
					</tr>
					{this.state.capabilities.map(cap => {

						var map = {};
												
						if(cap.grants){
							cap.grants.forEach(grant => {
								map[grant.role.key] = grant;
							});
						}

						var currentGrantRule = map[this.props.currentContent.key];

						return (
							<tr>
								<td>
									{cap.key}
								</td>
								<td onClick = {() => {
									this.setState({
										editingCell: {
											key: cap.key,
											currentGrantRule,
											currentJsonGrantRule: initialJson[cap.key]
										}
									});

									// We also need to trigger the modal to open if this is a custom rule, so lets do a quick check:
									if(typeof(this.currentCellValue(currentGrantRule, initialJson[cap.key])) === 'string') {
										// its a custom grant, let's let the modal know.
										this.setState({customRule: true});
									}

								}}>
									{this.renderCell(currentGrantRule, initialJson[cap.key])}
								</td>
							</tr>);
					})}
				</table>

				{this.state.editingCell && <Modal title = {this.state.editingCell.key + " for " + this.props.currentContent.name} visible = {true} isExtraLarge onClose = {() => {
					this.setState({editingCell: null, customRule: false});
				}}>
					<Form
						action = {"role/"+ this.props.currentContent.id}
						onValues = {values => {
							// Let's grab the json for this role
							var valueJson = initialJson;

							valueJson[this.state.editingCell.key] = values.rule;

							if(values.rule == "custom") {
								valueJson[this.state.editingCell.key] = values.customRule;
							}

							values = {};
							values.grantRuleJson = JSON.stringify(valueJson);
								
							return values;
						}}
						onSuccess = {(response) => {
							// Print out response.
							this.setState({updatedJson: response.grantRuleJson, editingCell: null});
						}}
						onFailed = {(error) => {
							this.setState({updateFail: error})
						}}
					>
						<Input 
							label = "Rule"
							type = "select"
							name = "rule" 
							onChange = {(e) => {
								if(e.target.value == "custom" != this.state.customRule) { // if check to avoid needless updating
									this.setState({customRule: e.target.value == "custom"}); // if custom rule, render custom rule input
								}
								
							}}
						>
							[
								<option value = {false} selected={currentEdit === false}>
									Always denied
								</option>,
								<option value = {true} selected={currentEdit === true}>
									Always granted
								</option>,
								<option value = {"custom"} selected={typeof(currentEdit) === "string"}>
								 	Custom rule
								</option>
							]
						</Input>
						{this.state.customRule && <Input defaultValue = {currentEdit} validate = {['Required']} label = "Custom Rule" type = "text" name = "customRule"/>}
						<Input type = "submit">Update and close</Input>
					</Form>
					{this.state.updateFail && <Alert type = "error">{"An error has occurred: " + this.state.updateFail}</Alert>}
				</Modal>}
			</div>
		];
	}

	render() {
		if(this.props.editor) {
			return this.renderEditMode();
		}

		return this.renderList();
    }
	
}