import CollapsibleEx from 'UI/Collapsible';
import Default from 'Admin/Layouts/Default';
import webRequest from 'UI/Functions/WebRequest';
import { useState, useEffect } from 'react';
import { useRouter } from 'UI/Session';

export default function Sitemap(props) {
	//const { } = props;
	const [ sitemap, setSitemap ] = useState(false);
	const { pageState, setPage } = useRouter();
	var entityData = [];

	function includePage(target, source, path) {
		target.createdUtc = source.createdUtc;
		target.description = source.description;
		target.editedUtc = source.editedUtc;
		target.id = source.id;
		target.isDraft = source.isDraft;
		target.preferIfLoggedIn = source.preferIfLoggedIn;
		target.revision = source.revision;
		target.revisionId = source.revisionId;
		target.title = source.title;
		target.url = path;
		target.userId = source.userId;
	}

	function buildSitemap(results) {
		const base = { children: [] };

		for (const node of results) {
			const pageUrl = '/' + node.url.replace(/^\/|\/$/g, '');
			const path = pageUrl.match(/\/[^\/]+/g);
			let curr = base;

			if (!path) {
				// root
				base.children.push({
					children: []
				});
				includePage(curr.children[curr.children.length - 1], node, '/');
			} else {
				path.forEach((e, i) => {
					const currPath = path.slice(0, i + 1).join("");
					const child = curr.children.find(e => e.url === currPath);

					if (child) {

						// found an existing node with the same path
						if (node.url == currPath) {

							// if preferIfLoggedIn varies, treat these as two variations of the same page
							if (node.preferIfLoggedIn != child.preferIfLoggedIn) {
								var loginVariant = { children: [] };
								includePage(loginVariant, node, currPath);
								child.children.push(loginVariant);
							} else {
								// otherwise update existing link info
								includePage(child, node, currPath);
                            }

                        }

						curr = child;
					}
					else {
						var grandchildren = [];

						// check - is this step a valid URL?
						if (!results.filter(page => '/' + page.url.replace(/^\/|\/$/g, '') == currPath).length) {
							return;
                        }

						// check for params
						var params = pageUrl.match(/{([^}]+)}/g);
						var paramInfo = [];

						if (params && params.length == 1) {
							paramInfo = params[0].slice(1, -1).split(".");
						}

						// check - should have two elements (e.g. user / id)
						if (paramInfo.length == 2) {
							// has params - inject each valid URL
							var pEntity = paramInfo[0];
							var pKey = paramInfo[1];

							entityData
								.filter(data => data.type.toLowerCase() == pEntity)
								.forEach(entity => {
									var output = {
										isPage: true,
										id: entity[pKey],
										url: currPath.replace("{" + pEntity + '.' + pKey + "}", entity[pKey]),
										title: entity.name || entity.title || '',
										description: entity.subTitle || entity.description || '',
										children: []
									};

									// determine where to find data to display for this entity
									var filteredNode = Object.entries(entity).filter(entry => {
										const [key, value] = entry;
										var lowerKey = key.toLowerCase();

										return (typeof (entry[key]) == 'string') &&
											lowerKey !== 'type' &&
											lowerKey !== 'key' &&
											!lowerKey.endsWith('ref') &&
											!lowerKey.endsWith('json');
									});

									var basicInfo = filteredNode.length ? filteredNode[0][1] : `${pEntity} #${entity[pKey]}`;

									if (output.title == '') {
										output.title = basicInfo;
									} else if (output.description == '' && basicInfo != output.title) {
										output.description = basicInfo;
									}

									grandchildren.push(output);
								});
						}

						curr.children.push({
							children: grandchildren
						});
						curr = curr.children[curr.children.length - 1];
						includePage(curr, node, currPath);
					}
				});

			}

		}

		base.children.forEach(node => {
			if (node.children.length) {
				node.children.sort((a, b) => (a.url > b.url) ? 1 : -1);
			}
		})

		return base.children;
    }

	async function handleFetch(url) {
		const resp = await webRequest(url);
		return resp && resp.json ? resp.json.results : undefined;
	}

	async function reduceFetch(acc, curr) {
		const prev = await acc;

		if (prev) {
			entityData = [...entityData, ...prev]
		}

		return handleFetch(curr);
	}

	useEffect(() => {
		webRequest('page/list').then(resp => {
			// check for URLs containing parameters (e.g. {user.id})
			// (...new Set() ensures we strip out any duplicates)
			var entityUrls = [...new Set(
				resp.json.results.map(result => {
					var params = result.url.match(/{([^}]+)}/g);
					var paramInfo = [];

					// TODO: en-admin/{entity}/{id}
					if (params && params.length == 1) {
						paramInfo = params[0].slice(1, -1).split(".");
					}

					var ignoreEntities = [
						'page', // we're already listing pages
						'upload', // no need to show every individual upload (that's a job for the Media page)
						// broken URLs in current DB
						'captcha',
						'customcontenttype'
					]

					if (paramInfo.length == 2 && paramInfo[1] == 'id' && !ignoreEntities.includes(paramInfo[0])) {
						return paramInfo[0] + '/list';
					}
				}).filter(entity => entity != undefined)
			)];

			entityData = [];

			const pipeFetch = async entityUrls => entityUrls.reduce(reduceFetch, Promise.resolve(''));

			pipeFetch(entityUrls).then(result => {

				if (result) {
					entityData = [...entityData, ...result]
                }

				var sm = buildSitemap(resp.json.results);
				sm.sort((a, b) => (a.url > b.url) ? 1 : -1);
				setSitemap(sm);
			});
		});
	}, []);

	function renderNode(page) {
		var hasParameter = page.url.match(/{([^}]+)}/g);
		var editClick = function (e) {
			e.stopPropagation();
			setPage('/en-admin/page/' + page.id);
		};

		var editButton = {
			icon: 'fa fa-edit',
			text: `Edit`,
			showLabel: true,
			variant: 'primary',
			onClick: editClick
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
			}
		};

		var buttons = !page.isPage ? [editButton, launchButton] : [launchButton];
		const slashUrl = '/' + page.url.replace(/^\/|\/$/g, '');
		var largeIcon = page.url == '/' ? 'fa-home' : 'fa-file';

		if (page.url.startsWith('/en-admin')) {
			largeIcon = 'fa-cog';
        }

		return <>
			<CollapsibleEx title={page.url} subtitle={page.title} info={page.isPage ? undefined : `ID: #${page.id}`} buttons={buttons} expanderLeft className="sitemap-expander"
				defaultClick={page.children.length || page.isPage ? undefined : editClick} icon={largeIcon}>
				{page.children.length && page.children.map(child => {
					return renderNode(child);
				})}
			</CollapsibleEx>
		</>;
    }

	var addUrl = window.location.href.replace(/\/+$/g, '') + '/add';

	return (
		<Default>
			<div className="admin-page">
				<header className="admin-page__subheader">
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
				</header>
				<div className="sitemap__wrapper">
					<div className="sitemap__internal">
						{sitemap && sitemap.map(page => {
							return renderNode(page);
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
