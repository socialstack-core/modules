// ========================
// API Imports
// ========================
import { Template } from "Api/Template";

// ========================
// UI Imports
// ========================
import Input from "UI/Input";

// ========================
// Types
// ========================
export type AddEditTemplateInfoProps = {
	existing?: Template;
};

/**
 * AddEditTemplateInfo Component
 *
 * Renders the "Template Information" form section for creating or editing a template.
 *
 * Features:
 * - Pre-fills fields when `existing` template data is provided.
 * - Supports editing `key`, `title`, and `description` fields.
 *
 */
const AddEditTemplateInfo: React.FC<AddEditTemplateInfoProps> = ({ existing }) => {
	return (
		<div className="template-info">
			{/* Template Key */}
			<Input
				type="text"
				label="Key"
				name="key"
				defaultValue={existing?.key}
				readOnly={Boolean(existing?.key)}
			/>

			{/* Template Title */}
			<Input
				type="text"
				label="Title"
				name="title"
				defaultValue={existing?.title}
			/>

			{/* Template Description */}
			<Input
				type="textarea"
				label="Description"
				name="description"
				defaultValue={existing?.description}
			/>
		</div>
	);
};

export default AddEditTemplateInfo;
