import Input from 'UI/Input';
import {niceName} from './Utils';
import getAutoForm, {getAllContentTypes} from 'Admin/Functions/GetAutoForm';


export default function ContentTypeSelect(props){
	
	var {primaryType} = props;
	var [contentTypes, setContentTypes] = React.useState(null);
	
	React.useEffect(() => {
		
		getAllContentTypes().then(ct => {
			setContentTypes(ct);
		});
		
	}, []);
	
	if(!contentTypes){
		return 'Getting types..';
	}
	
	var options = [<option key={'_'} value={''}>Select one..</option>];
	
	if(primaryType){
		options.push(
			<option key={'primary'} value={'primary'}>{`Primary type (${primaryType})`}</option>
		);
	}

	// sort content types alphabetically
	contentTypes = Object.keys(contentTypes).sort().reduce(
		(obj, key) => {
			obj[key] = contentTypes[key];
			return obj;
		},
		{}
	);
	
	for(var contentTypeKey in contentTypes){
		var ctInfo = contentTypes[contentTypeKey];
		options.push(<option key={contentTypeKey} value={contentTypeKey}>{niceName(ctInfo.name)}</option>);
	}
	
	return <Input type='select' name={props.name} value={props.value} defaultValue={props.value} onChange={props.onChange}>
		{options}
	</Input>
}
