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


// ========================
// Editor assistance
// ========================
import { createEmptyRoot } from "Admin/CanvasEditor/Utils";

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

			if (_canvasNode && template) {
				// we're in the RTE and a canvas node has been provided. 
				// Can inform this node about any new roots(or removal of roots) as necessary.
				// To do that though, we need to know what the roots in this template even are.
				// Step through the template's canvas collecting all slots:

				const slots: CanvasNode[] = [];
				const slotsByKey: Record<string, CanvasNode> = {};
				expand(template.bodyJson ? JSON.parse(template.bodyJson) : {}, (node: CanvasNode) => {
					if (node && node.typeName == "Admin/Template/Slot" && node.props?.name) {
						slots.push(node);
						slotsByKey[node.props.name] = node;
					}
				});

				// For each root not named in slotsByKey, remove it.
				let changes = false;

				if (_canvasNode.roots) {
					for (var k in _canvasNode.roots) {
						if (!slotsByKey[k]) {
							delete _canvasNode.roots[k];
							changes = true;
						}
					}
				}

				// Add any new roots:
				for (var k in slotsByKey) {
					if (_canvasNode.roots && _canvasNode.roots[k]) {
						continue;
					}

					const slotInfo = slotsByKey[k];

					// All the config fields of the slot (component restrictions etc)
					const { props } = slotInfo;

					// Slot.tsx has chosenComponentGroup as a prop 
					// so props.chosenComponentGroup may be an array(it's optional though of course)

					// The root created will be in *the parent canvas* and is the root node of the tree of components
					// at the add site. Can do e.g. newRoot.props.componentsPermitted = [...props.chosenComponentGroup]; (with all necessary null checks)
					// to pass the config through.

					// In the editor itself then, whenever someone clicks add, you would have to check up the tree for
					// all parents of the location where add was clicked and collect their componentsPermitted sets, combining them with "and".
					// If all restriction sets are empty then all components are permitted (you must check this first, before combining the sets). 
					// POtherwise, combine the sets and permit only the "and" composite. It may permit nothing at all due to the way and works 
					// (this is why you'd check for 'none' vs. checking if this composite set is empty)
					const newRoot = createEmptyRoot();
					newRoot.props = props;
					_canvasNode.roots[k] = newRoot;
					changes = true;
				}

				if (changes) {
					// Tell the RTE the node changed.
					props._rte.redraw();
				}
			}
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
