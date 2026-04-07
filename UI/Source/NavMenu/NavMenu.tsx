import navMenuApi, { NavMenu } from 'Api/NavMenu';
import useApi from 'UI/Functions/UseApi';
import Loading from 'UI/Loading';
import Dropdown from 'UI/Dropdown';
import { IconRef } from 'UI/Icon';
import { parse } from 'UI/FileRef';

interface NavMenuItem {
	id: string;
	label: string;
	iconRef?: string;
	target: string;
	openIn?: 'current' | 'newtab';
	children?: NavMenuItem[];
}

interface NavMenuProps {
	/**
	 * The menu ID.
	 */
	id?: number,

	/**
	 * The menu key (e.g. "primary", "footer").
	 */
	menuKey?: string,

	/**
	 * Either a menu key (string), pre-parsed content object with items array, or JSON string.
	 * This avoids an API request when content is provided inline.
	 */
	contentOrKey?: string | { items?: NavMenuItem[] },

	/**
	 * Custom child render function.
	 * @param item The menu item.
	 * @param index Current iteration index.
	 * @returns The React node to render.
	 */
	children?: (item: NavMenuItem, index: number) => React.ReactNode;
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
}> = ({ item, index, children }) => {
	const hasChildren = item.children && item.children.length > 0;

	const defaultContent = (
		<a 
			href={item.target} 
			className="nav-menu-link"
			target={item.openIn === 'newtab' ? '_blank' : undefined}
			rel={item.openIn === 'newtab' ? 'noopener noreferrer' : undefined}
		>
			{renderIcon(item.iconRef)}
			<span className="nav-menu-label">{item.label}</span>
		</a>
	);

	const linkContent = children ? children(item, index) : defaultContent;

	if (hasChildren) {
		return (
			<li className="nav-menu-item nav-menu-item--has-children">
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
			</li>
		);
	}

	return (
		<li className="nav-menu-item">
			{linkContent}
		</li>
	);
};

/**
 * A nav menu component that renders items from the contentJson format.
 */
const NavMenuDisplay: React.FC<NavMenuProps> = (props) => {
	
	// Check if contentOrKey has pre-parsed items or is a JSON string
	const preParsedItems = typeof props.contentOrKey === 'object' && props.contentOrKey?.items 
		? props.contentOrKey.items 
		: null;
	
	const isKeyString = typeof props.contentOrKey === 'string';

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
						/>
					))}
				</ul>
			</nav>
		);
	};

	// If we have pre-parsed items, render directly
	if (preParsedItems) {
		return renderItems(preParsedItems);
	}

	// Otherwise, load the menu by key or id
	const [navMenu] = useApi<NavMenu | undefined>(() => {
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

	if (!navMenu) {
		return <Loading />;
	}

	const items = getItemsFromContent(navMenu.contentJson);
	return items ? renderItems(items) : null;
};

export default NavMenuDisplay;
export type { NavMenuItem };
