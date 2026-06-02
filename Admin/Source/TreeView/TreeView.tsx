import { useState, useEffect } from 'react';
import { useRouter } from 'UI/Router';
import Icon from 'UI/Icon';
import Link from 'UI/Link';
import Input from 'UI/Input';
import Loading from 'UI/Loading';
import Button from 'UI/Button';
import { TreeNodeDetail } from 'Api/Pages';

/**
 * Props for the TreeView component
 */
export type TreeViewProps = {
	/**
	 * set true to allow items to be sorted (WIP)
	 */
	allowSelection?: boolean;

	/**
	 * set true to allow items to be selected (WIP)
	 */
	allowSorting?: boolean;

	/**
	 * set true to make default click = edit; action click then defaults to browse
	 * (set to false to browse by default, action click then defaults to edit) 
	 */
	clickToEdit?: boolean;

	/**
	 *
	 * @param path
	 */
	onLoadData: (path: string) => Promise<TreeNodeDetail | null>;
};

const TreeView: React.FC<TreeViewProps> = ({ allowSelection, allowSorting, clickToEdit, onLoadData }) => {
	const [currentNode, setCurrentNode] = useState<TreeNodeDetail | null>(null);
	const [sortColumn, setSortColumn] = useState("name");
	const [sortDirection, setSortDirection] = useState("asc");
	const { pageState } = useRouter();

	useEffect(() => {
		const { query } = pageState;
		var path = query?.get("path") || "";

		onLoadData(path)
			.then(result => {
				setCurrentNode(result);
			});

	}, [pageState]);

	function renderHeader(label: string, columnName: string) {

		if (!allowSorting) {
			return label;
		}

		let headerClass = ["btn", "admin-treeview__sort"];
		var sortActive = sortColumn == columnName;

		if (sortActive) {
			headerClass.push("admin-treeview__sort--active");
		}

		if (sortDirection == 'desc') {
			headerClass.push("admin-treeview__sort--desc");
		} else {
			headerClass.push("admin-treeview__sort--asc");
		}

		return <>
			<Button className={headerClass.join(' ')} tabIndex={-1} onClick={() => {
				setSortColumn(columnName);

				if (sortActive) {
					setSortDirection(sortDirection == "desc" ? "asc" : "desc");
				} else {
					setSortDirection("asc");
				}
			}}>
				{label}
				<svg xmlns="http://www.w3.org/2000/svg" className="admin-treeview__sort-down" viewBox="0 0 24 24" fill="none" stroke="currentColor"
					stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
					<circle cx="12" cy="12" r="10" />
					<path d="m16 10-4 4-4-4" />
				</svg>

				<svg xmlns="http://www.w3.org/2000/svg" className="admin-treeview__sort-up" viewBox="0 0 24 24" fill="none" stroke="currentColor"
					stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
					<circle cx="12" cy="12" r="10" />
					<path d="m8 14 4-4 4 4" />
				</svg>
			</Button>
		</>;
	}

	var renderCurrentNode = () => {
		if (!currentNode) {
			return null;
		}

		// Group up the ones with children first (the directories)
		var dirs = currentNode.children?.filter(child => child.hasChildren);
		var files = currentNode.children?.filter(child => {
			// Anything but pure intermediate nodes
			return child.type && child.type != "Group";
		});

		var currentPath = window.location.pathname;
		var baseUrl = currentPath + "?path=";

		return <table className="table ui-table table-hover admin-treeview">
			<thead>
				<tr>
					{allowSelection && <>
						<th className="admin-treeview__select">
							<Input type="checkbox" noWrapper label={`Select all`} />
						</th>
					</>}
					<th className="admin-treeview__name">
						{renderHeader(`Name`, 'name')}
					</th>
					<th className="admin-treeview__id">
						{renderHeader(`ID`, 'id')}
					</th>
					<th className="admin-treeview__actions">
						{allowSorting && <span>
							{`Actions`}
						</span>}
						{!allowSorting && <>
							{`Actions`}
						</>}
					</th>
				</tr>
			</thead>
			<tbody>
				{currentNode.children?.map(child => {
					var name = child.name;
					var description = child.hasChildren ?
						child.childKey || (dirs!.length == 1 ? '* (anything)' : '* (anything else)') :
						child.childKey || (files!.length == 1 ? '* (anything)' : '* (anything else)');
					var editUrl = child.editUrl;
					var browseUrl = child.hasChildren ? baseUrl + child.fullRoute : editUrl;

					var title : React.ReactNode = name;
					var subTitle : React.ReactNode = description;
					var clickUrl = clickToEdit ? editUrl : browseUrl;
					var createUrl = child.createUrl;
					var NameTag : "a" | "span" = clickUrl && clickUrl.length ? "a" : "span";

					if (child.type == "Page") {
						title = description;
						subTitle = name;
					}

					if (!title || (typeof title == "string" && !title.length)) {
						title = subTitle;
						subTitle = <>&nbsp;</>;
					}

					return <tr>
						{allowSelection && <>
							<td className="admin-treeview__select">
								<Input type="checkbox" noWrapper label={`Select`} />
							</td>
						</>}
						<td className="admin-treeview__name">
							{/* @ts-ignore */}
							<NameTag className="admin-treeview__name-wrapper" href={clickUrl}>
								{child.hasChildren && <>
									<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"
										stroke-linecap="round" stroke-linejoin="round">
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
								</>}
								{!child.hasChildren && <>
									<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
										stroke-linecap="round" stroke-linejoin="round">
										<path d="M11 21.73a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73z" />
										<path d="M12 22V12" />
										<polyline points="3.29 7 12 12 20.71 7" />
										<path d="m7.5 4.27 9 5.15" />
									</svg>
								</>}
								{title}
							</NameTag>
							<small className="admin-treeview__name-subtitle">
								{subTitle}
							</small>
						</td>
						<td className="admin-treeview__id">
							{child.contentId > 0 ? child.contentId : ``}
						</td>
						<td className="admin-treeview__actions">
							{clickToEdit && child.hasChildren && <>
								<Link href={browseUrl || undefined} xs variant="primary" outlined>
									{`Browse`}
								</Link>
							</>}
							{!clickToEdit && editUrl && editUrl.length > 0 && <>
								<Link href={editUrl} xs variant="primary" outlined>
									{`Edit`}
								</Link>
							</>}
							{createUrl && createUrl.length > 0 && <>
								<Link href={createUrl} xs variant="primary" outlined>
									{`Create`}
								</Link>
							</>}
						</td>
					</tr>
				})}
				{
					false && dirs && dirs.map(dir => {
						return <tr>
							<td>
								<Icon type='fa-folder' /> <Link href={baseUrl + dir.fullRoute}>{dir.childKey || (dirs!.length == 1 ? '* (anything)' : '* (anything else)')}</Link>
							</td>
						</tr>
					})
				}
				{
					false && files && files.map(file => {
						var fileName = file.childKey || (files!.length == 1 ? '* (anything)' : '* (anything else)');

						return <tr>
							<td>
								<h4><Link href={file.editUrl || undefined}>{fileName}</Link></h4>
								<h6>{file.name}</h6>
							</td>
						</tr>
					})
				}
			</tbody>
		</table>
	};

	return currentNode ? renderCurrentNode() : <Loading />;
};

export function buildBreadcrumbs(startUrl: string, startTitle: string, path: string, baseUrl: string) {
	var breadcrumbs = [
		{
			url: startUrl,
			title: startTitle
		}
	];

	if (path) {
		var pathParts = path.split('/');
		for (var i = 0; i < pathParts.length; i++) {
			var part = pathParts[i];

			if (part == '') {
				continue;
			}

			breadcrumbs.push({
				url: baseUrl + "?path=" + buildPath(pathParts, i + 1),
				title: part
			});

		}
	}

	return breadcrumbs;
}

export function buildPath(pathParts: string[], max: number) {
	var result = '';

	for (var i = 0; i < max; i++) {
		if (i != 0) {
			result += "/";
		}
		result += pathParts[i];
	}

	return result;
}


export default TreeView;