import webRequest from 'UI/Functions/WebRequest';
import Input from 'UI/Input';
import omit from 'UI/Functions/Omit';
import { useSession } from 'UI/Session';

export default function(props) {
	
	const { defaultValue, optionsArePrices, placeHolder } = props;
	const [options, setOptions] = React.useState();

	var { session } = useSession();
    var currencyLocale = session && session.currencylocale ? session.currencylocale : null;

	React.useEffect(() => {
		var locale = (optionsArePrices && currencyLocale) ? currencyLocale.id : null;

		webRequest(
			"customContentTypeSelectOption/list", 
			{
				where: {
					customContentTypeFieldId: this.props.field
				},
				sort: { field: 'order' }
			},
			locale ? {
				locale: locale
			}: null
		)
			.then(response => {
				if (response && response.json && response.json.results) {
					var results = response.json.results;

					results.unshift({ value: null });
					setOptions(results);
				}
			}).catch(e => {
				console.error(e);
			});
	}, []);

	return <div className="custom-field-select">
			<Input {...omit(props, ['contentType'])} type="select">{
				options ? options.map(option => {
					if(!option || !option.value){
						return <option value={null}>{placeHolder ? placeHolder : "None"}</option>;
					}
					
					return <option value={option.value}>{
						option.value
					}</option>;
				}) : defaultValue
				? [<option value={defaultValue}>{
						defaultValue
					}</option>] 
				: []
			}</Input>
		</div>;
	
}