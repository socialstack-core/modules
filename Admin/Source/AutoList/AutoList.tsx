import Canvas from 'UI/Canvas';
import Search from 'UI/Search';
import Table from 'UI/Table';
import SubHeader from 'Admin/SubHeader';
import {Content, ListFilter} from 'Api/Content';
import ConfirmModal from 'UI/Modal/ConfirmModal';
import { isoConvert } from "UI/Functions/DateTools";
import { useState, useEffect, useRef } from 'react';
import Icon, {IconRef} from "UI/Icon";
import Link from "UI/Link";
import {useRouter} from "UI/Router";
import Button from "UI/Button";
import Input from "UI/Input";
import Alert from "UI/Alert";
import Debounce from "UI/Functions/Debounce";
import Modal from "UI/Modal";
import AutoFormExtensions from "Admin/AutoForm/AutoFormExtensions";
import { TabsWrapper, TabsLinksWrapper, TabsLinkWrapper, TabsPanelsWrapper, TabsPanelWrapper } from "UI/Tabs";
import Drafts from 'Admin/Revisions/Drafts';

const capitalise = (name? : string) => {
	return name && name.length ? name.charAt(0).toUpperCase() + name.slice(1) : "";
}

/**
 * Props for the AutoList component.
 */
export interface AutoListProps {
	contentType: string; // e.g. "User" which must exist as "Api/User", a generated module exporting an ApiEndpoints instance.
	singular?: string;
	plural?: string;
	fields: string[];
	searchFields?: string[];
	customUrl?: string;
	title?: string;
	create?: boolean;
	beforeList?: React.ReactNode;
	afterList?: React.ReactNode;
}

export interface SortField {
	field: string;
	direction: 'asc' | 'desc';
}


const AutoList: React.FC<React.PropsWithChildren<AutoListProps>> = (props) => {
	
	// ====================
	// Props 
	// ====================
	const { contentType } = props;
	
	// ====================
	// Hooks
	// ====================
	const { pageState, updateQuery, setPage, removeQueryItems } = useRouter();
	const { query } = pageState;

	// ====================
	// State
	// ====================

	/**
	 * Holds information about selected items.
	 */
	const [bulkSelections, setBulkSelections] = useState<Record<number, boolean>>({});

	/**
	 * Sorting
	 */
	const [sort, setSort] = useState<SortField | null>(() => {
		// If an id field is specified, that's the default sort
		return props.fields.find(field => field == 'id') ? { field: 'id', direction: 'desc' } : null;
	});

	const updateQueryRef = useRef(updateQuery);

	/**
	 * Debounce to stop API spamming.
	 */
	const [debounce] = useState(() => {
		return new Debounce<string>((value: string) => {
			updateQueryRef.current({
				q: value
			});
		})
	})

	updateQueryRef.current = updateQuery;

	/**
	 * Tabs
	 */
	const currentTab = query?.get("tab");

	const setCurrentTab = (target) => {
		updateQuery({
			tab: target
		});
	};

	console.log(currentTab);

	/**
	 * Has an error occured.
	 */
	const [error, setError] = useState<PublicError>();

	/**
	 * Holds the current list for bulk selection
	 */
	const [currentList, setCurrentList] = useState<Content<uint>[]>([]);

	/**
	 * How many items are pending deletion
	 */
	const [pendingDelete, setPendingDelete] = useState<number>(0);

	/**
	 * Show the confirm deletion modal.
	 */
	const [needsConfirmDelete, setNeedsConfirmDelete] = useState<boolean>(false);


	// ====================
	// Derived state
	// ====================
	const searchText: string | null = pageState.query.get("q");

	const listFilter: Partial<ListFilter> = {
		sort: sort ?? {
			field: 'id',
			direction: 'desc'
		}
	}

	if (searchText) {
		let query = props.searchFields?.map((field) => field + ' contains ?').join(' or ');

		if (query) {
			listFilter.query = query;
			listFilter.args = props.searchFields?.map(() => searchText);
		}
	}
	
	// =================
	// useEffects
	// =================

	/**
	 * When the sort field is invalid, reset it back to ID.
	 */
	useEffect(() => {

		if (sort && !props.fields.find(field => field == sort.field)) {
			// Restore to id sort:
			setSort(props.fields.find(field => field == 'id') ? { field: 'id', direction: 'desc' } : null);
		}

	}, [props.fields, sort, searchText, pendingDelete]);
	
	const selected = Object.values(bulkSelections).filter(Boolean);
	
	// =================
	// Local Functions
	// =================
	const deleteSelected = () => {
		// this is the list of selected items IDs.
		const ids = Object.keys(bulkSelections).filter((key) => Boolean(bulkSelections[key]));
		
		return Promise.allSettled(
			ids.map(id => {
				return api.delete(id)
			})
		)
	}
	

	// =================
	// Miscellaneous
	// =================

	// Api is expected to be an ApiEndpoints object.
	const api = require('Api/' + props.contentType).default;
	
	// ===================
	// Guards
	// ===================
	if (!props.contentType) {
		return (
			<Alert variant={'warning'}>{`Old page identified: please delete your en-admin pages and restart the API to regenerate them.`}</Alert>
		)
	}

	var addUrl = props.customUrl
		? '/en-admin/' + props.customUrl + '/' + 'add'
		: '/en-admin/' + props.contentType.toLowerCase() + '/' + 'add';
	
	const title = capitalise(props.plural);

	const renderMainTableContent = () => {
		return <div className={'table-container'}>
			{error && <Alert variant={'danger'}>{error.message}</Alert>}

			{/* main area */}
			<Table
				over={api}
				paged
				className={'autolist-table'}
				filter={listFilter}
				onHeader={() => {
					return (
						<tr>
							{/* select / deselect all */}
							<th>
								<Input type="checkbox" sm noWrapper label={`Select all`} 
									onChange={(ev) => {
										currentList.forEach(item => {
											bulkSelections[item.id] = (ev.target as HTMLInputElement).checked;
										})
										setBulkSelections({ ...bulkSelections })
									}}
								/>
							</th>
							{props.fields.map((field) => {
								return (
									<th>
										<Button sm variant="link" onClick={() => {
											if (sort?.field != field) {
												setSort({ field, direction: 'asc' })
											} else {
												setSort({ field: sort.field, direction: sort.direction === 'asc' ? 'desc' : 'asc' });
											}
										}}>
											<span>
												{capitalise(field)}
											</span>
											{sort?.field === field && (
												<i className={'fr fr-chevron-' + (sort?.direction === 'asc' ? 'down' : 'up')} />
											)}
										</Button>
									</th>
								)
							})}
							<th>&nbsp;</th>
						</tr>
					)
				}}
				orNone={() => {
					return (
						<tr>
							<td colSpan={2 + props.fields.length}>
								<Alert variant={'info'}>{`No results found`}</Alert>
							</td>
						</tr>
					)
				}}
				onResults={(results) => {
					// grab the results. 
					setCurrentList(results);
					return results;
				}}
			>
				{(entry) => {
					var path = props.customUrl
						? '/en-admin/' + props.customUrl + '/'
						: '/en-admin/' + props.contentType.toLowerCase() + '/';

					return (
						<tr className="autolist-table__row">
							<td className="col--select">
								<Input type="checkbox" sm noWrapper label={`Select`}
									defaultChecked={bulkSelections[entry.id]}
									onChange={(ev) => {
										bulkSelections[entry.id] = (ev.target as HTMLInputElement).checked;
										setBulkSelections({ ...bulkSelections })
									}}
								/>
							</td>
							{props.fields.map((field) => {
								// debugger;
								const colClass = field == "id" ? "col--id" : undefined;

								return (
									<td className={colClass}>
										{/*
										<span className="autotable__field-contents">
											{entry[field]}
										</span>
										*/}
										<Button className="btn-action--edit" sm variant="link" onClick={() => setPage(path + entry.id)}>
											{entry[field]}
										</Button>
									</td>
								)
							})}
							<td className="col--actions">
								{/* Actions */}
								{/*
								<Button className="btn-action--edit" sm variant="link" onClick={() => setPage(path + entry.id)}>
									{`Edit`}
								</Button>
								*/}

								{AutoFormExtensions.getAutoFormButtons(contentType, 'list').map((item) => {
									return (
										<Button
											className={item.className}
											onClick={() => {
												if (item.href) {
													setPage(item.href);
												} else {
													if (item.onClick) {
														item.onClick(item, setPage);
													}
												}
											}}
										>
											{item.label}
										</Button>
									)
								})}
							</td>
						</tr>
					)
				}}
			</Table>
		</div>
	};

	const renderTabs = () => {
		return <TabsWrapper fullWidth>
			{/* tab links */}
			<TabsLinksWrapper>
				<TabsLinkWrapper>
					<input
						type="radio"
						name="tabs"
						id="tab-link1"
						aria-controls="tab-panel1"
						checked={currentTab == "main-table" || !currentTab}
						onClick={() => {
							setCurrentTab("main-table");
						}}
					/>
					<label htmlFor='tab-link1'>{title}</label>
				</TabsLinkWrapper>
				<TabsLinkWrapper>
					<input
						type="radio"
						name="tabs"
						id="tab-link2"
						aria-controls="tab-panel2"
						checked={currentTab == "list-drafts"}
						onClick={() => {
							setCurrentTab("list-drafts");
						}}
					/>
					<label htmlFor='tab-link2'>{`Drafts`}</label>
				</TabsLinkWrapper>
			</TabsLinksWrapper>

			{/* tab panels */}
			<TabsPanelsWrapper>
				<TabsPanelWrapper id="tab-panel1">
					{renderMainTableContent()}
				</TabsPanelWrapper>
				<TabsPanelWrapper id="tab-panel2">
					<Drafts contentType={contentType} hideTitle />
				</TabsPanelWrapper>
			</TabsPanelsWrapper>
		</TabsWrapper>;
	}

	var bulkActionLabel = '';

	if (selected?.length > 0) {
		const bulkAction = pendingDelete != 0 ? `Deleting` : `Delete`;
		const bulkSuffix = selected.length == 1 ? `item` : `items`;

		bulkActionLabel = `${bulkAction} ${selected.length} selected ${bulkSuffix}`;
	}
	
	return (
		<div className={'autolist'}>
			<section className={'autolist-header'}>
				<h2>
					{/*<Icon type={'fa-rocket'}/>*/}
					{title}
					{/*
					{props.create && (<div className={'right-items'}>
						<Link
							href={addUrl}
						>
							<Button variant={'primary'}>{`Create ${props.singular}`}</Button>
						</Link>
					</div>)}
					*/}
				</h2>
			</section>
			<section className={'autolist-body'}>
				<aside>
					{/* sidebar */}
					<div className={'search-filter'}>
						<input 
							type={'text'}
							className={'filter-field'}
							placeholder={'Search'}
							defaultValue={searchText}
							onInput={(ev) => {
								debounce?.handle((ev.target as HTMLInputElement).value.trim())
							}}
							onChange={(ev) => {
								debounce?.handle((ev.target as HTMLInputElement).value.trim())								
							}}
						/>
						<Icon
							type={'fa-search'}
						/>
					</div>
				</aside>
				<main>
					{needsConfirmDelete && (
						<Modal 
							visible={true}
							noFooter
							title={`Delete ${selected.length} items`}
							onClose={() => {
								setNeedsConfirmDelete(false);
								setPendingDelete(0)
							}}
						>
							{`Deleting these items means they will disappear forever. Are you sure?`}
							<Button 
								variant={'danger'}
								onClick={() => {
									deleteSelected().then(() => {
										location.reload();
									})
									.catch((e: PublicError) => {
										setError(e);
									})	
								}}
							>
								{`Delete Selected`}
							</Button>
						</Modal>
					)}
					{renderTabs()}
				</main>
			</section>
			<section className={'autolist-footer'}>
				<div className={'left-items'}>
					{selected && selected.length != 0 && <>
						<Button
							disabled={pendingDelete != 0}
							onClick={() => {
								setPendingDelete(selected.length);
								setNeedsConfirmDelete(true);
							}}>
							<Icon type={'fr fr-trash-alt'} /> {bulkActionLabel}
						</Button>
					</>}
				</div>
				<div className={'right-items'}>
					{props.create && (
						<Link href={addUrl}>
							<Button variant={'primary'}>{`Create ${props.singular}`}</Button>
						</Link>
					)}
				</div>
			</section>
		</div>
	)
}

export default AutoList;