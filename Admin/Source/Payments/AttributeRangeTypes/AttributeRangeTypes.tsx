import Input from 'UI/Input';

export interface AttributeRangeTypesProps {
	name?: string,
	value?: string,
	label?: string,
	defaultValue?: string
}

export default function AttributeRangeTypes(props: AttributeRangeTypesProps) {

	return <Input {...props} type='select'>
		<option value='0'>This attribute does not range</option>
		<option value='1'>This attribute always ranges (e.g. min - max)</option>
		<option value='2'>This attribute ranges sometimes (e.g. "min - max" or just max are acceptable)</option>
	</Input>;
	
}
