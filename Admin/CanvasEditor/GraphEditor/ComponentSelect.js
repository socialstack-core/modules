import Input from 'UI/Input';
import {niceName} from './Utils';
import getAutoForm, {getAllContentTypes} from 'Admin/Functions/GetAutoForm';
import {collectModules} from '../ModuleSelector/Utils';


var availableModuleGroups = null;

export default function ComponentSelect(props){
	
	if(!availableModuleGroups){
		availableModuleGroups = collectModules().standard;
	}
	
	var options = [<option key={'_'} value={''}>Select one..</option>];
	
	availableModuleGroups.forEach(moduleGroup => {
		var groupOptions = moduleGroup.modules.map(module => {
			
			return <option key={module.name} value={module.publicName}>{niceName(module.name)}</option>;
			
		});
		
		if(!moduleGroup.name){
			// UI group hides its name
			options = options.concat(groupOptions);
		}else{
			options.push(<optgroup key={moduleGroup.name} label={moduleGroup.name}>
				{groupOptions}
			</optgroup>);
		}
	});
	
	return <Input type='select' name={props.name} value={props.value} defaultValue={props.value} onChange={props.onChange}>
		{options}
	</Input>
}
