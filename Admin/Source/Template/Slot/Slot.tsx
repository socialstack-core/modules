import {useState} from "react";
import useApi from "UI/Functions/UseApi";
import componentGroupApi from "Api/ComponentGroup";
import Loading from "UI/Loading";
import Alert from "UI/Alert";
import Link from "UI/Link";
import Button from "UI/Button";
import {useRouter} from "UI/Router";
import Input from "UI/Input";

/**
 * Props for `AdminTemplateSlot`.
 */
export interface AdminTemplateSlotProps {
	/**
	 * The template roots object injected into Canvas nodes.
	 * Typically contains all top-level templates/components keyed by name.
	 * The underscore makes it an internal (private) field, hiding it from the editor itself.
	 */
	_templateRoots?: Record<string, React.ReactNode>;

	/**
	 * The name of the slot to render.
	 * Used to look up the content from `templateRoots`.
	 */
	name: string;

	/**
	 * Optional component restrictions for components that can appear in this slot.
	 * @module Admin/ComponentGroup
	 */
	componentGroups?: string[];
};

/**
 * AdminTemplateSlot Component
 *
 * This component renders a named "slot" from the templateRoots object.
 * It is commonly used within `AdminTemplate` Canvas rendering to populate
 * dynamic content areas.
 *
 * If no content exists for the given slot name, it displays a fallback
 * message `"Empty Slot"`.
 */
const AdminTemplateSlot: React.FC<AdminTemplateSlotProps> = (props) => {
	const { _templateRoots, name } = props;

	// Retrieve the slot content by name
	const slotContent = _templateRoots ? _templateRoots[name] : null;

	// Render the slot content or a default fallback
	return slotContent || `Empty Slot`;
};

export default AdminTemplateSlot;