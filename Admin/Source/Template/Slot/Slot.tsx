import {useState} from "react";
import useApi from "UI/Functions/UseApi";
import componentGroupApi from "Api/ComponentGroup";
import Loading from "UI/Loading";
import Alert from "UI/Alert";
import Link from "UI/Link";
import Button from "UI/Button";
import {setCustomPropEditor} from "Admin/CanvasEditor/PropEditor";
import {CustomComponentsInputProps} from "../types";
import {useRouter} from "UI/Router";
import Input from "UI/Input";

/**
 * Props for `AdminTemplateSlot`.
 */
export type AdminTemplateSlotProps = {
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
	 * What components are allowed inside this slot.
	 */
	chosenComponentGroup: string[];
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



setCustomPropEditor(
	/**
	 * This is for the slot component, it controls what components are and are not allowed
	 */
	'chosenComponentGroup',
	/**
	 * @param {CustomComponentsInputProps} props
	 */
	(props: CustomComponentsInputProps) => {

		const [refreshCounter, setRefreshCounter] = useState(0);
		
		const { pageState } = useRouter();
		
		const q = pageState?.query?.get("q");

		const [componentGroups] = useApi(() => {
			return componentGroupApi.list()
		}, [props.fieldName, refreshCounter])


		if (!componentGroups) {
			return (
				<Loading />
			)
		}

		if (componentGroups?.totalResults == 0 || componentGroups?.results?.length === 0) {
			return (
				<div className={'mb-3'}>
					<label className={'form-label ui-form-label'}>{`Chosen component group`}</label>
					<br />
					<Alert variant={'warning'}>
						{`No component groups defined `}
						<Link
							external
							href={'/en-admin/componentgroup/add'}
						>
							{`Create new group`}
						</Link>
					</Alert>
					<Button onClick={() => setRefreshCounter((prev) => prev + 1)}>&#128472; {`Refresh groups`}</Button>
				</div>
			)
		}

		return (
			<div className={'prop-editor allowed-components'}>
				<div className={'allowed-components-controls'}>
					<Link external href={'/en-admin/componentgroup/add'}>
						<Button>{`Create new group`}</Button>
					</Link>
					<Button onClick={() => setRefreshCounter((prev) => prev + 1)}>&#128472; {`Refresh groups`}</Button>
				</div>
				<Input 
					type={'select'}
					name={props.fieldName}
					label={props.niceName}
					defaultValue={props.value ?? ''}
					onChange={(ev) => {
						props.onChange(parseInt((ev.target as HTMLInputElement).value));
					}}
				>
					<option value={''}>{`Choose component group`}</option>
					{componentGroups?.results.map((componentGroup) => {
						return (
							<option value={componentGroup.id}>{componentGroup.name}</option>
						)
					})}
				</Input>
			</div>
		)
	}
)
