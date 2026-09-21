import { useEffect, useRef, useState } from "react";
import Collapsible from 'UI/Collapsible';
import Dialog from 'UI/Dialog';
import Button from 'UI/Button';

export interface ContentListEditorNode {
	id: string;
	children?: ContentListEditorNode[];
}

export type ContentDialogMode = 'add' | 'addChild' | 'edit';

export interface ContentListEditorProps<T extends ContentListEditorNode> {
	name?: string;
	value?: string;
	defaultValue?: string;
	label?: string;
	hideLabel?: boolean;
	readonly?: boolean;

	/**
	 * Optional additional classnames applied to the editor root.
	 */
	className?: string;

	/**
	 * True if items may contain nested children (renders rows as collapsible regions).
	 */
	nestable: boolean;

	/**
	 * Text for the footer button used to add a top level item.
	 */
	addLabel: string;

	/**
	 * Text for the per row "add child" button (only shown when nestable).
	 */
	addChildLabel?: string;

	/**
	 * Message shown when there are no items yet.
	 */
	emptyText?: string;

	/**
	 * Initialise the item list from the field's JSON value.
	 */
	parseItems: (value: string) => T[];

	/**
	 * Serialise the current item list back into the field's JSON value.
	 */
	serializeItems: (items: T[]) => string;

	/**
	 * Create a blank item (with a fresh id) ready for the add dialog.
	 */
	createItem: () => T;

	/**
	 * Render a row's title. Receives the optional drag handle to place where required.
	 */
	renderTitle: (item: T, dragHandle: React.ReactNode) => React.ReactNode;

	/**
	 * Render the add/edit dialog form for the draft item.
	 */
	renderDialogForm: (item: T, onChange: (updates: Partial<T>) => void) => React.ReactNode;

	/**
	 * Dialog title for the given mode / draft item.
	 */
	dialogTitleFor: (item: T, mode: ContentDialogMode) => string;

	/**
	 * Optional transform applied to the draft item just before it is committed.
	 */
	beforeSave?: (item: T) => T;
}

interface ContentDialogState<T extends ContentListEditorNode> {
	mode: ContentDialogMode;
	item: T;
	parentId?: string;
}

function ContentListEditor<T extends ContentListEditorNode>(props: ContentListEditorProps<T>) {
	const readonly = props.readonly || false;

	const [items, setItems] = useState<T[]>(() => {
		var initValString = props.value || props.defaultValue || '';
		if (initValString) {
			try {
				return props.parseItems(initValString);
			} catch {
				return [];
			}
		}
		return [];
	});

	const [dragId, setDragId] = useState<string | null>(null);
	const [dragOverId, setDragOverId] = useState<string | null>(null);
	const [dialog, setDialog] = useState<ContentDialogState<T> | null>(null);
	const inputRef = useRef<HTMLInputElement>(null);

	useEffect(() => {
		const input = inputRef.current;
		if (input) {
			(input as any).onGetValue = () => props.serializeItems(items);
		}
	}, [items]);

	const updateItem = (id: string, updates: Partial<T>, itemList: T[]): T[] => {
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

	const removeItem = (id: string, itemList: T[]): T[] => {
		return itemList
			.filter(item => item.id !== id)
			.map(item => {
				if (item.children && item.children.length > 0) {
					return { ...item, children: removeItem(id, item.children) };
				}
				return item;
			});
	};

	const addChild = (parentId: string, child: T, itemList: T[]): T[] => {
		return itemList.map(item => {
			if (item.id === parentId) {
				return {
					...item,
					children: [...(item.children || []), child]
				};
			}
			if (item.children && item.children.length > 0) {
				return { ...item, children: addChild(parentId, child, item.children) };
			}
			return item;
		});
	};

	const moveItem = (fromId: string, toId: string, itemList: T[]): T[] => {
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

	const openEdit = (item: T) => {
		setDialog({ mode: 'edit', item: JSON.parse(JSON.stringify(item)) });
	};

	const openAdd = () => {
		setDialog({ mode: 'add', item: props.createItem() });
	};

	const openAddChild = (parentId: string) => {
		setDialog({ mode: 'addChild', parentId, item: props.createItem() });
	};

	const updateDraft = (updates: Partial<T>) => {
		setDialog(prev => prev ? { ...prev, item: { ...prev.item, ...updates } } : prev);
	};

	const saveDialog = () => {
		if (!dialog) {
			return;
		}

		var draft: T = dialog.item;
		if (props.beforeSave) {
			draft = props.beforeSave(draft);
		}

		if (dialog.mode === 'edit') {
			setItems(prev => updateItem(dialog.item.id, draft, prev));
		} else if (dialog.mode === 'addChild') {
			setItems(prev => addChild(dialog.parentId!, draft, prev));
		} else {
			setItems(prev => [...prev, draft]);
		}

		setDialog(null);
	};

	const ContentRow: React.FC<{ item: T; depth: number }> = ({ item, depth }) => {
		const isDragging = dragId === item.id;
		const isDragOver = dragOverId === item.id;
		const hasChildren = item.children && item.children.length > 0;

		const dragHandle = readonly ? null : (
			<span
				className="content-list-editor-drag-handle"
				draggable
				onDragStart={(e) => handleDragStart(e, item.id)}
				onDragEnd={handleDragEnd}
			>
				<i className="fal fa-fw fa-grip-vertical"></i>
			</span>
		);

		const title = props.renderTitle(item, dragHandle);

		const handleEdit = (e: React.MouseEvent) => {
			e.preventDefault();
			e.stopPropagation();
			openEdit(item);
		};

		const handleDelete = (e: React.MouseEvent) => {
			e.preventDefault();
			e.stopPropagation();
			setItems(prev => removeItem(item.id, prev));
		};

		const rowClasses = `content-list-editor-row ${isDragging ? 'dragging' : ''} ${isDragOver ? 'drag-over' : ''}`;

		if (!props.nestable) {
			return (
				<div
					className={rowClasses}
					style={{ marginLeft: depth * 20 }}
					onDragOver={(e) => handleDragOver(e, item.id)}
					onDragLeave={handleDragLeave}
					onDrop={(e) => handleDrop(e, item.id)}
				>
					<div className="content-list-editor-row-summary">
						{title}
						{!readonly && (
							<div className="content-list-editor-row-actions">
								<Button sm outlined onClick={handleEdit}>
									<i className="fal fa-fw fa-edit"></i> {`Edit`}
								</Button>
								<Button sm outlined variant="danger" onClick={handleDelete}>
									<i className="fal fa-fw fa-trash"></i> {`Delete`}
								</Button>
							</div>
						)}
					</div>
					{hasChildren && (
						<div className="content-list-editor-children">
							{item.children!.map(child => (
								<ContentRow key={child.id} item={child} depth={depth + 1} />
							))}
						</div>
					)}
				</div>
			);
		}

		return (
			<div
				className={rowClasses}
				style={{ marginLeft: depth * 20 }}
				onDragOver={(e) => handleDragOver(e, item.id)}
				onDragLeave={handleDragLeave}
				onDrop={(e) => handleDrop(e, item.id)}
			>
				<Collapsible
					title={title}
					compact
					buttons={readonly ? undefined : [
						{
							icon: <i className="fal fa-fw fa-edit"></i>,
							text: `Edit`,
							showLabel: true,
							onClick: handleEdit,
							variant: 'primary'
						},
						{
							icon: <i className="fal fa-fw fa-trash"></i>,
							text: `Delete`,
							showLabel: true,
							onClick: handleDelete,
							variant: 'danger'
						}
					]}
				>
					{hasChildren && (
						<div className="content-list-editor-children">
							{item.children!.map(child => (
								<ContentRow key={child.id} item={child} depth={depth + 1} />
							))}
						</div>
					)}
					{!readonly && props.addChildLabel && (
						<div className="content-list-editor-add-child">
							<Button sm outlined variant="secondary" onClick={(e) => {
									e.preventDefault();
									e.stopPropagation();
									openAddChild(item.id);
								}}>
								<i className="fal fa-fw fa-plus"></i> {props.addChildLabel}
							</Button>
						</div>
					)}
				</Collapsible>
			</div>
		);
	};

	var className = 'content-list-editor';
	if (props.className) {
		className += ' ' + props.className;
	}

	return (
		<div className={className}>
			{props.label && !props.hideLabel && (
				<label className="form-label">
					{props.label}
				</label>
			)}

			<div className="content-list-editor-items">
				{items.length === 0 && props.emptyText && (
					<div className="content-list-editor-empty">
						{props.emptyText}
					</div>
				)}
				{items.map(item => (
					<ContentRow key={item.id} item={item} depth={0} />
				))}
			</div>

			{!readonly && (
				<footer className="content-list-editor-footer">
					<Button sm outlined onClick={openAdd}>
						<i className="fal fa-fw fa-plus"></i> {props.addLabel}
					</Button>
				</footer>
			)}

			<input
				type="hidden"
				name={props.name}
				ref={inputRef}
				value={props.serializeItems(items)}
			/>

			{dialog && (
				<Dialog
					title={props.dialogTitleFor(dialog.item, dialog.mode)}
					isOpen
					onClose={() => setDialog(null)}
				>
					{props.renderDialogForm(dialog.item, updateDraft)}
					<Dialog.Footer>
						<Button outlined onClick={() => setDialog(null)}>
							{`Cancel`}
						</Button>
						<Button type="submit" onClick={saveDialog}>
							{`Save`}
						</Button>
					</Dialog.Footer>
				</Dialog>
			)}
		</div>
	);
}

export default ContentListEditor;
