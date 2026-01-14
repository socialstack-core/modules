import Input from 'UI/Input';

export interface AttributeTypesProps {
	name?: string,
	value?: string,
	label?: string,
	defaultValue?: string
}

export default function AttributeTypes(props: AttributeTypesProps) {

	// 1=long, 2=double, 3=string, 4=image ref, 5=video ref, 6=file ref, 7=boolean

	return <Input {...props} type='select'>
		<option value=''>{`Please select`}</option>
		<option value='1'>{`Whole number`}</option>
		<option value='2'>{`Decimal number`}</option>
		<option value='3'>{`Text`}</option>
		<option value='4'>{`An image`}</option>
		<option value='5'>{`A video`}</option>
		<option value='6'>{`Any other file`}</option>
		<option value='7'>{`Yes/no`}</option>
	</Input>;

}
