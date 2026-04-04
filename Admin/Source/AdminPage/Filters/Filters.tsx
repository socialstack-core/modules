import Input from 'UI/Input';
import Popover from 'UI/Popover';
/**
 * Props for the Content component.
 */
interface AdminPageFiltersProps {
	searchText?: string,
	onInput?: () => void,
	onChange?: () => void,
	onFocus?: () => void,
	className?: string,
	children?: React.ReactNode,
	open?: boolean
}

/**
 * The Filters React component.
 * @param props React props.
 */
const AdminPageFilters: React.FC<React.PropsWithChildren<AdminPageFiltersProps>> = (props) => {
	const { searchText, onInput, onChange, onFocus, className, children, open } = props;
	const placeholder = props.placeholder || `Search`;

	const filterClasses = ["admin-page__filters"];

	if (className?.length) {
		filterClasses.push(className);
	}

	return (
		<Popover alignment="left" backgroundActive={true} underSubHeader={true} aboveFooter={true}
			className={filterClasses.join(' ')} method="manual" id="admin_filters" open={open === undefined ? true : open}>
			<fieldset className="admin-page__filters-fieldset fieldset--sm">
				{typeof onInput === 'function' && typeof onChange === 'function' && <>
					<Input
						type="search" noWrapper
						placeholder={placeholder}
						defaultValue={searchText}
						onInput={onInput}
						onChange={onChange}
						onFocus={onFocus}
					/>
				</>}

				<div className="admin-page__filters-internal">
					{children}
				</div>
			</fieldset>
		</Popover>
	);
}

// required for preact (without preact/compat layer) support
AdminPageFilters.displayName = "AdminPageFilters";

export default AdminPageFilters;
