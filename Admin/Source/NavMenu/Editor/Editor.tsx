import Input from 'UI/Input';
import { IconRef } from 'UI/Icon';
import FileSelector from 'UI/FileSelector';
import ContentListEditor, { ContentListEditorNode, ContentDialogMode } from 'Admin/ContentListEditor';

interface NavMenuItem extends ContentListEditorNode {
	id: string;
	label: string;
	iconRef?: string;
	target?: string;
	openIn?: 'current' | 'newtab';
	popoverTarget?: string;
	hideLabelMobile?: boolean;
	hideMobile?: boolean;
}

interface EditorProps {
	name?: string;
	value?: string;
	defaultValue?: string;
	label?: string;
	hideLabel?: boolean;
	readonly?: boolean;
}

const generateId = (): string => {
	return Math.random().toString(36).substring(2, 11);
};

const parseItems = (value: string): NavMenuItem[] => {
	var parsed = JSON.parse(value);
	return parsed.items || [];
};

const serializeItems = (items: NavMenuItem[]): string => {
	return JSON.stringify({ items });
};

const createItem = (): NavMenuItem => {
	return {
		id: generateId(),
		label: '',
		target: ''
	};
};

const renderTitle = (item: NavMenuItem, dragHandle: React.ReactNode) => (
	<div className="nav-menu-item-title">
		{dragHandle}
		{item.iconRef && (
			<span className="nav-menu-item-icon">
				<IconRef fileRef={item.iconRef as FileRef} />
			</span>
		)}
		<span className="nav-menu-item-label">
			{item.label || `Untitled`}
		</span>

		{/* URL target */}
		{!!item.target?.length && (
			<span className="nav-menu-item-target">
				<i className="fal fa-fw fa-link"></i> {item.target}
			</span>
		)}
		{!!item.target?.length && item.openIn === 'newtab' && (
			<span className="nav-menu-item-open-in" title={`Opens in new tab`}>
				<i className="fal fa-fw fa-external-link-square-alt"></i>
			</span>
		)}

		{/* popover target */}
		{!!item.popoverTarget?.length && (
			<span className="nav-menu-item-target">
				<i className="fal fa-bring-forward"></i> {item.popoverTarget}
			</span>
		)}
	</div>
);

const renderDialogForm = (item: NavMenuItem, onChange: (updates: Partial<NavMenuItem>) => void) => (
	<div className="nav-menu-dialog-form">
		<Input
			type="text"
			label={`Label`}
			value={item.label}
			onChange={(e) => onChange({ label: (e.target as HTMLInputElement).value })}
		/>
		<div className="form-group">
			<label className="form-label">{`Icon`}</label>
			<FileSelector
				iconOnly
				value={item.iconRef || ''}
				onChange={(e) => onChange({ iconRef: e.target.value })}
			/>
		</div>
		<Input
			type="text"
			label={`Target URL`}
			value={item.target}
			onChange={(e) => onChange({ target: (e.target as HTMLInputElement).value })}
			placeholder="/absolute/url"
		/>
		<Input
			type="select"
			label={`Open in`}
			value={item.openIn || 'current'}
			onChange={(e) => onChange({ openIn: (e.target as HTMLSelectElement).value as 'current' | 'newtab' })}
		>
			<option value="current">{`Current page`}</option>
			<option value="newtab">{`New tab`}</option>
		</Input>
		<Input
			type="text"
			label={`Popover Target`}
			value={item.popoverTarget}
			onChange={(e) => onChange({ target: (e.target as HTMLInputElement).value })}
			placeholder="popover_id"
			help={`Toggle the associated popover panel. Will override target URL if selected`}
		/>
		<Input
			type="checkbox" 
			label={`Hide label on low resolution screens (show icon)`}
			checked={item.hideLabelMobile}
			onChange={(e) => onChange({ target: (e.target as HTMLInputElement).checked })}
		/>
		<Input
			type="checkbox"
			label={`Hide menu item on low resolution screens`}
			checked={item.hideMobile}
			onChange={(e) => onChange({ target: (e.target as HTMLInputElement).checked })}
		/>
	</div>
);

const dialogTitleFor = (item: NavMenuItem, mode: ContentDialogMode): string => {
	if (mode === 'edit') {
		return item.label ? `Edit ${item.label}` : `Edit menu item`;
	}
	if (mode === 'addChild') {
		return `Add menu item`;
	}
	return `Add menu`;
};

const Editor: React.FC<EditorProps> = (props) => {
	return (
		<ContentListEditor<NavMenuItem>
			{...props}
			className="admin-nav-menu-editor"
			nestable
			addLabel={`Add menu`}
			addChildLabel={`Add menu item`}
			emptyText={`No menu items. Click "Add menu" to create one.`}
			parseItems={parseItems}
			serializeItems={serializeItems}
			createItem={createItem}
			renderTitle={renderTitle}
			renderDialogForm={renderDialogForm}
			dialogTitleFor={dialogTitleFor}
		/>
	);
};

export default Editor;
