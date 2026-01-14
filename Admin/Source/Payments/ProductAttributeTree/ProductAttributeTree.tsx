import TreeView, { buildBreadcrumbs } from 'Admin/TreeView';
import SubHeader from 'Admin/SubHeader';
import { useRouter } from 'UI/Router';
import { useState, useEffect } from 'react';
import productAttributeApi, {ProductAttribute} from 'Api/ProductAttribute';
import Input from "UI/Input";
import {ApiList} from "UI/Functions/WebRequest";
import Loop from "UI/Loop";
import Loading from "UI/Loading";
import Button from "UI/Button";
import Link from "UI/Link";
import Paginator from "UI/Paginator";

type TreeViewType = 'tree' | 'list';


export default function ProductAttributeTree(props) {
	var addUrl = '/en-admin/productattribute/add';
	var addGroupUrl = '/en-admin/productattributegroup/add';
	const { pageState, updateQuery } = useRouter();
	const { query } = pageState;
	var path = query?.get("path") || "";

	var breadcrumbs = buildBreadcrumbs(
		'/en-admin/productattribute',
		`Product Attributes`,
		path,
		'/en-admin/productattribute'
	);
	
	const [viewType, setViewType] = useState<TreeViewType>('tree');
	const [searchQuery, setSearchQuery] = useState(query.get("q") ?? "");
	const [attributes, setAttributes] = useState<ApiList<ProductAttribute>>();
	const [userCanEditAttribute, setUserCanEditAttribute] = useState<boolean>(false);
	const [sortField, setSortField] = useState<string>('id');
	const [sortOrder, setSortOrder] = useState<string>('ASC');

	// grab the current page
	const currentPage: uint = (pageState.query?.has("page") ? parseInt(pageState.query?.get("page") ?? "1") : 1) as uint;

	// and the page size.
	const pageSize: uint = (pageState.query?.has('limit') ? parseInt(pageState.query?.get("limit") ?? "10") : 20) as uint;

	useEffect(() => {
		
		if (!attributes) {
			productAttributeApi.list({
				pageIndex: (currentPage - 1 as uint),
				pageSize,
				sort: {
					field: sortField,
					direction: sortOrder.toLowerCase()
				}
			})
			.then((response) => {
				setAttributes(response);
			})
		}
		
	}, [attributes, sortField, sortOrder, pageSize, currentPage]);

	useEffect(() => {

		if (!searchQuery || searchQuery.length == 0) {
			productAttributeApi.list({
				pageIndex: (currentPage - 1) as uint,
				pageSize: pageSize,
				sort: {
					field: sortField,
					direction: sortOrder.toLowerCase()
				}
			})
				.then((response) => {
					setAttributes(response);
				})
		}

	}, [searchQuery, sortField, sortOrder, pageSize, currentPage]);

	useEffect(() => {
		
		if (searchQuery && searchQuery.length != 0) {
			productAttributeApi.list({
				pageIndex: (currentPage - 1) as uint,
				pageSize: pageSize,
				sort: {
					field: sortField,
					direction: sortOrder.toLowerCase()
				},
				query: "(Name contains ? or Key contains ?)",
				args: [searchQuery, searchQuery]
			})
			.then((response) => {
				setAttributes(response);
			})
		}
		
	}, [searchQuery, sortField, sortOrder, pageSize, currentPage]);
	
	
	return (
		<>
			<SubHeader title={`Edit Product Attributes`} breadcrumbs={breadcrumbs} />
			<div className="sitemap__wrapper product-category-tree">
				<div className="sitemap__internal">
					<div className="page-controls">
						<div className="btn-group ui-btn-group view-toggle" role="group" aria-label="Select view style">
							<Input
								type="radio"
								noWrapper
								label="Tree"
								groupIcon="fr-grid"
								groupVariant="primary"
								value={viewType === "tree"}
								onChange={() => setViewType("tree")}
								name="view-style"
							/>
							<Input
								type="radio"
								noWrapper
								label="List"
								groupIcon="fr-th-list"
								groupVariant="primary"
								value={viewType === "list"}
								onChange={() => setViewType("list")}
								name="view-style"
							/>
						</div>
						<div className="product-search">
							<Input
								type="search"
								defaultValue={searchQuery}
								onInput={(ev) => {
									setSearchQuery((ev.target as HTMLInputElement).value)
									setViewType("list")
								}}
								onFocus={() => {
									setViewType("list");
								}}
								placeholder="Filter attributes"
							/>
						</div>
					</div>
					
					{
						viewType === 'tree' ? 
						<TreeView onLoadData={(path) => {

							return productAttributeApi
								.getTreeNodePath(path)
								.then(resp => {
									
									const anyNode = resp?.self ?? (resp?.children ? resp?.children[0] : null);
									
									if (!anyNode)
									{
										return resp;
									}
									
									if (!userCanEditAttribute && anyNode.editUrl) {
										if (!anyNode.editUrl.endsWith('values')) {
											setUserCanEditAttribute(true);
										}
									}
									return resp;
								});
	
						}} /> : 
						(attributes ? (
							<>
								<Paginator
									totalResults={attributes?.totalResults}
									pageSize={pageSize}
									pageIndex={currentPage}
									onChange={(toPage: number) => {
										updateQuery({ page: toPage.toString() })
									}}
								/>
								<ListView 
									userCanEdit={userCanEditAttribute} 
									results={attributes}
									sortOrder={sortOrder}
									sortField={sortField}
									setSortField={(field) => setSortField(field)}
									setSortDirection={order => setSortOrder(order)}
								/>
								<Paginator
									totalResults={attributes?.totalResults}
									pageSize={pageSize}
									pageIndex={currentPage}
									onChange={(toPage: number) => {
										updateQuery({ page: toPage.toString() })
									}}
								/>
							</>
						) : <Loading />)
					}
				</div>
				{!props.noCreate && <>
					<footer className="admin-page__footer">
						<a href={addGroupUrl} className="btn btn-primary">
							{`New group`}
						</a>
						<a href={addUrl} className="btn btn-primary">
							{`New attribute`}
						</a>
					</footer>
				</>}
			</div>
		</>
	);
}

type ListViewProps = { 
	results: ApiList<ProductAttribute>,
	userCanEdit: boolean, 
	sortField: string,
	sortOrder: string,
	setSortField: (field: string) => void, 
	setSortDirection: (direction: string) => void
};

const ListView = (props: ListViewProps) => {
	
	const { results, userCanEdit } = props;
	
	const changeSortOrder = (field: string) => {
		
		if (field === props.sortField) {
			// switch dir. 
			props.setSortDirection(props.sortOrder === 'ASC' ? 'DESC' : 'ASC');
			return;
		}
		
		props.setSortField(field);
		props.setSortDirection('ASC');
	}
	
	return (
		<div className={'admin-page__internal'}>
			<table className={'table'}>
				<thead>
					<tr>
						<th 
							onClick={() => changeSortOrder('key')}
						>Name {props.sortField === 'key' ? <i className={props.sortOrder === 'ASC' ? 'fas fa-chevron-down' : 'fas fa-chevron-up'}/> : null}</th>
						<th 
							onClick={() => changeSortOrder('id')}
						>ID {props.sortField === 'id' ? <i className={props.sortOrder === 'ASC' ? 'fas fa-chevron-down' : 'fas fa-chevron-up'}/> : null}</th>
						<th>Actions</th>
					</tr>
				</thead>
				<tbody>
				{results.results.map((attr) => {
					return (
						<tr>
							<td>{attr.name}</td>
							<td>{attr.id}</td>
							<td className={'admin-treeview__actions'}>
								<Link
									className={'btn btn-sm btn-outline-primary'}
									href={'/en-admin/productattribute/' + attr.id + (!userCanEdit ? '/values' : '')}>
									{`Edit`}
								</Link>
							</td>
						</tr>
					)
				})}
				</tbody>
			</table>
		</div>
	)
}
