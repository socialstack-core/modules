import {getAll} from "Admin/Functions/GetPropTypes";

/**
 * getTemplateComponents
 *
 * Retrieves React components from the global `window.__mm` object
 * that match specific template naming conventions (`UI/Templates`, `Admin/Templates`, `Email/Templates`).
 *
 * The function optionally filters results using a provided `predicate` function.
 *
 * Expected global structure:
 * ```
 * window.__mm = {
 *     "UI/Templates/Header": ReactComponent,
 *     "Admin/Templates/Dashboard": ReactComponent,
 *     "Email/Templates/Welcome": ReactComponent,
 *     ...
 * }
 * ```
 *
 * Features:
 * - Scans all keys in `window.__mm`.
 * - Identifies only keys starting with:
 *   - `"UI/Templates/"` (case-insensitive)
 *   - `"Admin/Templates/"` (case-insensitive)
 *   - `"Email/Templates/"` (case-insensitive)
 * - Supports an optional `predicate` to filter which template components are returned.
 *
 * Example:
 * ```
 * // Get all components
 * const all = getTemplateComponents();
 *
 * // Get only UI templates
 * const onlyUi = getTemplateComponents(name => name.toLowerCase().startsWith("ui/templates/"));
 * ```
 *
 * @param predicate Optional function to filter components by their key name.
 * @returns A record mapping matching keys to their React component functions.
 */
const getTemplateComponents = (
	predicate?: (templateName: string) => boolean
): Promise<Record<string, React.FC>> => {
	
	return new Promise((resolve, reject) => {
		const allComponents: Record<string, React.FC> = {};

		getAll().then((components) => {
			Object.keys(components.codeModules).forEach((key: string) => {
				const lowerKey = key.toLowerCase();

				// Identify template categories
				const isUiTemplate = lowerKey.startsWith("ui/templates/");
				const isAdminTemplate = lowerKey.startsWith("admin/templates/");
				const isEmailTemplate = lowerKey.startsWith("email/templates/");

				// Skip anything not in one of the known template categories
				if (!isUiTemplate && !isAdminTemplate && !isEmailTemplate) {
					return;
				}

				// Add to results if it passes the optional predicate
				if (!predicate || predicate(key)) {
					allComponents[key] = window.__mm[lowerKey];
				}
			})
		})
		.then(() => {
			resolve(allComponents);
		})
		.catch(reject)

	})
	
};


export {
	getTemplateComponents
};
