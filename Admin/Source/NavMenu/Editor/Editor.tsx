import { useEffect, useRef, useState } from "react";
import Input from 'UI/Input';
import Collapsible from 'UI/Collapsible';
import { IconRef } from 'UI/Icon';
import FileSelector from 'UI/FileSelector';

interface NavMenuItem {
	id: string;
	label: string;
	iconRef?: string;
	target: string;
	openIn?: 'current' | 'newtab';
	children?: NavMenuItem[];
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

const Editor: React.FC<EditorProps> = (props) => {
	const readonly = props.readonly || false;

	const [items, setItems] = useState<NavMenuItem[]>(() => {
		var initValString = props.value || props.defaultValue || '';
		if (initValString) {
			try {
				var parsed = JSON.parse(initValString);
				return parsed.items || [];
			} catch {
				return [];
			}
		}
		return [];
	});

	const [dragId, setDragId] = useState<string | null>(null);
	const [dragOverId, setDragOverId] = useState<string | null>(null);
	const inputRef = useRef<HTMLInputElement>(null);

	useEffect(() => {
		const input = inputRef.current;
		if (input) {
			(input as any).onGetValue = (_v: string, _input: HTMLInputElement, _e: any) => {
				return JSON.stringify({ items });
			};
		}
	}, [items]);

	const updateItem = (id: string, updates: Partial<NavMenuItem>, itemList: NavMenuItem[]): NavMenuItem[] => {
		return itemList.map(item => {
			if (item.id === id) {
				return { ...item, ...updates };
			}
			if (item.children && item.children.length > 0) {
				return { ...item, children: updateItem(id, updates, item.children) };
			}
			return item;
		});
	};

	const removeItem = (id: string, itemList: NavMenuItem[]): NavMenuItem[] => {
		return itemList
			.filter(item => item.id !== id)
			.map(item => {
				if (item.children && item.children.length > 0) {
					return { ...item, children: removeItem(id, item.children) };
				}
				return item;
			});
	};

	const addChild = (parentId: string, itemList: NavMenuItem[]): NavMenuItem[] => {
		return itemList.map(item => {
			if (item.id === parentId) {
				const newChild: NavMenuItem = {
					id: generateId(),
					label: '',
					target: ''
				};
				return {
					...item,
					children: [...(item.children || []), newChild]
				};
			}
			if (item.children && item.children.length > 0) {
				return { ...item, children: addChild(parentId, item.children) };
			}
			return item;
		});
	};

	const moveItem = (fromId: string, toId: string, itemList: NavMenuItem[]): NavMenuItem[] => {
		const fromIndex = itemList.findIndex(item => item.id === fromId);
		const toIndex = itemList.findIndex(item => item.id === toId);

		if (fromIndex !== -1 && toIndex !== -1) {
			const newList = [...itemList];
			const [movedItem] = newList.splice(fromIndex, 1);
			newList.splice(toIndex, 0, movedItem);
			return newList;
		}

		return itemList.map(item => {
			if (item.children && item.children.length > 0) {
				return { ...item, children: moveItem(fromId, toId, item.children) };
			}
			return item;
		});
	};

	const handleDragStart = (e: React.DragEvent, id: string) => {
		setDragId(id);
		e.dataTransfer.effectAllowed = 'move';
		e.dataTransfer.setData('text/plain', id);
	};

	const handleDragOver = (e: React.DragEvent, id: string) => {
		e.preventDefault();
		e.dataTransfer.dropEffect = 'move';
		setDragOverId(id);
	};

	const handleDragLeave = () => {
		setDragOverId(null);
	};

	const handleDrop = (e: React.DragEvent, targetId: string) => {
		e.preventDefault();
		const fromId = e.dataTransfer.getData('text/plain');
		if (fromId && fromId !== targetId) {
			setItems(prev => moveItem(fromId, targetId, prev));
		}
		setDragId(null);
		setDragOverId(null);
	};

	const handleDragEnd = () => {
		setDragId(null);
		setDragOverId(null);
	};

	const addNewItem = () => {
		const newItem: NavMenuItem = {
			id: generateId(),
			label: '',
			target: ''
		};
		setItems(prev => [...prev, newItem]);
	};

	const NavMenuItemRow: React.FC<{ item: NavMenuItem; depth: number }> = ({ item, depth }) => {
		const [isEditing, setIsEditing] = useState(false);
		const [localLabel, setLocalLabel] = useState(item.label);
		const [localIconRef, setLocalIconRef] = useState(item.iconRef || '');
		const [localTarget, setLocalTarget] = useState(item.target);
		const [localOpenIn, setLocalOpenIn] = useState<'current' | 'newtab'>(item.openIn || 'current');

		useEffect(() => {
			setLocalLabel(item.label);
			setLocalIconRef(item.iconRef || '');
			setLocalTarget(item.target);
			setLocalOpenIn(item.openIn || 'current');
		}, [item]);

		const handleSave = () => {
			setItems(prev => updateItem(item.id, {
				label: localLabel,
				iconRef: localIconRef || undefined,
				target: localTarget,
				openIn: localOpenIn === 'current' ? undefined : localOpenIn
			}, prev));
			setIsEditing(false);
		};

		const handleCancel = () => {
			setLocalLabel(item.label);
			setLocalIconRef(item.iconRef || '');
			setLocalTarget(item.target);
			setLocalOpenIn(item.openIn || 'current');
			setIsEditing(false);
		};

		const handleDelete = () => {
			setItems(prev => removeItem(item.id, prev));
		};

		const handleAddChild = () => {
			setItems(prev => addChild(item.id, prev));
		};

		const handleIconChange = (e: { target: { value: string } }) => {
			setLocalIconRef(e.target.value);
		};

		const isDragging = dragId === item.id;
		const isDragOver = dragOverId === item.id;
		const hasChildren = item.children && item.children.length > 0;

		const title = (
			<div className="nav-menu-item-title">
				{!readonly && (
					<span
						className="nav-menu-drag-handle"
						draggable
						onDragStart={(e) => handleDragStart(e, item.id)}
						onDragEnd={handleDragEnd}
					>
						<i className="fal fa-fw fa-grip-vertical"></i>
					</span>
				)}
				{item.iconRef && (
					<span className="nav-menu-item-icon">
						<IconRef fileRef={item.iconRef as FileRef} />
					</span>
				)}
				<span className="nav-menu-item-label">
					{item.label || `Untitled`}
				</span>
		{item.target && (
					<span className="nav-menu-item-target">
						<i className="fal fa-fw fa-link"></i> {item.target}
					</span>
				)}
				{item.openIn === 'newtab' && (
					<span className="nav-menu-item-open-in" title="Opens in new tab">
						<i className="fal fa-fw fa-external-link-square-alt"></i>
					</span>
				)}
			</div>
		);

		return (
			<div
				className={`nav-menu-item-row ${isDragging ? 'dragging' : ''} ${isDragOver ? 'drag-over' : ''}`}
				style={{ marginLeft: depth * 20 }}
				onDragOver={(e) => handleDragOver(e, item.id)}
				onDragLeave={handleDragLeave}
				onDrop={(e) => handleDrop(e, item.id)}
			>
				<Collapsible
					title={title}
					open={isEditing}
					onOpen={() => setIsEditing(true)}
					onClose={() => setIsEditing(false)}
					compact
					buttons={
						readonly ? undefined : [
							{
								icon: <i className="fal fa-fw fa-plus"></i>,
								text: `Add child`,
								onClick: handleAddChild,
								variant: 'outline-secondary'
							},
							{
								icon: <i className="fal fa-fw fa-trash"></i>,
								text: `Delete`,
								onClick: handleDelete,
								variant: 'outline-danger'
							}
						]
					}
				>
					<div className="nav-menu-item-form">
						<Input
							type="text"
							label={`Label`}
							value={localLabel}
							onChange={(e) => setLocalLabel((e.target as HTMLInputElement).value)}
						/>
						<div className="form-group">
							<label className="form-label">{`Icon`}</label>
							<FileSelector
								iconOnly
								value={localIconRef}
								onChange={handleIconChange}
							/>
						</div>
						<Input
							type="text"
							label={`Target URL`}
							value={localTarget}
							onChange={(e) => setLocalTarget((e.target as HTMLSelectElement).value)}
							placeholder="/absolute/url"
						/>
						<Input
							type="select"
							label={`Open in`}
							value={localOpenIn}
							onChange={(e) => setLocalOpenIn((e.target as HTMLSelectElement).value as 'current' | 'newtab')}
						>
							<option value="current">{`Current page`}</option>
							<option value="newtab">{`New tab`}</option>
						</Input>
						<div className="nav-menu-item-actions">
							<button
								type="button"
								className="btn btn-sm btn-outline-primary"
								onClick={handleSave}
							>
								<i className="fal fa-fw fa-check"></i> {`Save`}
							</button>
							<button
								type="button"
								className="btn btn-sm btn-outline-secondary"
								onClick={handleCancel}
							>
								{`Undo`}
							</button>
						</div>
					</div>
				</Collapsible>

				{hasChildren && (
					<div className="nav-menu-children">
						{item.children!.map(child => (
							<NavMenuItemRow key={child.id} item={child} depth={depth + 1} />
						))}
					</div>
				)}
			</div>
		);
	};

	return (
		<div className="admin-nav-menu-editor">
			{props.label && !props.hideLabel && (
				<label className="form-label">
					{props.label}
				</label>
			)}

			<div className="nav-menu-items">
				{items.length === 0 && (
					<div className="nav-menu-empty">
						{`No menu items. Click "Add item" to create one.`}
					</div>
				)}
				{items.map(item => (
					<NavMenuItemRow key={item.id} item={item} depth={0} />
				))}
			</div>

			{!readonly && (
				<footer className="nav-menu-footer">
					<button
						type="button"
						className="btn btn-sm btn-outline-primary"
						onClick={addNewItem}
					>
						<i className="fal fa-fw fa-plus"></i> {`Add item`}
					</button>
				</footer>
			)}

			<input
				type="hidden"
				name={props.name}
				ref={inputRef}
				value={JSON.stringify({ items })}
			/>
		</div>
	);
};

export default Editor;
