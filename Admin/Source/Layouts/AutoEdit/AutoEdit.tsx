import { useTokens } from 'UI/Token';
import AutoForm from 'Admin/AutoForm';

interface AutoFormProps {
	// parent

	/**
	 * The entity type name as it appears in the API endpoint such as "User"
	 */
	contentType: string,

	/**
	 * The singular name of the content type such as "User".
	 */
	singular: string,

	/**
	 * The plural name of the content type such as "Users".
	 */
	plural: string,

	/**
	 * Optional previous page URL.
	 */
	previousPageUrl?: string,

	/**
	 * Optional previous page name.
	 */
	previousPageName?: string
}

const AutoEdit: React.FC<React.PropsWithChildren<AutoFormProps>> = ({ children, ...props }) => {
	return <AutoForm {...props} />;
};

export default AutoEdit;