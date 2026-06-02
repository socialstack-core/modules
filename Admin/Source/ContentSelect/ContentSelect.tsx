import Input from 'UI/Input';
import Search from 'UI/Search';
import Link from 'UI/Link';
import Button from "UI/Button";
import { ApiList, ApiIncludes } from "UI/Functions/WebRequest";
import { useRef, useEffect, useState } from 'react';
import { Content } from 'Api/Database';
import { AutoController, ListFilter } from 'Api/Startup';

type ContentSelectChangeEvent = {
	target: {
		value: number | null;
		name?: string;
	};
};

export type ContentSelectProps<T extends Content<uint>> = {
	/** API content type name, e.g. "User" or "Page". */
	contentType: string;
	/** Current value — typically an ID. */
	value?: number | null;
	/** Initial/default value — can be an object with `id` or a plain ID. */
	defaultValue?: number | { id: number } | null;
	/** Hidden input / field name for form submission. */
	name?: string;
	/** Label text shown above the select. */
	label?: string;
	/** When set, renders in search mode using this field name for searching. */
	search?: string;
	/** Field name to use as the display title. Falls back to heuristics. */
	titleField?: string;
	/** If true, the default "None" option is omitted in dropdown mode. */
	hideDefaultValue?: boolean;
	/** Text to show for the empty/none option. Defaults to "None". */
	noSelection?: string;
	/** Text to show for the empty/none option on mobile. Defaults to "None". */
	mobileNoSelection?: string;
	/** Called when the selection changes. */
	onChange?: (e: ContentSelectChangeEvent) => void;
	/** Custom render function for search results and the selected title. */
	onRender?: (entry: T) => React.ReactNode;
	/** Called to customise the search query. */
	onQuery?: (filter: ListFilter, query: string) => void;
	/** API endpoint override for the search component. */
	endpoint?: (filter: ListFilter, includes?: ApiIncludes[]) => Promise<ApiList<T>>;
	/** Internal flag used when rendered inside admin search. Hides footer links. */
	_isAdminSearch?: boolean;
};

/**
 * Dropdown to select a piece of content.
 */
export default function ContentSelect<T extends Content<uint>>(props: ContentSelectProps<T>) {

	function getDefaultValue() {
		var value = null;

		if (props.defaultValue) {
			if (typeof props.defaultValue === 'object' && props.defaultValue.id) {
				value = props.defaultValue.id;
			} else {
				value = props.defaultValue as number;
			}
		}

		return value;
	}

	/**
	 * Returns the best display title for a content object.
	 * Falls back to "No identifiable title" if none found.
	 */
	function getDisplayTitle(content: Record<string, any> | null, titleField: string | undefined, validFields: string[]) {
		if (!content) return 'No identifiable title';

		if (titleField && content[titleField]) {
			return content[titleField];
		}

		for (const field of validFields || []) {
			if (content[field]) {
				return content[field];
			}
		}

		return 'No identifiable title';
	}

	const [searchSelected, setSearchSelected] = useState<T | null>(null);
	const [selected, setSelected] = useState<number | null>(getDefaultValue());
	const [all, setAll] = useState<(T | null)[] | null>(null);
	const [api, setApi] = useState<AutoController<T, int> | null>(null);
	const inputRef = useRef<HTMLInputElement>(null);

	// Load data on mount and when relevant props change (replaces constructor load + componentWillReceiveProps)
	useEffect(() => {
		var loadedApi = require('Api/' + props.contentType).default as AutoController<T, int>;

		setApi(loadedApi);

		if (props.search) {
			var value = props.value || (typeof props.defaultValue === 'object' && props.defaultValue ? props.defaultValue.id : props.defaultValue);
			if (value) {
				loadedApi.load(value as int).then((response : T) => {
					setSearchSelected(response);
				});
			} else {
				setSearchSelected(null);
			}
		} else {
			loadedApi.listAll().then((response : ApiList<T>) => {
				var items : (T | null)[] = response.results;
				if (!props.hideDefaultValue) {
					items.unshift(null);
				}
				var sel = selected ? selected : getDefaultValue();
				setAll(items);
				setSelected(sel);
			});
		}
	}, [props.contentType, props.search, props.value, props.defaultValue]);

	// Form reset listener (replaces componentDidMount / componentWillUnmount)
	useEffect(() => {
		var input = inputRef.current;
		if (!input || !input.form) {
			return;
		}

		var form = input.form;
		var handleFormReset = () => {
			setTimeout(() => {
				setSearchSelected(null);
			}, 0);
		};

		form.addEventListener('reset', handleFormReset);

		return () => {
			form.removeEventListener('reset', handleFormReset);
		};
	}, []);

	function handleChange(e: any) {
		if (props.onChange) {
			props.onChange(e);
		}

		var value = null;

		if (e && e.target && e.target.value && e.target.value > 0) {
			value = e.target.value;
		}

		setSelected(value);
	}

	const { contentType, _isAdminSearch, value : propVal, defaultValue, ...restProps } = props;

	if (props.search) {
		var title: React.ReactNode = '';

		if (searchSelected) {
			if (props.onRender) {
				var rendered = props.onRender(searchSelected);
				if (rendered) {
					title = rendered;
				}
			}
			if (!title) {
				var titleField = props.titleField;
				const selectedAny = searchSelected as any;
				title = titleField && titleField.length && selectedAny[titleField]
					? selectedAny[titleField]
					: selectedAny.title || selectedAny.firstName || selectedAny.username || selectedAny.name || selectedAny.url;
			}
		}

		var value = props.defaultValue || props.value;

		return (
			<div className="mb-3 content-select">
				{props.label && (
					<label className="form-label">{props.label}</label>
				)}
				<input
					type="hidden"
					ref={ele => {
						(inputRef as React.MutableRefObject<HTMLInputElement | null>).current = ele;
						if (ele != null) {
							// @ts-ignore
							ele.onGetValue = (v, input, e) => {
								if (input != inputRef.current) {
									return v;
								}

								return searchSelected ? searchSelected.id : '';
							};
						}
					}}
					name={props.name}
				/>
				<Search
					name=""
					endpoint={api ? api.list : props.endpoint}
					field={props.titleField || props.search}
					limit={5}
					placeholder={'Search for a ' + contentType + '..'}
					inputClassName="ui-form-control"
					onRender={props.onRender}
					onQuery={props.onQuery}
					onFind={entry => {
						setSearchSelected(entry as T);
						if (props.onChange) {
							props.onChange({ target: { value: entry ? entry.id : null, name: props.name } });
						}
					}}
				/>
				<div className="selected-content">
					{searchSelected ? (
						<div className="selected-content__value">
							<span>{title}</span>
							<Button 
								sm 
								variant="secondary" 
								onClick={() => {
									setSearchSelected(null);
									if (props.onChange) {
										props.onChange({ target: { value: null, name: props.name } });
									}
								}}
							>
								<i className="fal fa-times"></i> Remove
							</Button>
						</div>
					) : value ? 'Item #' + value : 'None selected'}
				</div>
			</div>
		);
	}

	var sortedAll = all;
	var titleField = props.titleField;
	var validFields: string[] = [];

	if (sortedAll) {
		if (!titleField) {
			var firstValidItem = sortedAll.find(arg => arg != null) as T;

			if (firstValidItem) {
				var fieldNames = ['title', 'firstName', 'username', 'name', 'url', 'email'];
				fieldNames.forEach(field => {
					if ((firstValidItem as any)[field]) {
						validFields.push(field);
					}
				});

				if (validFields.length > 0) {
					titleField = validFields[0];
				}
			}
		}

		// sort by title
		if (titleField && titleField.length) {
			sortedAll = [...sortedAll].sort((a, b) => {
				if (a && b) {
					let titleA = getDisplayTitle(a, titleField, validFields);
					let titleB = getDisplayTitle(b, titleField, validFields);
					return titleA.localeCompare(titleB);
				}
				return -1;
			});

			// ensure "none" is first
			let noneIndex = sortedAll.findIndex(content => !content);
			if (noneIndex > -1) {
				sortedAll.splice(noneIndex, 1);
				sortedAll.unshift(null);
			}
		}
	}

	var noSelection = props.noSelection || `None`;
	var mobileNoSelection = props.mobileNoSelection || `None`;

	if (
		window.matchMedia('(max-width: 752px) and (pointer: coarse) and (orientation: portrait)').matches ||
		window.matchMedia('(max-height: 752px) and (pointer: coarse) and (orientation: landscape)').matches
	) {
		noSelection = mobileNoSelection;
	}

	return (
		<div className="content-select">
			<Input
				{...restProps}
				noWrapper
				type="select"
				defaultValue={selected ? selected : undefined}
				onChange={e => {
					handleChange(e);
				}}
			>
				{sortedAll
					? sortedAll.map(content => {
						if (!content) {
							return <option value={'0'}>{noSelection}</option>;
						}
						var title = getDisplayTitle(content, titleField, validFields);
						return <option value={content.id}>{`${title} (#${content.id})`}</option>;
					})
					: []}
			</Input>

			{!_isAdminSearch && <footer className="content-select__footer">
				<Link variant="primary" outlined sm
					href={'/en-admin/' + contentType.toLowerCase() + '/add'}
					className="btn-content-select-action btn-add-content"
				>
					<i className="fal fa-fw fa-plus"></i> {`New ${contentType}`}
				</Link>

				{selected && (
					<Link variant="primary" outlined sm
						href={'/en-admin/' + contentType.toLowerCase() + '/' + selected}
						className="btn-content-select-action btn-edit-content"
					>
						<i className="fal fa-fw fa-edit"></i> {`Edit ${contentType}`}
					</Link>
				)}
			</footer>}
		</div>
	);
}
