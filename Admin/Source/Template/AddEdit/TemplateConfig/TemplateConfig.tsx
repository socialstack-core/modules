// ========================
// API Imports
// ========================
import TemplateApi, { Template } from "Api/Template";

// ========================
// UI Imports
// ========================
import Input from "UI/Input";
import Alert from "UI/Alert";
import TemplateTypeSelector from "Admin/Template/TemplateTypeSelector";

// ========================
// React Imports
// ========================
import { useEffect, useState } from "react";

// ========================
// Utility Imports
// ========================
import { ApiList } from "UI/Functions/WebRequest";
import { getTemplateComponents } from "Admin/Template/Functions";
import {CanvasNode} from "Admin/Template/AddEdit";

// ========================
// Types
// ========================
export type AddEditTemplateConfigProps = {
	existing?: Template;
	onParentChange?: (parent: Template | string) => void;
};

// ========================
// Constants
// ========================
const DEFAULT_TEMPLATE = "UI/Templates/BaseWebTemplate";

/**
 * AddEditTemplateConfig Component
 *
 * Provides configuration controls for selecting a template type, base template,
 * and optional parent template when creating or editing a `Template`.
 *
 * Features:
 * - Dynamically loads available base template components based on selected type.
 * - Fetches possible parent templates for the chosen base template.
 * - Displays error messages if fetching fails.
 *
 * Behavior:
 * - Template types:
 *   1 → Web (UI or Admin templates)
 *   2 → Email templates
 *   3 → PDF templates
 */
const AddEditTemplateConfig: React.FC<AddEditTemplateConfigProps> = ({
		 existing,
 	 }) => {
	
	const isExistingTemplate = !!existing?.id;
	
 	// ========================
 	// Derived
 	// ========================
 	const bodyJson: CanvasNode = JSON.parse(existing?.bodyJson ?? '{}') ?? {};
	
	// ========================
	// State
	// ========================
	
	// a collection of possible parents based off the chosen template file
	const [possibleParents, setPossibleParents] = useState<ApiList<Template>>();
	// simple error state
	const [error, setError] = useState<string>();
	// chooses the template type
	const [templateType, setTemplateType] = useState<uint>(
		existing?.templateType ?? (1 as uint)
	);
	const [templateComponents, setTemplateComponents] = useState<Record<string, React.FC>>();

	// ========================
	// Effects
	// ========================

	useEffect(() => {
		getTemplateComponents((key) => {
			const lowerKey = key.toLowerCase();

			if (templateType === 1) {
				// Web templates (UI/Admin)
				return (
					lowerKey.startsWith("ui") || lowerKey.startsWith("admin")
				);
			}

			if (templateType === 2) {
				// Email templates
				return lowerKey.startsWith("email");
			}

			if (templateType === 3) {
				// PDF templates
				return lowerKey.startsWith("pdf");
			}

			return false;
		})
		.then((result) => setTemplateComponents(result))
		.catch((error) => setError(error.message ?? error))
			.catch((error) => console.log(error));
	}, [templateType]);

	useEffect(() => {
		if (!possibleParents?.results) {
			TemplateApi.list()
				.then(setPossibleParents)
				.catch((error) => setError(error.message ?? error))
				.catch(console.error);
		}
	}, [possibleParents]);
	
	// try and find the value.
	let templateValue: string | int = bodyJson.t;
	
	// load the value from the template
	if (bodyJson.d && bodyJson.d.templateKey) {
		templateValue = possibleParents?.results.find(parent => parent.key == bodyJson?.d?.templateKey)!.id!;
	}
	
	// ========================
	// Render
	// ========================
	return (
		<div className="template-config">
			{/* Error message */}
			{error && <Alert variant="danger">{error}</Alert>}

{/* Template type selector */}
			<TemplateTypeSelector
				label="Template type"
				name="templateType"
				value={templateType}
				onChange={(e) => {
					setTemplateType(
						parseInt(
							(e.target as HTMLSelectElement).value
						) as int
					);
				}}
			/>
			{!isExistingTemplate && (
			<Input 
				type={'select'}
				label={`Parent template`}
				name="bodyJson"
				defaultValue={existing?.bodyJson}
			>
				<optgroup label={`Basic templates`}/>
				{Object.keys(templateComponents ?? {}).map((component) => {
					return (
						<option value={JSON.stringify({ t: component })} key={component}>{component}</option>
					)
				})}
				<optgroup label={`Existing templates`}/>
				{possibleParents?.results?.map((template: Template) => {
					return (
						<option value={JSON.stringify({ t: "Admin/Template", d: { templateKey: template.key } })}>{template.title}</option>
					)
				})}
			</Input>
			)}
			
		</div>
	);
};

export default AddEditTemplateConfig;
