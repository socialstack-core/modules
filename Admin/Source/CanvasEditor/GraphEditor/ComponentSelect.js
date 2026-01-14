import Input from 'UI/Input';
import Loading from 'UI/Loading';
import {niceName} from './Utils';
import getAutoForm, {getAllContentTypes} from 'Admin/Functions/GetAutoForm';
import {collectModules} from '../ModuleSelector/Utils';
import {useEffect, useState} from 'react';


export default function ComponentSelect(props){
	
	const [componentSet, setComponentSet] = useState();
	
	useEffect(() => {
		
		collectModules(props.componentGroups)
		.then(set => {
			setComponentSet(set);
		});
		
	}, [props.componentGroups]);
	
	if(!componentSet){
		return <Loading />;
	}
	
	var options = [<option key={'_'} value={''}>Select one..</option>];
	
	var groupOptions = componentSet.modules.map(module => {
		
		return <option key={module.name} value={module.publicName}>{niceName(module.name)}</option>;
		
	});
	
	options = options.concat(groupOptions);
	
	return <Input type='select' name={props.name} value={props.value} defaultValue={props.value} onChange={props.onChange}>
		{options}
	</Input>
}
