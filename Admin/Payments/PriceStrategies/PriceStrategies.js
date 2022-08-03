import Input from 'UI/Input';


export default function PriceStrategies(props) {

	return <>
		<Input {...props} type='select'>
			<option value='0'>Standard. Purchase quantity is multiplied by the tier price. One more of something can be cheaper overall.</option>
			<option value='1'>Step once. </option>
			<option value='2'>Step always. One more of something is never cheaper overall.</option>
		</Input>
		<p>
			For more information on price strategies, <a href='https://wiki.socialstack.dev/index.php?title=Pricing_Strategies'>check the docs here</a>.
		</p>
	</>;
	
}
