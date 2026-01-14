import Default, { DefaultInputType } from 'UI/Input/Default';

type SearchInputType = DefaultInputType & {
	hideIcon?: boolean
};

// Registering 'search' as being available
declare global {
	interface InputPropsRegistry {
		'search': SearchInputType;
	}
}

const Search: React.FC<CustomInputTypeProps<"search">> = (props) => {
	const { field, helpFieldId, onInputRef, inputRef, validationFailure } = props;
	const { hideIcon, onChange, className, ...attribs } = field;

	let fieldMarkup: React.ReactNode;

	fieldMarkup = <Default type="search" className={hideIcon ? "search-input--no-icon" : undefined} config={props} field={field} />;

	return fieldMarkup;
}

window.inputTypes['search'] = Search;
export default Search;