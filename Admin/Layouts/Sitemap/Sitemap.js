import Collapsible from 'UI/Collapsible';
import Default from 'Admin/Layouts/Default';
import webRequest from 'UI/Functions/WebRequest';
import { useState, useEffect } from 'react';
import { useRouter } from 'UI/Session';
import ConfirmModal from 'UI/Modal/ConfirmModal';

export default function Sitemap(props) {
	const [ sitemap, setSitemap ] = useState(false);
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

		return rootPage;
	}

	function addPage(url, rootPage) {

		if (!url || !url.length) {
			return null;
		}

		var tokenSet = [];

		// strip leading and trailing slashes
		if (url[0] == '/') {
			url = url.substring(1);
		}

		if (url.length && url[url.length - 1] == '/') {
			url = url.substring(0, url.length - 1);
		}

		// url parts
		var pg = rootPage;

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
					part = "*";
				}

				var next = Object.entries(pg.children).filter(page => page[0] == part);

				if (!next.length) {
					pg.children[part] = next = {
						children: {},
						pages: [],
						redirection: null,
						urlTokenNames: null,
						urlTokenNamesJson: null,
						urlTokens: null,
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

	useEffect(() => {
		webRequest('page/list').then(resp => {
			setSitemap(buildSitemap(resp.json.results));
		});
	}, []);

	function renderNode(node, isRoot) {
		var hasParameter = node.urlTokens && node.urlTokens.length > 0;

		return <>
			{
				node.pages.map(page => {
					var pageUrl = '/' + window.location.pathname.replace(/^\/+|\/+$/g, '') + '/' + page.id;

					var editClick = function (e) {
						e.stopPropagation();
						setPage(pageUrl);
					};

					var removeClick = function (e) {
						e.stopPropagation();
						setShowConfirmModal(page.id);
					}

					var editButton = {
						icon: 'fa fa-edit',
						text: `Edit`,
						showLabel: true,
						variant: 'primary',
						//onClick: editClick
						onClick: pageUrl
					};

					var launchButton = {
						disabled: hasParameter,
						icon: 'fa fa-external-link',
						text: `Launch`,
						showLabel: true,
						variant: 'secondary',
						//onClick: window.location.origin + page.url,
						//target: '_blank'
						onClick: function () {
							setPage(page.url);
						},
						children: [
							{
								icon: 'fa fa-fw fa-trash',
								text: `Remove`,
								onClick: removeClick
							}
						]
					};

					var isPage = page.type == "Page";
					var buttons = isPage ? [editButton, launchButton] : [launchButton];
					var largeIcon = page.url == '/' ? 'fa-home' : 'fa-file';

					if (page.url.startsWith('/en-admin')) {
						largeIcon = 'fa-cog';
					}

					var hasChildren = Object.keys(node.children).length;

					var jsx = <>
						{page.preferIfLoggedIn && <>
							<i className={'sitemap-expander--prefers-logged-in fa fa-fw fa-user-circle'} title={`Preferred if logged in`}></i>
						</>}
						<span className="info">
							{!isPage ? undefined : `ID: #${page.id}`}
						</span>
					</>;

					return <>
						<Collapsible compact expanderLeft title={page.url} subtitle={page.title}
							jsx={jsx} buttons={buttons}
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

	function removePage(id) {
		webRequest(
			'page/' + id,
			null,
			{ method: 'delete' }
		).then(response => {
			window.location.reload();
		});
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
						{showConfirmModal && <>
							<ConfirmModal confirmCallback={() => removePage(showConfirmModal)} confirmVariant="danger" cancelCallback={() => setShowConfirmModal(false)}>
								<p>
									{`This will remove page ID #${showConfirmModal}.`}
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
