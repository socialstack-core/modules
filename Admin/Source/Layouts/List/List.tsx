import AutoList, { AutoListProps } from 'Admin/AutoList';

export interface ListProps extends AutoListProps {
	noCreate?: boolean;
}

const List: React.FC<React.PropsWithChildren<ListProps>> = (props): React.ReactNode => {

	let { contentType, singular, plural, children, ...listProps } = props;

	let textPlural = plural;

	if (!props.columns || !Array.isArray(props.columns)) {
		return 'No fields to list';
	}

	const acceptedFields: string[] = ['id', 'title', 'name', 'email', 'username', 'description' , 'reference'];

	const defSearchFields: string[] = props.columns
		.filter(field => field.isSearchable || acceptedFields.includes(field.field))
		.map(field => field.field);

	if (!defSearchFields.length) {
		defSearchFields.push('id');
	}

	return (
		<AutoList 
			contentType={contentType} 
			singular={singular}
			plural={plural}
			{...listProps}
			title={`Edit or create ${textPlural}`}
			create={!props.noCreate}
			searchFields={props.searchFields || defSearchFields} 
		/>
	)

}

export default List;