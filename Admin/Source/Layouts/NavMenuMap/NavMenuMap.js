import Collapsible from 'UI/Collapsible';
import { useState, useEffect } from 'react';
import { useRouter } from 'UI/Router';
import Modal from 'UI/Modal';
import SubHeader from 'Admin/SubHeader';
import Icon from 'UI/Icon';
import ConfirmModal from 'UI/Modal/ConfirmModal';
import Search from 'UI/Search';
import Canvas from 'UI/Canvas';
import navMenuItemApi from 'Api/NavMenuItem';
import navMenuApi from 'Api/NavMenu';

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

		navMenuItemApi.list().then(menuItemResp => {
			let menuItems = menuItemResp.results.sort((a, b) => a.order - b.order);

			menuItems.forEach(menuItem => {

				if (!menuItem.children) {
					menuItem.children = [];
				}

			});

			navMenuApi.list().then(menuResp => {
				let menus = menuResp.results.sort((a, b) => a.order - b.order);

				menus.forEach(menu => {
					menu.children = menuItems.filter(m => m.menuKey == menu.key);
				});

				setMenuMap(menus);
				setIsLoading(false);
			});

		});

	}

	function searchMenu(menu) {
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
			icon: <Icon type='fa-plus-circle' />,
			text: `New`,
			showLabel: true,
			variant: 'secondary',
			onClick: newClick
		};

		var optionsButton = {
			icon: <Icon type='fa-edit' />,
			text: `Edit`,
			showLabel: true,
			variant: 'secondary',
			onClick: editClick,
			children: []
		};

		// potential future enhancement: allow menu items to be cloned
		/*
		optionsButton.children.push({
			icon: <Icon type='fa-copy' regular fixedWidth />,
			text: `Save as ...`,
			onClick: cloneClick
		});
		*/

		var hasChildren = Object.keys(node.children).length;

		//optionsButton.children.push({
		//	separator: true
		//});
		optionsButton.children.push({
			icon: <Icon type='fa-trash' regular fixedWidth />,
			text: `Remove`,
			onClick: removeClick
		});

		return <>
			<Collapsible compact expanderLeft title={topLevelMenu ? node.name : undefined} json={topLevelMenu ? undefined : node.bodyJson} subtitle={node.target}
				buttons={topLevelMenu ? [newButton, optionsButton] : [optionsButton]} className="menumap-expander" defaultClick={hasChildren ? undefined : editClick}
				icon={topLevelMenu ? <Icon type='fa-list-alt' /> : <Icon type='fa-chevron-right' />} hidden={node.exclude}>
				{node.children.length && node.children.map(child => {
					return renderNode(child);
				})}
			</Collapsible>
		</>;

	}

	function removeMenu(menu) {
		if (menu.type == 'NavMenu') {
			navMenuApi.delete(menu.id).then(response => {
				window.location.reload();
			});
		} else {
			navMenuItemApi.delete(menu.id).then(response => {
				window.location.reload();
			});
		}
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
		<>
			<SubHeader title={`Edit Navigation Menus`} breadcrumbs={[{title: `Navigation menus`}]} onQuery={(where, query) => {
				setSearchText((!query || query.trim().length == 0) ? false : query.toLowerCase());
			}}/>
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
		</>
	);
}
