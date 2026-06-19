import Table from 'UI/Table';
import {Content} from 'Api/Database';
import {ListFilter} from 'Api/Startup';
import {useState, useEffect, useRef, useMemo} from 'react';
import Icon from "UI/Icon";
import Link from "UI/Link";
import {useRouter} from "UI/Router";
import Button from "UI/Button";
import Input from "UI/Input";
import Alert from "UI/Alert";
import Canvas from "UI/Canvas";
import Loading from "UI/Loading";
import Form from "UI/Form";
import Debounce from "UI/Functions/Debounce";
import ConfirmDialog from "UI/Dialog/ConfirmDialog";
import AutoFormExtensions from "Admin/AutoForm/AutoFormExtensions";
import { TabsWrapper, TabsLinksWrapper, TabsLinkWrapper, TabsPanelsWrapper, TabsPanelWrapper } from "UI/Tabs";
import Drafts from 'Admin/Revisions/Drafts';
import AdminPage from 'Admin/AdminPage';
import Footer from 'Admin/Footer';
import getAutoForm from 'Admin/Functions/GetAutoForm';
import { Breadcrumb } from 'Admin/AdminPage/SubHeader';


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
	columns: AutoListColumn[],
	previousPageUrl?: string;
	previousPageName?: string;
	hideEndpointUrl?: boolean;
}

export interface AutoListColumn {
	label: string;
	field: string;
	module: string;
    isSearchable: boolean;
	title?: boolean;
}

export interface SortField {
	field: string;
	direction: 'asc' | 'desc';
}

export interface AutoListCellRenderer
{
	entity: Content<uint>,
	field: string,
	value: any
}

const AutoList: React.FC<React.PropsWithChildren<AutoListProps>> = (props) => {
	
	// ====================
	// Props 
	// ====================
	const { contentType } = props;
	const { getPageIncludes } = useRouter();	
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
		const titleColumn = props.columns.find(column => column.title);
		if (titleColumn) {
			return { field: titleColumn.field, direction: 'asc' };
		}

		return props.columns.find(column => column.field === 'name')
			? { field: 'name', direction: 'asc' }
			: props.columns.find(column => column.field === 'id')
				? { field: 'id', direction: 'desc' }
				: null;
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
	});

	const [formData, setFormData] = useState<any>(null);
	const [searchForm, setSearchForm] = useState<any>(null);

	// Load the form fields - the initial content itself is provided via props.content
	useEffect(() => {
		setFormData(null);
		setSearchForm(null);
		getAutoForm('content', contentType.toLowerCase())
			.then(formData => {
				const { form } = formData;
				const { fields } = form;

				const searchFormCanvas = {
					c: formData.form.fields?.filter(field => {
						// Searchable?
						return !!field.searchMode;
					}).map(field => {

						// Note that some fields may be readonly/ disabled
						// but are explicitly overriding their searchability.
						// Due to this, we're explicitly removing disabled/readonly from the data.

						// also strip "Required" from validation
						let cleanValidate = field.data?.validate as string[] | string | undefined;

						if (Array.isArray(cleanValidate)) {
							cleanValidate = cleanValidate.filter(v => v !== "Required");
							if (cleanValidate?.length === 0) cleanValidate = undefined;
						} else if (cleanValidate === "Required") {
							cleanValidate = undefined;
						}

						// Check if this is a checkbox/boolean field - swap to custom CheckboxFilter
						const isCheckbox = field.data?.type === "checkbox";

						if (isCheckbox) {
							return {
								t: 'Admin/AutoList/CheckboxFilter',
								c: field.content,
								__key: 'sf_' + (field.data?.name || 'unnamed_field'),
								d: field.data ? {
									...field.data,
									disabled: undefined,
									hidden: undefined,
									readonly: undefined,
									required: undefined,
									validate: cleanValidate,
									_isAdminSearch: true,
									type: "select",
									noSelection: "Please select..."
								} : null
							};
						}

						return {
							t: field.module,
							c: field.content,
							__key: 'sf_' + (field.data?.name || 'unnamed_field'),
							d: field.data ? {
								...field.data,
								disabled: undefined,
								hidden: undefined,
								readonly: undefined,
								required: undefined,
								validate: cleanValidate,
								_isAdminSearch: true
							} : null
						};

					})
				};

				setFormData(formData);
				setSearchForm(searchFormCanvas);
			})
			.catch(e => {
				console.error(e);
			});
	}, [props.contentType]);

	const includes = useMemo(() => (getPageIncludes() ?? '').split(','), [props.contentType]);

	updateQueryRef.current = updateQuery;

	/**
	 * Tabs
	 */
	const currentTab = query?.get("tab");

	const setCurrentTab = (target: string) => {
		updateQuery({
			tab: target
		});
	};

	//console.log(currentTab);

	/**
	 * Has an error occured.
	 */
	const [error, setError] = useState<PublicError>();

	/**
	 * Holds the current list for bulk selection
	 */
	const [currentList, setCurrentList] = useState<Content<uint>[]>([]);

	/**
	 * Clear bulk selections when page changes (currentList updates on pagination)
	 */
	useEffect(() => {
		setBulkSelections({});
	}, [currentList]);

	/**
	 * How many items are pending deletion
	 */
	const [pendingDelete, setPendingDelete] = useState<number>(0);

	/**
	 * Show the confirm deletion modal.
	 */
	const [needsConfirmDelete, setNeedsConfirmDelete] = useState<boolean>(false);

	const [searchFilter, setSearchFilter] = useState<any>(null);

	// ====================
	// Derived state
	// ====================
	const searchText: string | undefined = pageState.query.get("q") || undefined;

	const listFilter: Partial<ListFilter> = {
		sort: sort ?? {
			field: 'id',
			direction: 'desc'
		}
	}

	if (searchText) {
		const isNumeric = /^\d+$/.test(searchText);

		let query = props.searchFields
			?.filter((field) => field !== 'id' || isNumeric) // Skip 'id' field if not numeric
			?.map((field) => field + (field == 'id' ? ' = ?' : ' contains ?'))
			.join(' or ');

		if (query) {
			listFilter.query = query;
			listFilter.args = props.searchFields
				?.filter((field) => field !== 'id' || isNumeric)
				?.map(() => searchText);
		}
	}

	if (searchFilter) {
		listFilter.query = searchFilter.query;
		listFilter.args = searchFilter.args;
	}

	// =================
	// useEffects
	// =================

	/**
	 * When the sort field is invalid, reset it back to ID.
	 */
	useEffect(() => {
		if (!sort) return;

		const validFields = props.columns?.map((column) => column.field);

		if (!validFields.includes(sort.field)) {
			const defaultSortField = props.columns?.find(field => field.field === 'id') ? 'id' : (props.columns[0]?.field ?? 'id');
			setSort(defaultSortField ? { field: defaultSortField, direction: 'desc' } : null);
		}
	}, [sort, props.columns]);
	
	const selected = Object.values(bulkSelections).filter(Boolean);
	
	// =================
	// Local Functions
	// =================
	const deleteSelected = () => {
		// this is the list of selected items IDs.
		const ids = Object.entries(bulkSelections)
			.filter(([key, value]) => Boolean(value))
			.map(([key, value]) => key);
		
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
			<Alert variant="warning">
				{`Old page identified: please delete your en-admin pages and restart the API to regenerate them.`}
			</Alert>
		)
	}

	var addUrl = props.customUrl
		? '/en-admin/' + props.customUrl + '/' + 'add'
		: '/en-admin/' + props.contentType.toLowerCase() + '/' + 'add';
	
	const title = capitalise(props.plural);
	
	// iterates a "." chain to resolve a property on an entity
	const accessInclude = (path: string, target: Content<uint>) => {
		
		const parts = path.split('.');
		
		let current = target;
		
		for (let i = 0; i < parts.length; i++) {
			const key = parts[i] as keyof typeof current;

			if (!current[key])
			{
				return `None specified`;
			}
			current = current[key] as any;
		}
		
		return current;
	}

	const renderMainTableContent = () => {
		return <div className={'table-container'}>
			{error && <Alert variant={'danger'}>{error.message}</Alert>}

			{/* main area */}
			<Table
				over={api}
				paged paginatorOnly dockBottom
				className="autolist-table"
				filter={listFilter as ListFilter}
				includes={includes}
				onHeader={() => {
					return <>
						<tr>
							{/* select / deselect all */}
							<th colSpan={props.columns.length + 1}>
								<Input type="checkbox" sm noWrapper label={`Select all`}
									checked={currentList.length > 0 && currentList.every(item => bulkSelections[item.id])}
									onChange={(ev) => {
										currentList.forEach(item => {
											bulkSelections[item.id] = (ev.target as HTMLInputElement).checked;
										})
										setBulkSelections({ ...bulkSelections })
									}}
								/>
							</th>
						</tr>
						<tr>
							{props.columns.map((field) => {
								return (
									<th>
										<Button sm variant="link" onClick={() => {
											if (sort?.field != field.field) {
												setSort({ field: field.field, direction: 'asc' })
											} else {
												setSort({ field: sort.field, direction: sort.direction === 'asc' ? 'desc' : 'asc' });
											}
										}}>
											<span>
												{capitalise(field.label || field.field)}
											</span>
											{sort?.field === field.field && (
												<i className={'fr fr-chevron-' + (sort?.direction === 'asc' ? 'down' : 'up')} />
											)}
										</Button>
									</th>
								)
							})}
							<th>
								<span className="sr-only">
									{`Actions`}
								</span>
							</th>
						</tr>
					</>;
				}}
				orNone={() => {
					return (
						<tr>
							<td colSpan={props.columns.length + 1}>
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
				{(entry: Content<uint>) => {
					var path = props.customUrl
						? '/en-admin/' + props.customUrl + '/'
						: '/en-admin/' + props.contentType.toLowerCase() + '/';

					return (
						<tr className="autolist-table__row">
							{props.columns.map((field, index) => {
								const colClasses = ["autolist-table__col"];

								if (field.field == "id") {
									colClasses.push("autolist-table__col--id");
								}

								if (index == 0) {
									colClasses.push("autolist-table__col--select");
								}

								const isInclude = field.field.includes('.');
								const value = isInclude ? accessInclude(field.field, entry) : (entry as any)[field.field as string];
								const hasValue = typeof value === 'string' ? value.trim().length > 0 : value;
								const CustomRenderer: React.FC<AutoListCellRenderer> = field.module ? require(field.module).default : null;

								// assumes at least 2 columns (first gains a selection checkbox, second renders the link for the entire row)
								return (
									<td className={colClasses.join(' ')}>
										{/* first column - selection */}
										{index == 0 && <>
											<div className="autolist-table__select-wrapper">
												<Input type="checkbox" sm noWrapper
													checked={!!bulkSelections[entry.id]}
													onChange={(ev) => {
														bulkSelections[entry.id] = (ev.target as HTMLInputElement).checked;
														setBulkSelections({ ...bulkSelections })
													}}
												/>
												{hasValue && CustomRenderer && <CustomRenderer entity={entry} field={field.field} value={value} />}
												{hasValue && !CustomRenderer && value}
												{!hasValue && `[No ${field.label || field.field}]`}
											</div>
										</>}
										{/* second column - link (utilises pseudo element to cover entire row) */}
										{index == 1 && <>
											<Link sm href={`${path}${entry.id}`} className="autolist-table__link">
												{CustomRenderer ? <CustomRenderer entity={entry} field={field.field} value={value} /> : value}
											</Link>
										</>}
										{/* remaining columns */}
										{index > 1 && <>
											{CustomRenderer ? <CustomRenderer entity={entry} field={field.field} value={value} /> : value}
										</>}
									</td>
								)
							})}
							<td className="autolist-table__col autolist-table__col--actions">
								<span className="autolist-table__actions">
									{AutoFormExtensions.getAutoFormButtons(contentType, 'list').map((item) => {
										return <>
											{item.href && <>
												<Link xs outlined href={item.href} className={item.className}>
													{item.label}
												</Link>
											</>}
											{item.onClick && <>
												<Button xs outlined onClick={() => item.onClick!(undefined, setPage)}>
													{item.label}
												</Button>
											</>}
										</>;
									})}
								</span>
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

/*
	// check for overriding "parent" property
	// can be used to override the default parent breadcrumb link in the event the parent page is not available
	// (e.g. /navmenu lists all nested menus, /navmenuitem/[id] describes a submenu, but /navmenuitem does not exist)
	let parentUrl = props.parent && props.parent.trim().length ?
		`/en-admin/${props.parent.toLowerCase()}` :
		`/en-admin/${props.contentType.toLowerCase()}`;
*/

	var breadcrumbs: Breadcrumb[] = [];

	if (props.previousPageUrl && props.previousPageName) {
		breadcrumbs.push({
			url: props.previousPageUrl,
			title: props.previousPageName
		});
	}

	if (!props.hideEndpointUrl) {
		breadcrumbs.push({
			title: title
		});
	}

	return <>
		<AdminPage.SubHeader title={title} breadcrumbs={breadcrumbs} />
		<AdminPage.ContentWrapper>
			<AdminPage.Filters
				searchText={searchText}
				open={true}
				onInput={(ev: React.InputEvent<HTMLInputElement>) => {
					debounce?.handle(ev.currentTarget.value.trim())
				}}
				onChange={(ev: React.ChangeEvent<HTMLInputElement>) => {
					debounce?.handle(ev.currentTarget.value.trim())
				}}>
				{searchForm ? <Form xs onReset={() => {
					setSearchFilter({ query: '', args: [] });
				}} action={(vals : any) => {
					return new Promise((s, r) => {
						var query = '';
						var args = [];

						for (var k in vals) {
							var val = vals[k];

							if (val === "") {
								continue;
							}

							if (query) {
								query += ' and ';
							}
							query += k;

							var isArray = val && Array.isArray(val);

							// eq or contains
							var fieldInfo = formData?.form?.fields?.find((field: any) => field.data?.name == k);

							var isContains = fieldInfo?.searchMode == 2 || isArray;

							if (isContains) {
								query += " contains ";
							} else {
								query += " = ";
							}

							if (isArray) {
								// tags etc - arrays of includes
								query += "[?]";
							} else {
								query += "?";
							}

							args.push(vals[k]);
						}

						setSearchFilter({
							query,
							args
						});

						s({});
					});
				}}>
					<Canvas bodyJson={searchForm} />
					<footer className="admin-page__filters-footer">
						<Button sm outlined type="reset">
							{`Clear Filters`}
						</Button>
						<Button sm type="submit">
							{`Search`}
						</Button>
					</footer>
				</Form> : <Loading />}
			</AdminPage.Filters>
			<AdminPage.Content>
				{renderTabs()}
				{needsConfirmDelete && <>
					<ConfirmDialog variant="danger" title={`Delete ${selected.length} Items`} isOpen={needsConfirmDelete}
						onClose={() => {
							setNeedsConfirmDelete(false);
							setPendingDelete(0);
						}}
						confirmText={`Delete selected`}
						confirmCallback={() => {
							return deleteSelected()
								.then(() => {
									setNeedsConfirmDelete(false);
									setPendingDelete(0);
									location.reload();
								})
								.catch((e: PublicError) => {
									setError(e);
								});
						}}>
						<p>
							{`Deleting these items means they will disappear forever. Are you sure?`}
						</p>
					</ConfirmDialog>
				</>}
			</AdminPage.Content>
		</AdminPage.ContentWrapper>
		<Footer>
			<Footer.BulkActions>
				{selected?.length > 0 && <>
					<Button
						variant="danger" outlined
						disabled={pendingDelete != 0}
						onClick={() => {
							setPendingDelete(selected.length);
							setNeedsConfirmDelete(true);
						}}>
						<Icon type={'fr fr-trash-alt'} /> {bulkActionLabel}
					</Button>
				</>}
			</Footer.BulkActions>
			<Footer.CallsToAction>
				{props.create && (
					<Link href={addUrl}>
						<Button variant={'primary'}>{`Create ${props.singular}`}</Button>
					</Link>
				)}
			</Footer.CallsToAction>
		</Footer>
	</>;
}

export default AutoList;