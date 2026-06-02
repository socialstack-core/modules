import { useState, useEffect } from 'react';
import Dialog from 'UI/Dialog';
import Button from 'UI/Button';
import Link from 'UI/Link';
import pageApi from 'Api/Page';
import { TreeNodeDetail } from 'Api/Pages';
import { RouterNodeMetadata } from 'Api/Startup/Routing';
import Loading from 'UI/Loading';

export type PageSelectorValue = {
	id: number;
	title: string;
	url: string;
};

export interface PageSelectorProps {
	value?: PageSelectorValue[];
	defaultValue?: PageSelectorValue[];
	onChange?: (pages: PageSelectorValue[]) => void;
	max?: number;
	label?: string;
	name?: string;
}

export default function PageSelector(props: PageSelectorProps) {
	const [isOpen, setIsOpen] = useState(false);
	const [currentNode, setCurrentNode] = useState<TreeNodeDetail | null>(null);
	const [path, setPath] = useState('');
	const [loading, setLoading] = useState(false);

	var initValue = (props.value || props.defaultValue || []).filter((t: PageSelectorValue) => t != null);
	var atMax = props.max && props.max > 0 && initValue.length >= props.max;

	useEffect(() => {
		loadNode(path);
	}, [path]);

	function loadNode(nodePath: string) {
		setLoading(true);
		pageApi.getRouterTreeNodePath(nodePath || '').then((result: TreeNodeDetail | null) => {
			setCurrentNode(result);
			setLoading(false);
		}).catch(() => {
			setLoading(false);
		});
	}

	function handleSelect(node: RouterNodeMetadata) {
		if (!node.contentId || node.contentId <= 0) {
			return;
		}

		var newPage: PageSelectorValue = {
			id: node.contentId,
			title: node.name || node.childKey || '',
			url: node.fullRoute || ''
		};

		if (initValue.some((p: PageSelectorValue) => p.id === newPage.id)) {
			setIsOpen(false);
			return;
		}

		var newValue = [...initValue, newPage];
		props.onChange && props.onChange(newValue);
		setIsOpen(false);
	}

	function handleRemove(page: PageSelectorValue) {
		var newValue = initValue.filter((p: PageSelectorValue) => p.id !== page.id);
		props.onChange && props.onChange(newValue);
	}

	function navigateTo(pathSegment: string) {
		if (pathSegment === '') {
			setPath('');
			return;
		}

		var pathParts = path ? path.split('/') : [];
		var targetParts = pathSegment.split('/');

		if (targetParts.length === 1) {
			var index = pathParts.indexOf(pathSegment);
			if (index >= 0) {
				setPath(pathParts.slice(0, index + 1).join('/'));
			} else {
				setPath(path ? path + '/' + pathSegment : pathSegment);
			}
		} else {
			setPath(pathSegment);
		}
	}

	function buildBreadcrumbs() {
		var crumbs: string[] = [''];
		if (path) {
			crumbs = ['', ...path.split('/')];
		}
		return crumbs;
	}

	function getBreadcrumbPaths() {
		var paths: string[] = [];
		var parts = path ? path.split('/') : [];
		var currentPath = '';
		paths.push(currentPath);
		for (var i = 0; i < parts.length; i++) {
			currentPath += (currentPath ? '/' : '') + parts[i];
			paths.push(currentPath);
		}
		return paths;
	}

	function renderCurrentNode() {
		if (!currentNode) {
			return null;
		}

		var dirs = currentNode.children?.filter((child: RouterNodeMetadata) => child.hasChildren);
		var files = currentNode.children?.filter((child: RouterNodeMetadata) => {
			return child.type && child.type != 'Group';
		});

		return (
			<table className="table ui-table table-hover admin-pageselector__tree">
				<thead>
					<tr>
						<th className="admin-pageselector__name">{`Name`}</th>
						<th className="admin-pageselector__id">{`ID`}</th>
						<th className="admin-pageselector__actions">{`Actions`}</th>
					</tr>
				</thead>
				<tbody>
					{dirs?.map((child: RouterNodeMetadata) => {
						var name = child.name || '';
						var description = child.childKey || ((dirs?.length || 0) == 1 ? '* (anything)' : '* (anything else)');

						return (
							<tr key={child.fullRoute}>
								<td className="admin-pageselector__name">
									<span className="admin-pageselector__name-wrapper">
										<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5"
											strokeLinecap="round" strokeLinejoin="round">
											<path d="M2.97 12.92A2 2 0 0 0 2 14.63v3.24a2 2 0 0 0 .97 1.71l3 1.8a2 2 0 0 0 2.06 0L12 19v-5.5l-5-3-4.03 2.42Z" />
											<path d="m7 16.5-4.74-2.85" />
											<path d="m7 16.5 5-3" />
											<path d="M7 16.5v5.17" />
											<path d="M12 13.5V19l3.97 2.38a2 2 0 0 0 2.06 0l3-1.8a2 2 0 0 0 .97-1.71v-3.24a2 2 0 0 0-.97-1.71L17 10.5l-5 3Z" />
											<path d="m17 16.5-5-3" />
											<path d="m17 16.5 4.74-2.85" />
											<path d="M17 16.5v5.17" />
											<path d="M7.97 4.42A2 2 0 0 0 7 6.13v4.37l5 3 5-3V6.13a2 2 0 0 0-.97-1.71l-3-1.8a2 2 0 0 0-2.06 0l-3 1.8Z" />
											<path d="M12 8 7.26 5.15" />
											<path d="m12 8 4.74-2.85" />
											<path d="M12 13.5V8" />
										</svg>
										{name || description}
									</span>
								</td>
								<td className="admin-pageselector__id">
									{child.contentId && child.contentId > 0 ? child.contentId : ''}
								</td>
								<td className="admin-pageselector__actions">
									<Button xs variant="primary" outlined onClick={() => navigateTo(child.fullRoute || '')}>
										{`Browse`}
									</Button>
								</td>
							</tr>
						);
					})}
					{files?.map((child: RouterNodeMetadata) => {
						var name = child.name || '';
						var description = child.childKey || ((files?.length || 0) == 1 ? '* (anything)' : '* (anything else)');
						var isSelectable = !!(child.contentId && child.contentId > 0);
						var canBrowse = !!(child.hasChildren && child.fullRoute);

						return (
							<tr key={child.fullRoute}>
								<td className="admin-pageselector__name">
									{canBrowse ? (
										<a
											href="#"
											className="admin-pageselector__name-wrapper"
											onClick={(e) => {
												e.preventDefault();
												navigateTo(child.fullRoute || '');
											}}
										>
											<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
												strokeLinecap="round" strokeLinejoin="round">
												<path d="M11 21.73a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73z" />
												<path d="M12 22V12" />
												<polyline points="3.29 7 12 12 20.71 7" />
												<path d="m7.5 4.27 9 5.15" />
											</svg>
											{child.type == 'Page' ? description : name || description}
										</a>
									) : (
										<span className="admin-pageselector__name-wrapper">
											<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
												strokeLinecap="round" strokeLinejoin="round">
												<path d="M11 21.73a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73z" />
												<path d="M12 22V12" />
												<polyline points="3.29 7 12 12 20.71 7" />
												<path d="m7.5 4.27 9 5.15" />
											</svg>
											{child.type == 'Page' ? description : name || description}
										</span>
									)}
									{child.type == 'Page' && name && (
										<small className="admin-pageselector__name-subtitle">
											{name}
										</small>
									)}
								</td>
								<td className="admin-pageselector__id">
									{child.contentId && child.contentId > 0 ? child.contentId : ''}
								</td>
								<td className="admin-pageselector__actions">
									{canBrowse && (
										<Button xs variant="primary" outlined onClick={() => navigateTo(child.fullRoute || '')}>
											{`Browse`}
										</Button>
									)}
									{isSelectable && (
										<Button xs variant="primary" outlined onClick={() => handleSelect(child)}>
											{`Select`}
										</Button>
									)}
								</td>
							</tr>
						);
					})}
				</tbody>
			</table>
		);
	}

	var breadcrumbPaths = getBreadcrumbPaths();
	var breadcrumbParts = (path ? path.split('/') : []).filter(p => p);

	return (
		<div className="admin-pageselector mb-3">
			{props.label && (
				<label className="form-label admin-pageselector__label">
					{props.label}
				</label>
			)}

			<ul className="admin-pageselector__entries">
				{initValue.map((entry: PageSelectorValue) => (
					<li key={entry.id} className="admin-pageselector__entry">
						<div className="admin-pageselector__entry-content">
							<span className="admin-pageselector__entry-title">{entry.title}</span>
						</div>
						<Button sm outlined variant="danger" className="btn-entry-select-action btn-remove-entry"
							title={`Remove`}
							onClick={(e) => { e.preventDefault(); handleRemove(entry); }}>
							<i className="fal fa-fw fa-times"></i>
						</Button>
					</li>
				))}
			</ul>

			<input type="hidden" name={props.name} value={JSON.stringify(initValue.map((e: PageSelectorValue) => e.id))} />

			<footer className="admin-pageselector__footer">
				{atMax ? (
					<span className="admin-pageselector__max">
						{`Max of ${props.max} added`}
					</span>
				) : (
					<Button variant="primary" onClick={() => setIsOpen(true)}>
						{`Select Pages`}
					</Button>
				)}
			</footer>

			<Dialog
				isOpen={isOpen}
				onClose={() => setIsOpen(false)}
				title={`Select Pages`}
			>
				<div className="admin-pageselector__dialog">
					<nav className="admin-pageselector__breadcrumbs" aria-label="Breadcrumb">
						<ol className="breadcrumb">
							<li className="breadcrumb-item">
								<Link href="#" onClick={(e: any) => { e.preventDefault(); navigateTo(''); }}>
									{`Root`}
								</Link>
							</li>
							{breadcrumbParts.map((part: string, index: number) => (
								<li key={index} className="breadcrumb-item">
									<Link
										href="#"
										onClick={(e: any) => {
											e.preventDefault();
											navigateTo(breadcrumbPaths[index + 1]);
										}}
									>
										{part}
									</Link>
								</li>
							))}
						</ol>
					</nav>

					<div className="admin-pageselector__tree-container">
						{loading ? <Loading /> : renderCurrentNode()}
					</div>
				</div>
			</Dialog>
		</div>
	);
}
