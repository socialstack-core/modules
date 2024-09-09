import Collapsible from 'UI/Collapsible';
import Default from 'Admin/Layouts/Default';
import webRequest from 'UI/Functions/WebRequest';
import { useState, useEffect } from 'react';
import { useRouter } from 'UI/Session';
import getRef from 'UI/Functions/GetRef';
import ConfirmModal from 'UI/Modal/ConfirmModal';
import Modal from 'UI/Modal';
import Input from 'UI/Input';
import Form from 'UI/Form';
import Search from 'UI/Search';

export default function Datamap(props) {
	const [ datamap, setDatamap ] = useState(false);
	const [showCloneModal, setShowCloneModal] = useState(false);
	const [ showConfirmModal, setShowConfirmModal ] = useState(false);
	const [ searchText, setSearchText ] = useState();
	const [ isEmpty, setIsEmpty ] = useState();
	const [ isLoading, setIsLoading ] = useState(true);
	const { setPage } = useRouter();

	function buildDatamap(results) {
		const base = { children: [] };

		for (const node of results) {
			let curr = base;

			curr.children.push({
				children: []
			});

			curr = curr.children[curr.children.length - 1];
			Object.assign(curr, node);
		}

		base.children.forEach(node => {
			if (node.children.length) {
				node.children.sort((a, b) => (a.nickName.toLowerCase() > b.nickName.toLowerCase()) ? 1 : -1);
			}
		});

		base.children.sort((a, b) => (a.nickName.toLowerCase() > b.nickName.toLowerCase()) ? 1 : -1);

		return base.children;
	}

	function requestData(node) {
		return new Promise((resolve) => {

			if (node.childrenLoaded) {
				resolve(null);
			}

			try {
				webRequest(`${node.name}/list`).then(resp => {
					resolve(resp.json.results);
				});

			} catch (e) {
				console.warn(e);
				resolve(null);
			}

		});
	}

	async function loadNode(node) {
		let children = await requestData(node);

		if (children) {
			setDatamap(prevDatamap => {
				return prevDatamap.map(prevNode => {

					if (prevNode.nickName.toLowerCase() == node.nickName.toLowerCase()) {
						prevNode.children = [];

						children.forEach(entry => {
							var output = {
								children: []
							};
							Object.assign(output, entry);
							output.nickName = output.name;
							output.name = output.title;

							prevNode.children.push(output);
						});

						prevNode.childrenLoaded = true;
						prevNode.noChildren = !children || children.length == 0;

						// clear exclusions if search cleared
						if (!searchText) {
							clearSearch(prevNode);
						} else {
							// mark elements as excluded if they don't match the search criteria
							searchData(prevNode);
						}

					}

					return prevNode;
				});
			})
		}

	}

	useEffect(() => {
		reloadData();
	}, []);

	function reloadData() {
		setIsLoading(true);

		webRequest('customcontenttype/list', {
			where: {
				deleted: false,
				isForm: false
			}
		})
			.then(resp => {
				var dm = buildDatamap(resp.json.results);
				dm.sort((a, b) => (a.nickName > b.nickName) ? 1 : -1);
				setDatamap(dm);
				setIsLoading(false);
			});
	}

	function searchData(data) {

		if (!data) {
			return;
		}

		data.children.forEach(child => {
			searchData(child);
		});

		let name = data.name ? data.name.toLowerCase() : '';
		let nickName = data.nickName ? data.nickName.toLowerCase() : '';
		let type = data.type ? data.type.toLowerCase() : '';
		let numericSearch = parseInt(searchText, 10);

		data.exclude = !name.includes(searchText) && !nickName.includes(searchText) && !type.includes(searchText);

		if (data.exclude && !isNaN(numericSearch)) {
			data.exclude = data.id != numericSearch;
		}

		// check - if a node has a child marked as not excluded, the parent should remain visible
		if (data.exclude && data.children?.length) {
			data.exclude = data.children.filter((child) => !child.exclude).length == 0;
		}

		return data.exclude;
	}

	function clearSearch(data) {

		if (!data) {
			return;
		}

		data.children.forEach(child => {
			clearSearch(child);
		});

		data.exclude = false;
	}

	// update search filtering
	useEffect(() => {

		if (datamap) {

			if (searchText) {
				setIsLoading(true);

				// first ensure all nodes are loaded
				datamap.forEach(node => {
					loadNode(node);
				});

				setIsLoading(false);
			}

			// TODO: implement a proper fix for this - timeout currently necessary for initial search
			setTimeout(() => {
				let empty = true;
				setIsLoading(true);

				setDatamap(prevDatamap => {
					return prevDatamap.map(data => {

						// clear exclusions if search cleared
						if (!searchText) {
							clearSearch(data);
						} else {
							// mark elements as excluded if they don't match the search criteria
							searchData(data, false);
						}

						if (!data.exclude) {
							empty = false;
						}

						return data;
					});
				});

				setIsEmpty(empty);
				setIsLoading(false);
			}, 200);

		}

	}, [searchText]);

	function renderLoading() {
		return <div className="datamap__loading">
			<div className="spinner-border text-primary" role="status">
				<span className="visually-hidden">
					{`Loading...`}
				</span>
			</div>
		</div>;
	}

	function renderEmpty() {
		return <em className="datamap__empty">
			{searchText && `No content types match your search criteria`}
			{!searchText && `No available content types found`}
		</em>;
	}

	function renderNode(data, isChild) {
		var isInstance = data.type != 'CustomContentType';

		var newClick = function (e) {
			e.stopPropagation();
			setPage('/en-admin/' + data.name + '/add');
		};

		var editClick = function (e) {
			e.stopPropagation();

			if (isInstance) {
				// instance of a customContentType
				setPage('/en-admin/' + data.type + '/' + data.id);
			} else {
				// top-level definition of a customContentType
				setPage('/' + window.location.pathname.replace(/^\/+|\/+$/g, '') + '/' + data.id);
            }

		};

		var cloneClick = function (e) {
			e.stopPropagation();
			setShowCloneModal(data);
		}

		var removeClick = function (e) {
			e.stopPropagation();
			setShowConfirmModal(data);
		}

		var newButton = {
			icon: 'fa fa-plus-circle',
			text: `New`,
			showLabel: true,
			variant: 'primary',
			onClick: newClick
		};

		var editButton = {
			icon: 'fa fa-edit',
			text: `Edit`,
			showLabel: true,
			variant: 'primary',
			onClick: editClick,
			children: []
		};

		let allowClone = true;
		let allowRemove = true;

		/*
		var launchButton = {
			disabled: hasParameter,
			icon: 'far fa-fw fa-external-link',
			text: `Launch`,
			showLabel: true,
			variant: 'secondary',
			//onClick: window.location.origin + page.url,
			//target: '_blank'
			onClick: function () {
				setPage(page.url);
			}
		};

		var buttons = !page.isPage ? [editButton, launchButton] : [launchButton];
		const slashUrl = '/' + page.url.replace(/^\/|\/$/g, '');
		//var largeIcon = page.url == '/' ? 'fa-home' : 'fa-file';
		 */

/* WIP - currently doesn't clone associated records (e.g. fields)
		if (allowClone) {
			editButton.children.push({
				icon: 'far fa-fw fa-copy',
				text: `Save as ...`,
				onClick: cloneClick
			});
		}
*/

		if (allowRemove) {
			/*
			editButton.children.push({
				separator: true
			});
			*/
			editButton.children.push({
				icon: 'far fa-fw fa-trash',
				text: `Remove`,
				onClick: removeClick
			});
		}

		var buttons = isInstance ? [editButton] : [newButton, editButton];
		var largeIcon = data.iconRef ? getRef(data.iconRef, { classNameOnly: true }) : (isInstance ? 'fa-file-alt' : 'fa-database');

		return <>
			{/* NB: set defaultClick={editClick} to allow editing by clicking anywhere on expander */}
			<Collapsible compact expanderLeft title={data.nickName} subtitle={data.name} noContent={isChild ? true : undefined}
				info={`ID: #${data.id}`} buttons={buttons} className="datamap-expander"
				defaultClick={undefined} icon={largeIcon} searchText={searchText} hidden={data.exclude} onOpen={() => loadNode(data)}>
				{!isChild && !data.childrenLoaded && data.children.length == 0 && <>
					<div className="datamap-expander__loading">
						<div className="spinner-border text-primary" role="status">
							<span className="visually-hidden">
								{`Loading...`}
							</span>
						</div>
					</div>
				</>}
				{!isChild && data.childrenLoaded && data.noChildren && <>
					<div className="datamap-expander__no-data">
						<i className="far fa-2x fa-fw fa-exclamation-triangle"></i>
						{`No data available`}
					</div>
				</>}
				{!isChild && data.childrenLoaded && data.children.length > 0 && data.children.map(child => {
					return renderNode(child, true);
				})}
			</Collapsible>
		</>;
    }

	function removeData(data) {

		if (!data || !data.type || !data.id) {
			console.error("Unable to remove selected item: ", data);
			return;
		}

		switch (data.type.toLowerCase()) {
			case "customcontenttype":
				// mark as deleted
				webRequest(
					`customContentType/${data.id}`,
					{ deleted: 1 },
					null
				).then(response => {
					window.location.reload();
				});
				break;

			default:
				// remove underlying instance of a type
				webRequest(
					`${data.type}/${data.id}`,
					null,
					{ method: 'delete' }
				).then(response => {
					window.location.reload();
				});
				break;
		}

	}

	function getDataDescription(data) {
		let hasNickname = data.nickName && data.nickName.trim().length;
		let name = hasNickname ? data.nickName : data.name;

		return `${name} (ID: ${data.id})`;
	}

	var addUrl = window.location.href.replace(/\/+$/g, '') + '/add';

	return (
		<Default>
			<div className="admin-page">
				<header className="admin-page__subheader">
					<div className="admin-page__subheader-info">
						<h1 className="admin-page__title">
							{`Edit Site Data`}
						</h1>
						<ul className="admin-page__breadcrumbs">
							<li>
								<a href={'/en-admin/'}>
									{`Admin`}
								</a>
							</li>
							<li>
								{`Custom content types`}
							</li>
						</ul>
					</div>
					<Search className="admin-page__search" placeholder={`Search`}
						onQuery={(where, query) => {
							setSearchText((!query || query.trim().length == 0) ? false : query.toLowerCase());
						}} />
				</header>
				<div className="datamap__wrapper">
					<div className="datamap__internal">
						{showCloneModal && <>
							<Modal visible onClose={() => setShowCloneModal(false)} title={`Save Data As`}>
								<p>
									<strong>{`Cloning from:`}</strong> <br />
									{getDataDescription(showCloneModal)}
								</p>
								<hr />
								<Form
									onSuccess={(response) => {

										// TODO: get related fields
										/*
										webRequest('customcontenttypefield/list',
											{
												where:
												{
													customContentTypeId: showCloneModal.id
												}
											}).then(response => {
												console.log(response);

												var customDataFieldUrls = [...new Set(
													response.json.results.map(result => {
														return `customcontenttypefield/${result.id}`;
													})
												)];

												// ...

											});
											*/


										/*
										webRequest("customcontenttype", clonedData, {}).then(response => {
											setShowCloneModal(false);
											reloadData();
										});
										*/

									}}>

									<Input label={`Name`} id="datamap__clone-name" type="text" name="nickName" required />

									<div className="sitemap__clone-modal-footer">
										<button type="button" className="btn btn-outline-danger" onClick={() => setShowCloneModal(false)}>
											{`Cancel`}
										</button>
										<input type="submit" className="btn btn-primary" value={`Save Copy`} />
									</div>
								</Form>
							</Modal>
						</>}

						{showConfirmModal && <>
							<ConfirmModal confirmCallback={() => removeData(showConfirmModal)} confirmVariant="danger" cancelCallback={() => setShowConfirmModal(false)}>
								<p>
									{`This will remove ${showConfirmModal.type} entry ID #${showConfirmModal.id}.`}
								</p>
								<p>
									{`Are you sure you wish to do this?`}
								</p>
							</ConfirmModal>
						</>}
						{isLoading && renderLoading()}
						{!isLoading && isEmpty && renderEmpty()}
						{!isLoading && datamap && datamap.map(data => {
							return renderNode(data, false);
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
