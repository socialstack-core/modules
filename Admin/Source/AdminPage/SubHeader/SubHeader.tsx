import Link from 'UI/Link';
import Search from 'UI/Search';
import Badge from 'UI/Badge';
import Button from 'UI/Button';
import AutoFormExtensions, {AutoFormType} from "Admin/AutoForm/AutoFormExtensions";
import {ListFilter} from "Api/Startup";
import {useRouter} from "UI/Router";

/**
 * Props for the SubHeader component.
 */
interface AdminPageSubHeaderProps {
	title?: string,
	subtitle?: string,
	className?: string,
	/**
	 * Always prefixed with a link to the admin homepage. I.e. it's as if your array starts with {url: '/en-admin/', title: 'Admin home'}.
	 * If null, no breadcrumbs at all appear.
	 */
	breadcrumbs?: Breadcrumb[],

	primaryUrl?: string,

	/**
	 * Provide this to make a searchbar appear. It is the function that runs when the debounced search query is entered.
	 * @param filter
	 * @param query
	 * @returns
	 */
	onQuery?: (filter: ListFilter, query: string) => void

	/**
	 * Provide this to allow a custom search component to be displayed instead of the default.
	 */
	contentType?: string,

	/**
	 * Provide this to distinguish the page type on the sub header.
	 */
	pageType?: AutoFormType

	/**
	 * The default search value.
	 */
	defaultSearchValue?: string,

	/**
	 * onQuery is fired when results are fetched, this is for when the input changes.
	 * @param value
	 */
	onInput?: (value: string) => void,

	/**
	 * true if item is a revision
	 */
	isRevision?: boolean,

	children?: React.ReactNode
}

/**
 * A breadcrumb.
 */
export interface Breadcrumb {
	title: string,
	/**
	 The url is ignored for the last breadcrumb, representing 'this' page.
	*/
	url?: string,
	href?: string
}

/**
 * The SubHeader React component.
 * @param props React props.
 */
const AdminPageSubHeader: React.FC<React.PropsWithChildren<AdminPageSubHeaderProps>> = (props) => {
	const { title, subtitle, className, breadcrumbs, isRevision, children } = props;
	const { pageState } = useRouter();

	const SearchOverride = props.contentType && props.pageType ? AutoFormExtensions.getCustomSearchProvider(props.contentType, props.pageType) : undefined;

	const subheaderClasses = ['admin-page__subheader'];

	if (className?.length) {
		subheaderClasses.push(className);
	}

	return (
		<header className={subheaderClasses.join(' ')}>
			<div className="admin-page__subheader-info">
				{breadcrumbs && <ol className="admin-page__subheader-breadcrumbs">
					<li>
						<Link href={'/en-admin/'}>
							{`Admin home`}
						</Link>
					</li>
					{breadcrumbs.map(
						(breadcrumb, index) => {
							var lastOne = index == props.breadcrumbs!.length - 1;

							return lastOne ? <li>
								{breadcrumb.title}
							</li> : <li>
								<Link href={breadcrumb.href || breadcrumb.url}>
									{breadcrumb.title}
								</Link>
							</li>;
						}
					)}
				</ol>}
				<h1 className="admin-page__subheader-title">
					<span>
						{title}

						{/* hidden via CSS if filter sidebar not available */}
						<Button sm outlined popoverTarget="admin_filters" popoverTargetAction="toggle" className="admin-page__subheader-filters">
							<span>
								{`Toggle filters`}
							</span>

							{/* filters icon */}
							<svg xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" viewBox="0 0 24 24">
								<path d="M14 17H5M19 7h-9" />
								<circle cx="17" cy="17" r="3" />
								<circle cx="7" cy="7" r="3" />
							</svg>

							{/* search icon */}
							{/*
						<svg xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" viewBox="0 0 24 24">
							<path d="m21 21-4.3-4.3" />
							<circle cx="11" cy="11" r="8" />
						</svg>
						*/}
						</Button>

						{props.primaryUrl && <>
							<Link sm href={props.primaryUrl} target="_blank" rel="noopener noreferrer" className="admin-page__subheader-link">
								<span className="sr-only">
									{`Open page`}
								</span>
								<svg xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" viewBox="0 0 24 24">
									<path d="M15 3h6v6M10 14L21 3M18 13v6a2 2 0 01-2 2H5a2 2 0 01-2-2V8a2 2 0 012-2h6" />
								</svg>
							</Link>
						</>}
					</span>
					{subtitle && <>
						<small>
							{subtitle}
						</small>
					</>}
				</h1>
			</div>

			{(children || isRevision) && <>
				<span className="admin-page__subheader-additional">
					{children}
					{isRevision && <>
						<Badge xs variant="warning">
							{`Revision`}
						</Badge>
					</>}
				</span>
			</>}

			{props.onQuery &&
				(
					SearchOverride ? 
						<SearchOverride 
							onChange={props.onQuery} 
						/> :
						<Search
							className="admin-page__search"
							placeholder={`Search..`}
							onQuery={props.onQuery}
							searchText={pageState.query.get("q") ?? ""}
							onInput={(value: string) => {
								props.onInput && props.onInput(value);
							}}
						/>			
				)
			}
		
		</header>
	);
}

// required for preact (without preact/compat layer) support
AdminPageSubHeader.displayName = "AdminPageSubHeader";

export default AdminPageSubHeader;
