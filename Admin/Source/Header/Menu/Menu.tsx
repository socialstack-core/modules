import { useState, useMemo } from "react";
import useApi from "UI/Functions/UseApi";
import adminNavMenuApi, { AdminNavMenuItem } from "Api/AdminNavMenuItem";
import Icon, { IconRef } from "UI/Icon";
import Link from "UI/Link";
import Input from "UI/Input";
import Alert from "UI/Alert";

export type AdminNavMenuProps = {
};

const AdminNavMenu: React.FC<AdminNavMenuProps> = () => {
	const [filter, setFilter] = useState<string>("");
	const [viewType, setViewType] = useState<"list" | "grid">("list");

	const [items] = useApi(() =>
			adminNavMenuApi.list(),
		[]
	);

	// filter results
	const filteredItems = useMemo(() => {
		
		if (filter) {
			return items.results?.filter((item) => {
				// allow categories/groups
				if (item.parentId == 0) {
					return true;
				}
				if (item.title?.toLowerCase().includes(filter.toLowerCase())) {
					return true;
				}
				return false;
			})
		}
		
		return items?.results;
	}, [items, filter]);

	return (
		<div className={`admin-page__menu admin-page__menu--${viewType}`}>
			{/* search */}
			<header className="admin-page__menu-filter">
				<Input
					noWrapper
					className="form-control ui-form-control ui-form-control--sm"
					type="search"
					placeholder={`Search menu...`}
					value={filter}
					onInput={(e) => setFilter(e.currentTarget.value)}
				/>
			</header>

			<Alert variant="info" className="admin-page__menu-none-found">
				{`No menu options match "${filter}"`}
			</Alert>

			{/* links */}
			<div className="admin-page__menu-links">

				{/* grouped items */}
				{filteredItems?.filter((item) => item.parentId === 0)
					.map((group) => {
						const children = filteredItems.filter(
							(child) => group.id === child.parentId
						);
							
						if (children.length === 0) {
							return null;
						}

						return (
							<div className="admin-page__menu-link-group" key={group.key}>
								<h3 className="admin-page__menu-link-group-title">
									{group.title}
								</h3>
								{children.map((child) => (
									<AdminNavMenuSingleItem key={child.key} item={child} />
								))}
							</div>
						);
					})}

				{/* other items */}
				<div className="admin-page__menu-link-group">
					<h3 className="admin-page__menu-link-group-title">
						{`Other`}
					</h3>
					{filteredItems
						?.filter((i) => i.parentId === 0 && i.url)
						.map((item) => (
							filter ? (
								item.title?.toLowerCase().includes(filter) && <AdminNavMenuSingleItem key={item.key} item={item} />
							) : <AdminNavMenuSingleItem item={item} key={item.key} />
						))}
				</div>

							{/* Sticky Bottom Toggle */}
							{/*
			<div className="nav-toggle-bar">
				<div className={'inner'}>
					<button
						className={viewType === "list" ? "active" : ""}
						onClick={() => setViewType("list")}
					>
						<Icon type="fa:list" /> List
					</button>
					<button
						className={viewType === "grid" ? "active" : ""}
						onClick={() => setViewType("grid")}
					>
						<Icon type="fa:th" /> Grid
					</button>
				</div>
			</div>
*/}
			</div>
		</div>
	);
};

const AdminNavMenuSingleItem: React.FC<{ item: AdminNavMenuItem }> = ({item}) => {
	return (
		<Link href={item.url ?? '#'} className="admin-page__menu-link">
			{item.iconRef ? (
				<IconRef fileRef={item.iconRef} fixedWidth />
			) : <>
				{/* render empty spacer */}
				<Icon fixedWidth />
			</>}
			<span>{item.title}</span>
		</Link>
	);
};

export default AdminNavMenu;
