import Input, { InputProps } from 'UI/Input';

const TemplateTypeSelector: React.FC<Omit<InputProps<"select">, 'type'>> = (props) => {

	return <Input {...props} type="select">
		<option value={1}>Web</option>
		<option value={2}>Email</option>
		<option value={3}>PDF</option>
	</Input>;

}

export default TemplateTypeSelector; 
