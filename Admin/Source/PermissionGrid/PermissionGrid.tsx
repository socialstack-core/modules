import Input from 'UI/Input';
import Modal from 'UI/Modal';
import { PermissionApi as permissionsApi, PermissionMeta, GrantMeta } from 'Api/Permissions';
import { useState, useEffect, useRef } from 'react';
import { CodeModuleType, getAll, getEntities } from 'Admin/Functions/GetPropTypes';
import Loading from 'UI/Loading';
import EntityFieldRuleEditor from 'Admin/EntityFieldRuleEditor';
import { Role } from 'Api/Role';

interface PermissionGridProps {
	editor: boolean;
	defaultValue?: string;
	value?: string;
	onToggleViewAll?: (value: boolean) => void,
	entities?: CodeModuleType[],
	currentContent: Role,
	name?: string
}

const alphaSort = (a: string, b: string) => {
    const firstCharA = a.toLowerCase();
    const firstCharB = b.toLowerCase();
    if (firstCharA < firstCharB) return -1;
    if (firstCharA > firstCharB) return 1;
    return 0;
}

type DropdownType = "inherited" | "always" | "never" | "custom";

type GrantInfo = {
	inherited: boolean,
	value: string | boolean
};

type EditingCell = {
	key: string,
	grantInfo: GrantInfo
};

/**
 * A grid of capabilities and the roles they're active on
 */
const PermissionGrid: React.FC<React.PropsWithChildren<PermissionGridProps>> = (props) => {

	const [roles, setRoles] = useState<Role[]>([]);
	const [customRuleEle, setCustomRuleEle] = useState<HTMLInputElement | null>(null);
	const [capabilities, setCapabilities] = useState<PermissionMeta[]>([]);
	const [filteredCapabilities, setFilteredCapabilities] = useState<PermissionMeta[]>([]);
	const [filter, setFilter] = useState('');
	const [grants, setGrants] = useState<Record<string, string | boolean> | null>(null);
	const [dropdownType, setDropdownType] = useState<DropdownType | null>(null);
	const [editingCell, setEditingCell] = useState<EditingCell | null>(null);
	const [entities, setEntities] = useState<CodeModuleType[]>();

	useEffect(() => {
		permissionsApi.list().then(permissionInfo => {

			if (props.editor) {
				var val = props.value || props.defaultValue || '';

				var grants : Record<string, string | boolean> = {};

				if (val) {
					try {
						grants = JSON.parse(val);

						if (!grants || Array.isArray(grants)) {
							grants = {};
						}
					} catch (e) {
						console.log("Bad grant json: ", e);
					}
				}

				setGrants(grants);
			}

			setRoles(permissionInfo.roles || []);
			setCapabilities(permissionInfo.capabilities || []);
			setFilteredCapabilities(permissionInfo.capabilities || []);
		});

	}, [props.editor, props.value, props.defaultValue]);

	useEffect(() => {

		if (!entities)
		{
			if (props.entities)
			{
				setEntities(props.entities);
			}
			else
			{
				getEntities().then(setEntities);
			}
		}

	}, [entities, props.entities])

	const updateFilter = (filter : string) => {
		setFilter(filter);
		setFilteredCapabilities(
			capabilities.filter((capability) => (capability.key || '').toLowerCase().startsWith(filter.toLowerCase()))
		);
	}

	const clearFilter = () => {
		setFilter('');
		setFilteredCapabilities(capabilities);
	}

	const renderFilter = () => {
		return <>
			<th>
				<div className="admin_permission-grid__filter">
					<label htmlFor="permission_filter" className="col-form-label">
						{`Capability`}
					</label>
					<div className="admin_permission-grid__filter-field input-group">
						<select
							className="form-control" 
							id="permission_filter"
							defaultValue={filter}
							onChange={(ev) => {
								const el: HTMLSelectElement = ev.target;

								if (el.value.length == 0)
								{
									clearFilter();
								}
								else
								{
									updateFilter(el.value);
								}
							}}
						>
							<option value=''>{`No filter`}</option>
							{entities?.sort((a,b) => alphaSort(a.instanceName!, b.instanceName!))
									  .map(entity => {
											return (
												<option value={entity.instanceName}>{entity.instanceName}</option>
											)
									   })
							}
						</select>
						
					</div>
				</div>
			</th>
		</>;
	}

	const renderList = () => {
		if (!capabilities) {
			return null;
		}

		filteredCapabilities.sort(function (a, b) {
			const keyA = a.key || '';
			const keyB = b.key || '';
			if (keyA < keyB) return -1;
			if (keyA > keyB) return 1;
			return 0;
		});

		let noMatches = (!filteredCapabilities || filteredCapabilities.length == 0) && capabilities.length > 0;

        return (
            <div className={'admin_permission-grid'}>
				<table className="table table-striped">
					<thead>
						<tr>
							{renderFilter()}
							{roles.map(role => {
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
								<td colSpan={roles.length + 1}>
									<span className="admin_permission-grid--nomatch">
										{`No matching capabilities`}
									</span>									
								</td>
							</tr>
						</>}

						{filteredCapabilities.map(cap => {
							var map : Record<string, GrantMeta> = {};

							if (cap.grants) {
								cap.grants.forEach(grant => {
									const key = grant.role?.key || '';
									map[key] = grant;
								});
							}

							return (
								<tr>
									<td>
										{cap.key}
									</td>
									{roles.map(role => {
										var grant = map[role?.key || ''];

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
								<td colSpan={roles.length + 1}>
								abc
									{!filter || filter.length == 0 && <>
										x
										{`Displaying ${capabilities.length} capabilities`}
									</>}
									{filter && filter.length > 0 && <>
										xx
										{`Displaying ${filteredCapabilities.length} of ${capabilities.length} capabilities`}
									</>}
								</td>
							</tr>
						</tfoot>
					</>}
				</table>
            </div>
        );
	}

	const renderCell = (grantInfo : GrantInfo) => {

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

	const getGrantInfo = (capability : PermissionMeta) => {
		var content = getContent();
		var capGrant = null; // Not granted is the default
		if(capability.grants){
			var grantSet = capability.grants;
			for(var i=0;i<grantSet.length;i++){
				if(grantSet[i].role?.key == content.key){
					capGrant = grantSet[i];
					break;
				}
			}
			
		}
		
		var current : GrantInfo = {
			inherited: true,
			value: false
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

		const key = capability.key || '';

		if(grants && grants[key] !== undefined){
			current.inherited = false;
			current.value = grants[key];
		}
		
		return current;
	}
	
	const getContent = () => {
		return props.currentContent || {};
	}
	
	const renderEditMode = () => {

		if(!grants || !filteredCapabilities){
			return null;
		}
		
		filteredCapabilities.sort(function (a, b) {
			const keyA = a.key || '';
			const keyB = b.key || '';
			if (keyA < keyB) return -1;
			if (keyA > keyB) return 1;
			return 0;
		});

		let noMatches = (!filteredCapabilities || filteredCapabilities.length == 0) && capabilities.length > 0;

		return [
			<Input type='hidden' onInputRef={ref => {
				if(ref){
					(ref as any).onGetValue = () => {
						return JSON.stringify(grants);
					};
				}
			}} label={`Grants`} name={props.name} />,
			<div className={'admin_permission-grid'}>
				<table className="table table-striped">
					<thead>
					<tr>
						{renderFilter()}
						{roles.map(role => {
							if(getContent().id == role.id) {
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
								<td colSpan={2}>
									<span className="admin_permission-grid--nomatch">
										{`No matching capabilities`}
									</span>
								</td>
							</tr>
						</>}

					{filteredCapabilities.map(cap => {
						
						// Is it overriden? If yes, we have a custom value (otherwise it's inherited).
						var grantInfo = getGrantInfo(cap);
						
						return (
							<tr>
								<td>
									{cap.key}
								</td>
								<td onClick = {() => {
									setEditingCell({
										key: cap.key || '',
										grantInfo
									});
									setDropdownType(getDropdownType(grantInfo));
								}}>
									{renderCell(grantInfo)}
								</td>
							</tr>);
					})}
					</tbody>
					{!noMatches && <>
						<tfoot>
							<tr>
								<td colSpan={2} >
									{filter.length == 0 && <>
										{`Displaying ${capabilities.length} capabilities`}
									</>}
									{filter.length > 0 && <>
										{`Displaying ${filteredCapabilities.length} of ${capabilities.length} capabilities`}
									</>}
								</td>
							</tr>
						</tfoot>
					</>}
				</table>

				{editingCell && renderEditModal(editingCell)}
			</div>
		];
	}
	
	const getDropdownType = (grantInfo : GrantInfo) => {
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
	
	const renderEditModal = (editingCell : EditingCell) => {
		var { grantInfo } = editingCell;
		var content = getContent();

		return <Modal title={editingCell.key + " for " + content.name} visible = {true} isExtraLarge onClose = {() => {
			setEditingCell(null);
		}}>
			<Input 
				label = "Rule"
				type = "select"
				name = "rule" 
				onChange={(e) => {
					setDropdownType((e.target as HTMLSelectElement).value as DropdownType);
				}}
				defaultValue={dropdownType || ''}
			>
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
			</Input>
			{dropdownType == 'custom' && <Input onInputRef={(el: HTMLElement) => setCustomRuleEle(el as HTMLInputElement)} defaultValue = {typeof grantInfo.value === 'string' ? grantInfo.value : ''} validate = {['Required']} label = "Custom Rule" type = "text" name = "customRule"/>}
			<Input type="button" onClick={(e) => {
				e.preventDefault();

				if (!editingCell || !grants) {
					return;
				}

				// Apply the change to grants now.
				var type = dropdownType;
				
				if(type === "inherited"){
					delete grants[editingCell.key];
				}else if(type === "never"){
					grants[editingCell.key] = false;
				}else if(type === "always"){
					grants[editingCell.key] = true;
				}else if(type === "custom"){
					grants[editingCell.key] = customRuleEle?.value || "";
				}
				
				setEditingCell(null);
				
			}}>
				Apply
			</Input>
		</Modal>
	}
	
	if(props.editor) {
		return (
			<>
				{renderEditMode()}
				{filter.length != 0 && 
					<div className='field-rules'>
						<EntityFieldRuleEditor role={props.currentContent} key={filter} entity={filter}/>
					</div>
				}
			</>
		);
	}

	return (
		<>
			{renderList()}
			{filter.length != 0 && 
				<div className='field-rules'>
					<EntityFieldRuleEditor role={props.currentContent} key={filter} entity={filter}/>
				</div>
			}
		</>
	);
}


export default PermissionGrid;