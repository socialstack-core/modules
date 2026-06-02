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

	let fullClassName = hideIcon ? "search-input--no-icon" + (className ? " " + className : "") : className;

	fieldMarkup = <Default type="search" config={props} field={{ ...field, className: fullClassName }} />;

	return fieldMarkup;
}

window.inputTypes['search'] = Search;
export default Search;