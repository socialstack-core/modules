import Collapsible from 'UI/Collapsible';
import Default from 'Admin/Layouts/Default';
import webRequest from 'UI/Functions/WebRequest';
import { useState, useEffect } from 'react';
import { useRouter } from 'UI/Session';
import Modal from 'UI/Modal';
import ConfirmModal from 'UI/Modal/ConfirmModal';
import Search from 'UI/Search';
import Canvas from 'UI/Canvas';

export default function NavMenuMap(props) {
	const [menuMap, setMenuMap] = useState(false);
	const [showWarningModal, setShowWarningModal] = useState(false);
	const [showRemoveConfirmModal, setShowRemoveConfirmModal] = useState(false);
	const [searchText, setSearchText] = useState();
	const [isEmpty, setIsEmpty] = useState();
	const [isLoading, setIsLoading] = useState(true);
	const { setPage } = useRouter();

	useEffect(() => {
		reloadMenus();
	}, []);

	function reloadMenus() {
		setIsLoading(true);

		webRequest('navmenuitem/list').then(menuItemResp => {
			let menuItems = menuItemResp.json.results.sort((a, b) => a.order - b.order);

			menuItems.forEach(menuItem => {

				if (!menuItem.children) {
					menuItem.children = [];
				}

			});

			webRequest('navmenu/list').then(menuResp => {
				let menus = menuResp.json.results.sort((a, b) => a.order - b.order);

				menus.forEach(menu => {
					menu.children = menuItems.filter(m => m.menuKey == menu.key);
				});

				setMenuMap(menus);
				setIsLoading(false);
			});

		});

	}

	function searchMenu(menu) {
		debugger;

		if (!menu) {
			return;
		}

		menu.children.forEach(child => {
			searchMenu(child);
		});

		let title = menu.type == 'NavMenu' ? (menu.title ? menu.title.toLowerCase() : '') : (menu.bodyJson ? menu.bodyJson.toLowerCase() : '');
		let target = menu.target ? menu.target.toLowerCase() : '';
		let numericSearch = parseInt(searchText, 10);

		menu.exclude = !title.includes(searchText) && !target.includes(searchText);

		if (menu.exclude && !isNaN(numericSearch)) {
			menu.exclude = menu.id != numericSearch;
		}

		// check - if a node has a child marked as not excluded, the parent should remain visible
		if (menu.exclude && menu.children?.length) {
			menu.exclude = menu.children.filter((child) => !child.exclude).length == 0;
		}

		return menu.exclude;
	}

	function clearSearch(menu) {

		if (!menu) {
			return;
		}

		menu.children.forEach(child => {
			clearSearch(child);
		});

		menu.exclude = false;
	}

	// update search filtering
	useEffect(() => {
		if (menuMap) {
			setIsLoading(true);
			let searchMap = structuredClone(menuMap);
			let empty = true;

			menuMap.forEach(menu => {
				// clear exclusions if search cleared
				if (!searchText) {
					clearSearch(menu);
				} else {
					// mark elements as excluded if they don't match the search criteria
					searchMenu(menu);
				}

				if (!menu.exclude) {
					empty = false;
				}
			});

			setIsEmpty(empty);
			setMenuMap(searchMap);
			setIsLoading(false);
		}

	}, [searchText]);

	function renderLoading() {
		return <div className="menumap__loading">
			<div className="spinner-border text-primary" role="status">
				<span className="visually-hidden">
					{`Loading...`}
				</span>
			</div>
		</div>;
	}

	function renderEmpty() {
		return <em className="menumap__empty">
			{searchText && `No menu entries match your search criteria`}
			{!searchText && `No available menu entries found`}
		</em>;
	}

	function renderNode(node) {
		let topLevelMenu = node.type == "NavMenu";

		var newClick = function (e) {
			e.stopPropagation();

			let hasTarget = node.target && node.target.trim().length;

			if (hasTarget) {
				setShowWarningModal(node);
			} else {
				setPage(`/en-admin/navmenuitem/add?navMenuId=${node.id}`);
			}

		};

		var editClick = function (e) {
			e.stopPropagation();

			let editUrl = topLevelMenu ? `/en-admin/navmenu/${node.id}` : `/en-admin/navmenuitem/${node.id}`;

			// open target in new tab if clicked via middle mouse button / shift-clicked
			if (e.button === 1 || (e.button === 0 && e.shiftKey)) {
				const newWindow = window.open(editUrl, '_blank', 'noopener, noreferrer');

				if (newWindow) {
					newWindow.opener = null;
				}

			} else {
				setPage(editUrl);
			}

		};

		var cloneClick = function (e) {
			e.stopPropagation();
			setShowCloneModal(node);
		}

		var removeClick = function (e) {
			e.stopPropagation();
			setShowRemoveConfirmModal(node);
		}

		var newButton = {
			icon: 'fa fa-plus-circle',
			text: `New`,
			showLabel: true,
			variant: 'secondary',
			onClick: newClick
		};

		var optionsButton = {
			icon: 'fa fa-edit',
			text: `Edit`,
			showLabel: true,
			variant: 'secondary',
			onClick: editClick,
			children: []
		};

		// potential future enhancement: allow menu items to be cloned
		/*
		optionsButton.children.push({
			icon: 'far fa-fw fa-copy',
			text: `Save as ...`,
			onClick: cloneClick
		});
		*/

		var hasChildren = Object.keys(node.children).length;

		//optionsButton.children.push({
		//	separator: true
		//});
		optionsButton.children.push({
			icon: 'far fa-fw fa-trash',
			text: `Remove`,
			onClick: removeClick
		});

		return <>
			<Collapsible compact expanderLeft title={topLevelMenu ? node.name : undefined} json={topLevelMenu ? undefined : node.bodyJson} subtitle={node.target}
				buttons={topLevelMenu ? [newButton, optionsButton] : [optionsButton]} className="menumap-expander" defaultClick={hasChildren ? undefined : editClick}
				icon={topLevelMenu ? 'fa-list-alt' : 'fa-chevron-right'} hidden={node.exclude}>
				{node.children.length && node.children.map(child => {
					return renderNode(child);
				})}
			</Collapsible>
		</>;

	}

	function removeMenu(menu) {
		webRequest(
			menu.type == 'NavMenu' ? `navmenu/${menu.id}` : `navmenuitem/${menu.id}`,
			null,
			{ method: 'delete' }
		).then(response => {
			window.location.reload();
		});
	}

	function getMenuDescription(menu) {
		let hasUrl = menu.target && menu.target.trim().length;

		if (menu.type == "NavMenu") {
			return `${menu.name} (${hasUrl ? menu.target + ', ' : ''}ID: ${menu.id})`;
		}

		return <>
			<Canvas>{menu.bodyJson}</Canvas>
			{`(${hasUrl ? menu.target + ', ' : ''}ID: ${menu.id})`}
		</>;

	}

	var addUrl = window.location.href.replace(/\/+$/g, '') + '/add';

	return (
		<Default>
			<div className="admin-page">
				<header className="admin-page__subheader">
					<div className="admin-page__subheader-info">
						<h1 className="admin-page__title">
							{`Edit Navigation Menus`}
						</h1>
						<ul className="admin-page__breadcrumbs">
							<li>
								<a href={'/en-admin/'}>
									{`Admin`}
								</a>
							</li>
							<li>
								{`Navigation menus`}
							</li>
						</ul>
					</div>
					<Search className="admin-page__search" placeholder={`Search`}
						onQuery={(where, query) => {
							setSearchText((!query || query.trim().length == 0) ? false : query.toLowerCase());
						}} />
				</header>
				<div className="menumap__wrapper">
					<div className="menumap__internal">
						{showWarningModal && <>
							<Modal visible="true" onClose={() => setShowWarningModal(false)} title={`Please Note`} className="menumap__warning-modal">
								<p>
									<strong>{`The following menu is currently set as a link:`}</strong><br/>
									{getMenuDescription(showWarningModal)}
								</p>
								<p>
									{`Please remove the target link from the above menu before attempting to add subitems.`}
								</p>
								<footer className="menumap__warning-modal-footer">
									<button type="button" className="btn btn-primary">
										{`OK`}
									</button>
								</footer>
							</Modal>
						</>}
						{showRemoveConfirmModal && <>
							<ConfirmModal confirmCallback={() => removeMenu(showRemoveConfirmModal)} confirmVariant="danger" cancelCallback={() => setShowRemoveConfirmModal(false)}>
								<p>
									<strong>{showRemoveConfirmModal.type == 'NavMenu' ? `This will remove the following menu:` : `This will remove the following menu item:`}</strong> <br />
									{getMenuDescription(showRemoveConfirmModal)}
								</p>
								<p>
									{`Are you sure you wish to do this?`}
								</p>
							</ConfirmModal>
						</>}
						{isLoading && renderLoading()}
						{!isLoading && isEmpty && renderEmpty()}
						{!isLoading && menuMap && menuMap.map(data => {
							return renderNode(data);
						})}
					</div>
					{!this.props.noCreate && <>
						<footer className="admin-page__footer">
							<a href={addUrl} className="btn btn-primary">
								{`Create new`}
							</a>
						</footer>
					</>}
				</div>
			</div>
		</Default>
	);
}
