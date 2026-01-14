import Input from 'UI/Input';


export default function Availability(props) {

	return <Input {...props} type='select'>
			<option value='0'>Yes</option>
			<option value='1'>Pre-order</option>
			<option value='2'>No (permanently)</option>
			<option value='3'>No (awaiting stock)</option>
		</Input>;
	
}
