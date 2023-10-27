import MultiSelect from 'Admin/MultiSelect';

export default function ArrayEditor(props){
	var inputProps = props.props;
	var arrayOfType = inputProps.customMeta.of;
	var searchField = inputProps.customMeta.field;
	
	return <div>
		<MultiSelect defaultValue={inputProps.defaultValue} contentType={arrayOfType} label={arrayOfType} hideLabel field={searchField} {...props} />
	</div>;
}