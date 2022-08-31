import Input from 'UI/Input';


export default function BillingFrequencies(props) {

	return <Input {...props} type='select'>
			<option value='0'>One off</option>
			<option value='1'>Weekly</option>
			<option value='2'>Monthly</option>
			<option value='3'>Quarterly</option>
			<option value='4'>Yearly</option>
		</Input>;
	
}
