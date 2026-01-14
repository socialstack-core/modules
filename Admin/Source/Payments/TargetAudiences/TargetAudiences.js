import Input from 'UI/Input';

export interface TargetAudiencesProps {
	name?: string,
	value?: string,
	label?: string,
	defaultValue?: string
}

export default function TargetAudiences(props: TargetAudiencesProps) {
	
	return <Input {...props} type='select'>
		<option value='0'>{`Public web`}</option>
		<option value='1'>{`Internal`}</option>
		<option value='2'>{`Development`}</option>
	</Input>;

}
