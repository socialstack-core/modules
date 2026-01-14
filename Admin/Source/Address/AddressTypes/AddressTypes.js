import Input from 'UI/Input';

export interface AddressTypesProps {
	name?: string,
	value?: string,
	label?: string,
	defaultValue?: string
}

export default function AddressTypes(props: AddressTypesProps) {
	
	return <Input {...props} type='select'>
		<option value='0'>{`Payment`}</option>
		<option value='1'>{`Business`}</option>
	</Input>;

}
