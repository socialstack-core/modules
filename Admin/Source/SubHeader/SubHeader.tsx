import Link from 'UI/Link';
import Search from 'UI/Search';
import AutoFormExtensions, {AutoFormType} from "Admin/AutoForm/AutoFormExtensions";
import {ListFilter} from "Api/Content";
import {useRouter} from "UI/Router";

/**
 * Props for the SubHeader component.
 */
interface SubHeaderProps {
	title?: string,
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
}

/**
 * A breadcrumb.
 */
export interface Breadcrumb {
	title: string,
	/**
	 The url is ignored for the last breadcrumb, representing 'this' page.
	*/
	url?: string
}

/**
 * The SubHeader React component.
 * @param props React props.
 */
const SubHeader: React.FC<React.PropsWithChildren<SubHeaderProps>> = (props) => {
	
	const { pageState } = useRouter();
	
	const SearchOverride = props.contentType && props.pageType ? AutoFormExtensions.getCustomSearchProvider(props.contentType, props.pageType) : undefined;

	return (
		<header className="admin-page__subheader">
			<div className="admin-page__subheader-info">
				<h1 className="admin-page__title">
					{props.title}
				</h1>
				{props.breadcrumbs && <ul className="admin-page__breadcrumbs">
					<li>
						<Link href={'/en-admin/'}>
							{`Admin home`}
						</Link>
					</li>
					{props.breadcrumbs.map(
						(breadcrumb, index) => {
							var lastOne = index == props.breadcrumbs!.length - 1;

							return lastOne ? <li>
								{breadcrumb.title}
							</li> : <li>
								<Link href={breadcrumb.url!}>
									{breadcrumb.title}
								</Link>
							</li>;
						}
					)}
				</ul>}
			</div>
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

			{props.primaryUrl &&
				<div className="admin-page__url">
					<p>{`Page URL: `}</p>
					<a href={props.primaryUrl} target="_blank">
						{props.primaryUrl}
					</a>
				</div>
			}
			
			{props.children}
		</header>
	);
}

export default SubHeader;