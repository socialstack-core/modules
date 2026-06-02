import TreeView, { buildBreadcrumbs } from 'Admin/TreeView';
import { useRouter } from 'UI/Router';
import { useState, useEffect } from 'react';
import productAttributeApi, {ProductAttribute} from 'Api/ProductAttribute';
import Input from "UI/Input";
import {ApiList} from "UI/Functions/WebRequest";
import Loading from "UI/Loading";
import Link from "UI/Link";
import Paginator from "UI/Paginator";
import AdminPage from 'Admin/AdminPage';
import Footer from 'Admin/Footer';
import Button from 'UI/Button';

type TreeViewType = 'tree' | 'list';

interface ProductAttributeTreeProps {
	noCreate?: boolean
}

export default function ProductAttributeTree(props: ProductAttributeTreeProps) {
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
				query: '',
				args: [],
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
				query: '',
				args: [],
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

	return <>
		<AdminPage.SubHeader
			title={`Edit Product Attributes`}
			breadcrumbs={breadcrumbs}>
			<div className="btn-group ui-btn-group view-toggle" role="group" aria-label={`Select view style`}>
				<Input
					type="radio"
					noWrapper
					label={`Tree`}
					groupIcon="fr-grid"
					groupVariant="primary"
					checked={viewType === "tree"}
					onChange={() => setViewType("tree")}
					name="view-style"
				/>
				<Input
					type="radio"
					noWrapper
					label={`List`}
					groupIcon="fr-th-list"
					groupVariant="primary"
					checked={viewType === "list"}
					onChange={() => setViewType("list")}
					name="view-style"
				/>
			</div>
		</AdminPage.SubHeader>
		<AdminPage.ContentWrapper>
			<AdminPage.Filters
				searchText={searchQuery}
				placeholder={`Filter attributes`}
				onInput={(ev) => {
					setSearchQuery((ev.target as HTMLInputElement).value)
					setViewType("list")
				}}
				onChange={(ev) => {
					setSearchQuery((ev.target as HTMLInputElement).value)
					setViewType("list")
				}}
				onFocus={() => {
					setViewType("list");
				}}>
				{viewType === 'list' && <>
					<Paginator
						overviewOnly
						totalResults={attributes?.totalResults}
						pageSize={pageSize}
						pageIndex={currentPage}
						onChange={(toPage: number) => {
							updateQuery({ page: toPage.toString() })
						}}
					/>
				</>}
			</AdminPage.Filters>

			<AdminPage.Content>
				{viewType === 'tree' ?
						<TreeView onLoadData={(path) => {

							return productAttributeApi
								.getTreeNodePath(path)
								.then(resp => {

									const anyNode = resp?.self ?? (resp?.children ? resp?.children[0] : null);

									if (!anyNode) {
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
								<ListView
									userCanEdit={userCanEditAttribute}
									results={attributes}
									sortOrder={sortOrder}
									sortField={sortField}
									setSortField={(field) => setSortField(field)}
									setSortDirection={order => setSortOrder(order)}
								/>
							<Paginator
								paginatorOnly dockBottom
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
			</AdminPage.Content>
		</AdminPage.ContentWrapper>
		<Footer>
			{!props.noCreate && <>
				<Link href={addGroupUrl} variant="primary" outlined>
					{`New group`}
				</Link>
				<Link href={addUrl} variant="primary">
					{`New attribute`}
				</Link>
			</>}
		</Footer>
	</>;
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
	const {
		results, userCanEdit,
		sortField, sortOrder,
		setSortField, setSortDirection
	} = props;
	
	const changeSortOrder = (field: string) => {
		
		if (field === sortField) {
			// switch dir. 
			setSortDirection(sortOrder === 'ASC' ? 'DESC' : 'ASC');
			return;
		}
		
		setSortField(field);
		setSortDirection('ASC');
	}
	
	return (
		<table className="table ui-table ui-table--sm table-hover">
			<thead>
				<tr>
					<th>
						<Button sm variant="link" onClick={() => changeSortOrder('key')}>
							<span>
								{`Name`}
							</span>
							{sortField === 'key' && (
								<i className={'fr fr-chevron-' + (sortOrder === 'asc' ? 'down' : 'up')} />
							)}
						</Button>
					</th>
					<th>
						<Button sm variant="link" onClick={() => changeSortOrder('id')}>
							<span>
								{`ID`}
							</span>
							{sortField === 'id' && (
								<i className={'fr fr-chevron-' + (sortOrder === 'asc' ? 'down' : 'up')} />
							)}
						</Button>
					</th>
					<th>
						{`Actions`}
					</th>
				</tr>
			</thead>
			<tbody>
			{results.results.map((attr) => {
				return (
					<tr>
						<td>{attr.name}</td>
						<td>{attr.id}</td>
						<td className={'admin-treeview__actions'}>
							<Link xs variant="primary" outlined
								href={'/en-admin/productattribute/' + attr.id + (!userCanEdit ? '/values' : '')}>
								{`Edit`}
							</Link>
						</td>
					</tr>
				)
			})}
			</tbody>
		</table>
	)
}
