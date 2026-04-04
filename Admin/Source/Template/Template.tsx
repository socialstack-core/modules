// ========================
// React Imports
// ========================
import { useEffect, useState } from "react";

// ========================
// API & Cache Imports
// ========================
import { Template } from "Api/Template";
import { getTemplateByKey } from "Admin/Template/Cache";

// ========================
// UI Imports
// ========================
import Canvas from "UI/Canvas";
import Loading from "UI/Loading";

// ========================
// Types
// ========================
import { expand, CanvasNode } from "UI/Functions/CanvasExpand";

export type AdminTemplateProps = React.PropsWithChildren & {
	templateKey: string;
	_canvasNode?: CanvasNode;
};

/**
 * AdminTemplate Component
 *
 * Loads and renders an admin-facing template from the template cache by its `templateKey`.
 *
 * Features:
 * - Fetches the `Template` object from cache or API using `getTemplateByKey`.
 * - Displays a loading spinner until the template is loaded.
 * - Renders template content via the `Canvas` component.
 * - Injects `_templateRoots` into `Admin/Template/Slot` nodes during canvas rendering.
 *
 * Example:
 * ```
 * <AdminTemplate templateKey="site_default" />
 * ```
 */
const AdminTemplate: React.FC<AdminTemplateProps> = (props) => {
	const { templateKey, _canvasNode } = props;

	// ========================
	// State
	// ========================
	const [template, setTemplate] = useState<Template | null>();
	const [error, setError] = useState<boolean>();

	// ========================
	// Effects
	// ========================
	// Fetch template when `templateKey` changes
	useEffect(() => {
		templateKey && getTemplateByKey(templateKey, template => {
			setTemplate(template);
			setError(!template);
		});
	}, [templateKey]);

	// ========================
	// Render
	// ========================
	if (!templateKey) {
		return `Please specify a template to use.`;
	}

	if (error) {
		return `The template "${templateKey}" doesn't exist.`;
	}

	if (!template) {
		return <Loading />;
	}

	return (
		<Canvas
			onRenderNode={(node: CanvasNode) => {
				// Intercept Canvas nodes to inject template root props
				if (node.typeName === "Admin/Template/Slot") {
					if (!node.props) {
						node.props = {};
					}
					node.props._templateRoots = props;
				}

				return node;
			}}
		>
			{template.bodyJson}
		</Canvas>
	);
};

export default AdminTemplate;
