import Input from 'UI/Input';


export default function TaxExempt(props) {

	return <Input {...props} type='select'>
			<option value='0'>No</option>
			<option value='1'>Yes</option>
			<option value='2'>Eligible Only</option>
		</Input>;
	
}
