import { useEffect } from 'react';
import navMenuApi, { NavMenu } from 'Api/NavMenu';
import useApi from 'UI/Functions/UseApi';
import Loading from 'UI/Loading';
import Dropdown from 'UI/Dropdown';
import Link from 'UI/Link';

interface NavMenuItem {
	id: string;
	label: string;
	iconRef?: string;
	target: string;
	openIn?: 'current' | 'newtab';
	/**
	 * optional popover target id - the item is rendered as a button toggling a
	 * popover with this id (handled by the consuming component).
	 */
	popoverTarget?: string;
	children?: NavMenuItem[];
	hideLabelMobile?: boolean;
	hideMobile?: boolean;
}

interface NavMenuProps {
	/**
	 * The menu ID.
	 */
	id?: number;

	/**
	 * The menu key (e.g. "primary", "footer").
	 */
	menuKey?: string;

	/**
	 * Either a menu key (string), pre-parsed content object with items array, or JSON string.
	 * This avoids an API request when content is provided inline.
	 */
	contentOrKey?: string | { items?: NavMenuItem[] };

	/**
	 * Custom child render function.
	 * @param item The menu item.
	 * @param index Current iteration index.
	 * @returns The React node to render.
	 */
	children?: (item: NavMenuItem, index: number) => React.ReactNode;

	/**
	 * true if only bare links should be rendered (no wrapping <li/>)
	 */
	linksOnly?: boolean;

	/**
	 * optional classnames to add to each rendered link
	 */
	linkClass?: string;

	/**
	 * true if loading animation should be disabled
	 */
	disableLoader?: boolean;
}

const getItemsFromContent = (content: string | { items?: NavMenuItem[] } | undefined): NavMenuItem[] => {
	if (!content) {
		return [];
	}

	// If it's an object with items, use it directly
	if (typeof content === 'object' && content.items) {
		return content.items;
	}

	// If it's a string
	if (typeof content === 'string') {
		// If it's JSON, parse it
		if (content.trim().startsWith('{')) {
			try {
				const parsed = JSON.parse(content);
				return parsed.items || [];
			} catch (e) {
				console.error(e);
				return [];
			}
		}
		// Otherwise it's a key - caller should handle loading
		return [];
	}

	return [];
};

const renderIcon = (iconRef?: string) => {
	if (!iconRef) {
		return null;
	}
	return <i className={iconRef.replace(':', ' ')} />;
};

const NavMenuItemComponent: React.FC<{
	item: NavMenuItem;
	index: number;
	children?: (item: NavMenuItem, index: number) => React.ReactNode;
	linksOnly?: boolean;
	linkClass?: string;
}> = ({ item, index, children, linksOnly, linkClass }) => {
	const hasChildren = item.children && item.children.length > 0;
	const linkClasses = ['nav-menu-link'];

	if (!!linkClass?.length) {
		linkClasses.push(linkClass);
	}

	if (item.hideLabelMobile) {
		linkClasses.push("nav-menu-link--hide-label-mobile");
	}

	if (item.hideMobile) {
		linkClasses.push("nav-menu-link--hide-mobile");
	}

	const defaultContent = (
		<Link
			href={item.target} 
			className={linkClasses.join(' ')}
			target={item.openIn === 'newtab' ? '_blank' : undefined}
			rel={item.openIn === 'newtab' ? 'noopener noreferrer' : undefined}
		>
			{renderIcon(item.iconRef)}
			<span className="nav-menu-label">{item.label}</span>
		</Link>
	);

	const linkContent = children ? children(item, index) : defaultContent;

	const renderDropdown = () => {
		return (
				<Dropdown
					label={linkContent}
				items={item.children!.map((child, childIndex) => ({
						onClick: () => {
							if (child.target) {
								if (child.openIn === 'newtab') {
									window.open(child.target, '_blank');
								} else {
									window.location.href = child.target;
								}
							}
						},
						content: children ? children(child, childIndex) : (
							<a href={child.target} className="nav-menu-link">
								{renderIcon(child.iconRef)}
								{child.label}
							</a>
						)
					}))}
				/>
		);
	};

	if (hasChildren) {
		return linksOnly ? renderDropdown() : (
			<li className="nav-menu-item nav-menu-item--has-children">
				{renderDropdown()}
			</li>
		);
	}

	return linksOnly ? linkContent : (
		<li className="nav-menu-item">
			{linkContent}
		</li>
	);
};

/**
 * A nav menu component that renders items from the contentJson format.
 */
const NavMenuDisplay: React.FC<NavMenuProps> = (props: NavMenuProps) => {
	
	// Check if contentOrKey has pre-parsed items or is a JSON string
	const preParsedItems = typeof props.contentOrKey === 'object' && props.contentOrKey?.items 
		? props.contentOrKey.items 
		: null;
	
	// JSON string content (e.g. injected via the canvas) can be rendered without an API request
	const isJsonContent = typeof props.contentOrKey === 'string' && props.contentOrKey.trim().startsWith('{');
	const isKeyString = typeof props.contentOrKey === 'string' && !isJsonContent;

	const renderItems = (items: NavMenuItem[]) => {
		return (
			<nav className="nav-menu">
				<ul className="nav-menu-list">
					{items.map((item, index) => (
						<NavMenuItemComponent
							key={item.id}
							item={item}
							index={index}
							children={props.children}
							linkClass={props.linkClass}
						/>
					))}
				</ul>
			</nav>
		);
	};

	const renderLinks = (items: NavMenuItem[]) => {
		return items.map((item, index) => (
			<NavMenuItemComponent
				key={item.id}
				item={item}
				index={index}
				children={props.children}
				linksOnly={props.linksOnly}
				linkClass={props.linkClass}
			/>
		));
	};

	// Otherwise, load the menu by key or id
	const [navMenu] = useApi<NavMenu | undefined>(() => {
		if (preParsedItems || isJsonContent) {
			return Promise.resolve(undefined);
		}
		if (props.id) {
			return navMenuApi.load(props.id as int);
		}

		const keyToLoad = props.menuKey || (isKeyString ? props.contentOrKey : undefined);

		if (keyToLoad) {
			return navMenuApi.list({
				pageSize: 1 as int,
				pageIndex: 0 as int,
				query: 'Key = ?',
				args: [keyToLoad]
			}).then(result => {
				return result.results && result.results.length > 0 ? result.results[0] : undefined;
			});
		}

		return Promise.resolve(undefined);
	}, [props.id, props.menuKey, props.contentOrKey]);

	// If we have pre-parsed items, render directly
	if (preParsedItems) {
		return props.linksOnly ? renderLinks(preParsedItems) : renderItems(preParsedItems);
	}

	// If we have JSON content, parse and render directly
	if (isJsonContent) {
		const jsonItems = getItemsFromContent(props.contentOrKey);
		return props.linksOnly ? renderLinks(jsonItems) : renderItems(jsonItems);
	}

	if (!navMenu) {
		return props.disableLoader ? null : <Loading />;
	}

	const items = getItemsFromContent(navMenu.contentJson);

	return items ? (props.linksOnly ? renderLinks(items) : renderItems(items)) : null;
};

export default NavMenuDisplay;
export type { NavMenuItem };
export { getItemsFromContent };