import Collapsible from 'UI/Collapsible';
import Default from 'Admin/Layouts/Default';
import webRequest from 'UI/Functions/WebRequest';
import { useState, useEffect } from 'react';
import { useRouter } from 'UI/Session';
import ConfirmModal from 'UI/Modal/ConfirmModal';
import Modal from 'UI/Modal';
import Input from 'UI/Input';
import Form from 'UI/Form';

export default function Sitemap(props) {
	const [ sitemap, setSitemap ] = useState(false);
	const [ showCloneModal, setShowCloneModal] = useState(false);
	const [ showConfirmModal, setShowConfirmModal ] = useState(false);
	const { setPage } = useRouter();

	function buildSitemap(results) {
		var map = [];

		var rootPage = {
			children: {},
			pages: [],
			redirection: null,
			urlTokenNames: null,
			urlTokenNamesJson: null,
			urlTokens: null,
			wildcard: null
		};

		for (const node of results) {
			var pg = addPage(node.url, rootPage);

			// skipped?
			if (pg == null) {
				continue;
			}

			if (pg.pages == null) {
				pg.pages = [];
			}

			node.url = trimSlashes(node.url);

			// if there are >1, ensure that the page at the start of the set is the one that is favoured (if any)
			if (node.preferIfLoggedIn) {
				pg.pages.unshift(node);
			} else {
				pg.pages.push(node);
			}

			map.push({
				pageId: node.id,
				url: node.url
			});

		}

		return sortNode(rootPage);
	}

	function addPage(url, rootPage) {

		if (!url || !url.length) {
			return null;
		}

		var tokenSet = [];
		url = trimSlashes(url);

		// url parts
		var pg = rootPage;
		var cumulativeParts = [];

		if (url.length) {
			var parts = url.split('/');
			var skip = false;

			for (var i = 0; i < parts.length; i++) {
				var part = parts[i];
				var token = null;

				if (part.length) {

					// old-style tokens
					if (part[0] == ':') {
						token = part.substring(1);
						tokenSet.push({
							rawToken: token
						});

					} else if (part[0] == '{') {
						// new-style tokens
						token = (part[part.length - 1] == '}') ?
							part.substring(1, part.length - 1) : part.substring(1);

						var dotIndex = token.indexOf('.');

						if (dotIndex != -1) {
							var contentType = token.substring(0, dotIndex);
							var fieldName = token.substring(dotIndex + 1);

							/*
							var type = Api.Database.ContentTypes.GetType(contentType);
	
							if (type == null) {
								//Console.WriteLine("[WARN] Bad page URL using a type that doesn't exist. It was " + url + " using type " + contentType);
								skip = true;
								break;
							}
							*/

							//var service = Api.Startup.Services.GetByContentType(type);

							tokenSet.push({
								//contentType: type,
								contentType: contentType,
								//contentTypeId = Api.Database.ContentTypes.GetId(type),
								fieldName: fieldName,
								//fieldOrProperty: null,
								isId: fieldName.toLowerCase() == "id",
								rawToken: token,
								//service: service,
								typeName: contentType
							});

						} else {
							tokenSet.push({
								rawToken: token
							});
						}

					}

				}

				if (token != null) {
					// Anything. Treat these tokens as *:
					part = "{" + token + "}";
				}

				cumulativeParts.push(part);
				var next = Object.entries(pg.children).filter(page => page[0] == part);

				if (!next.length) {
					pg.children[part] = next = {
						children: {},
						pages: [],
						redirection: null,
						urlTokenNames: null,
						urlTokenNamesJson: null,
						urlTokens: null,
						parts: [...cumulativeParts],
						wildcard: null
					};

					if (token != null) {
						// It's the wildcard one:
						pg.wildcard = next;
					}
				} else {
					next = next[0][1];
				}

				pg = next;
			}

			if (skip) {
				return null;
			}

		}

		pg.urlTokens = tokenSet;
		pg.urlTokenNames = tokenSet.map(token => token.rawToken);

		if (pg.urlTokenNames == null || !pg.urlTokenNames.length) {
			pg.urlTokenNamesJson = "null";
		}
		else {
			pg.urlTokenNamesJson = JSON.stringify(pg.urlTokenNames);
		}

		return pg;
	}

	// sort object keys alphabetically
	function sortNode(node) {
		const sortedChildren = Object.fromEntries(
			Object.keys(node.children).sort().map(key => [key, node.children[key]])
		);
		node.children = sortedChildren;

		Object.entries(node.children).map(([key, value]) => {
			return sortNode(node.children[key]);
		});

		return node;
	}

	// strip leading and trailing slashes
	function trimSlashes(url) {

		if (url) {

			if (url[0] == '/') {
				url = url.substring(1);
			}

			if (url.length && url[url.length - 1] == '/') {
				url = url.substring(0, url.length - 1);
			}

		}

		return url;
	}

	useEffect(() => {
		reloadPages();
	}, []);

	function reloadPages() {
		webRequest('page/list').then(resp => {
			setSitemap(buildSitemap(resp.json.results));
		});
	}

	function cloneBranchClick(e) {
		e.stopPropagation();
		// TODO
	}

	function renderNode(node, isRoot) {
		var hasParameter = node.urlTokens && node.urlTokens.length > 0;

		if (!node.pages || node.pages.length == 0 && Object.keys(node.children).length) {
			var title = "";

			if (node.parts) {
				node.parts.forEach(part => {
					title += `/${part}`;
				});
			}

			var largeIcon = 'fa-file';

			if (title.startsWith('/en-admin')) {
				largeIcon = 'fa-cog';
			}

			/* when available, add buttons={[cloneBranchButton]} to <Collapsible />
			var cloneBranchButton = {
				icon: 'far fa-fw fa-copy',
				text: `Save branch as ...`,
				showLabel: true,
				variant: 'secondary',
				onClick: cloneBranchClick,
				children: []
			};
			*/

			return <>
				<Collapsible compact expanderLeft title={title}
					open={isRoot} alwaysOpen={isRoot} className="sitemap-expander"
					icon={largeIcon}>
					{renderNodeChildren(node)}
				</Collapsible>
			</>;
		}

		return <>
			{
				node.pages.map(page => {
					// ensure all pages are prefixed with a path separator
					let pageUrl = page.url;

					if (!pageUrl.startsWith("/")) {
						pageUrl = "/" + page.url;
					}

					var editClick = function (e) {
						e.stopPropagation();

						let editUrl = '/' + window.location.pathname.replace(/^\/+|\/+$/g, '') + '/' + page.id;

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
						setShowCloneModal(page);
					}

					var removeClick = function (e) {
						e.stopPropagation();
						setShowConfirmModal(page);
					}

					var optionsButton = {
						icon: 'fa fa-edit',
						text: `Edit`,
						showLabel: true,
						variant: 'secondary',
						onClick: editClick,
						children: []
					};

					var isPage = page.type == "Page";
					var largeIcon = page.url == '/' ? 'fa-home' : 'fa-file';

					let allowLaunch = !hasParameter;
					let allowClone = true;
					let allowRemove = true;

					if (pageUrl.startsWith('/en-admin')) {
						largeIcon = 'fa-cog';
						//allowLaunch = false;
						//allowClone = false;
						allowRemove = false;
					}

					if (allowLaunch) {
						optionsButton.children.push({
							icon: 'far fa-fw fa-external-link',
							text: `Launch`,
							onClick: window.location.origin + pageUrl,
							target: '_blank'
						});
					}

					if (allowClone) {
						optionsButton.children.push({
							icon: 'far fa-fw fa-copy',
							text: `Save as ...`,
							onClick: cloneClick
						});
					}

					var hasChildren = Object.keys(node.children).length;

					/* TODO
					if (allowClone && hasChildren) {
						optionsButton.children.push({
							icon: 'far fa-fw fa-copy',
							text: `Save branch as ...`,
							onClick: cloneBranchClick
						});
					}
					*/

					if (allowRemove) {
						optionsButton.children.push({
							separator: true
						});
						optionsButton.children.push({
							icon: 'far fa-fw fa-trash',
							text: `Remove`,
							onClick: removeClick
						});
					}

					var jsx = <>
						{page.preferIfLoggedIn && <>
							<i className={'sitemap-expander--prefers-logged-in fa fa-fw fa-user-circle'} title={`Preferred if logged in`}></i>
						</>}
						<span className="info">
							{!isPage ? undefined : `ID: #${page.id}`}
						</span>
					</>;

					return <>
						<Collapsible compact expanderLeft title={pageUrl} subtitle={page.title}
							jsx={jsx} buttons={[optionsButton]}
							open={isRoot} alwaysOpen={isRoot} noContent={!hasChildren} className="sitemap-expander"
							defaultClick={hasChildren || !isPage ? undefined : editClick} icon={largeIcon}>
							{renderNodeChildren(node)}
						</Collapsible>
					</>;
				})
			}
		</>;
	}

	function renderNodeChildren(node) {
		return <>
			{
				Object.entries(node.children).map(([key, value]) => {
					return renderNode(node.children[key], false);
				})
			}
		</>;
	}

	function removePage(page) {
		webRequest(
			'page/' + page.id,
			null,
			{ method: 'delete' }
		).then(response => {
			window.location.reload();
		});
	}

	function getPageDescription(page) {
		let hasUrl = page.url && page.url.trim().length;

		if (page.title && page.title.trim().length) {
			return `${page.title} (${hasUrl ? page.url + ', ' : ''}ID: ${page.id})`;
		} else {
			return hasUrl ? `${page.url} (ID: ${page.id})` : `ID: ${page.id}`;
		}

	}

	var addUrl = window.location.href.replace(/\/+$/g, '') + '/add';

	return (
		<Default>
			<div className="admin-page">
				<header className="admin-page__subheader">
					<div className="admin-page__subheader-info">
						<h1 className="admin-page__title">
							{`Edit Site Pages`}
						</h1>
						<ul className="admin-page__breadcrumbs">
							<li>
								<a href={'/en-admin/'}>
									{`Admin`}
								</a>
							</li>
							<li>
								{`Pages`}
							</li>
						</ul>
					</div>
				</header>
				<div className="sitemap__wrapper">
					<div className="sitemap__internal">
						{showCloneModal && <>
							<Modal visible onClose={() => setShowCloneModal(false)} title={`Save Page As`}>
								<p>
									<strong>{`Cloning from:`}</strong> <br />
									{getPageDescription(showCloneModal)}
								</p>
								<hr />
								<Form 
									onSuccess={(response) => {
										let clonedPage = structuredClone(showCloneModal);
										clonedPage.url = response.url;
										clonedPage.title = response.title;
										clonedPage.description = response.description;

										webRequest("page", clonedPage, {}).then(response => {
											setShowCloneModal(false);
											reloadPages();
										});

									}}>

									<Input label={`Url`} id="sitemap__clone-url" type="text" name="url" required />
									<Input label={`Title`} id="sitemap__clone-title" type="text" name="title" />
									<Input label={`Description`} id="sitemap__clone-description" type="text" name="description" />

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
							<ConfirmModal confirmCallback={() => removePage(showConfirmModal)} confirmVariant="danger" cancelCallback={() => setShowConfirmModal(false)}>
								<p>
									<strong>{`This will remove the following page:`}</strong> <br />
									{getPageDescription(showConfirmModal)}
								</p>
								<p>
									{`Are you sure you wish to do this?`}
								</p>
							</ConfirmModal>
						</>}
						{sitemap && renderNode(sitemap, true)}
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
