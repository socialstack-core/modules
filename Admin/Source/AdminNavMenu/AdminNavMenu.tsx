import { useState, useMemo } from "react";
import useApi from "UI/Functions/UseApi";
import adminNavMenuApi, { AdminNavMenuItem } from "Api/AdminNavMenuItem";
import Icon, { IconRef } from "UI/Icon";
import Link from "UI/Link";

export type AdminNavMenuProps = {
	navOpen: boolean;
};

const AdminNavMenu: React.FC<AdminNavMenuProps> = ({ navOpen }) => {
	const [filter, setFilter] = useState<string>("");
	const [viewType, setViewType] = useState<"list" | "grid">("list");

	const [items] = useApi(
		() =>
			adminNavMenuApi.listAll().then((response) =>
				[...(response.results ?? [])].sort((a, b) =>
					(a.title ?? "").localeCompare(b.title ?? "", undefined, { sensitivity: "base" })
				)
			),
		[]
	);

	// filter results
	const filteredItems = useMemo(() => {
		
		if (filter) {
			return items?.filter((item) => {
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
		
		return items;
	}, [items, filter]);

	return (
		<aside
			className={`admin-nav-menu ${navOpen ? "expanded" : ""} view-${viewType}`}
		>
			<div className={"admin-nav-container"}>
				{/* Filter Input */}
				<div className="nav-filter">
					<input
						type="text"
						placeholder="Search menu..."
						value={filter}
						onInput={(e) => setFilter(e.currentTarget.value)}
					/>
				</div>

				<div className="link-container">
					{/* Grouped Items */}
					{filteredItems?.filter((item) => item.parentId === 0)
						.map((group) => {
							const children = filteredItems.filter(
								(child) => group.id === child.parentId
							);
							
							if (children.length === 0) {
								return null;
							}

							return (
								<div className="link-group" key={group.key}>
									<h4>{group.title}</h4>

									{children.map((child) => (
										<AdminNavMenuSingleItem key={child.key} item={child} />
									))}
								</div>
							);
						})}

					{/* Other Section */}
					<div className="link-group">
						<h4>Other</h4>
						{filteredItems
							?.filter((i) => i.parentId === 0 && i.url)
							.map((item) => (
								filter ? (
									item.title?.toLowerCase().includes(filter) && <AdminNavMenuSingleItem key={item.key} item={item} />
								) : <AdminNavMenuSingleItem item={item} key={item.key} />
							))}
					</div>
				</div>
			</div>

			{/* Sticky Bottom Toggle */}
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
		</aside>
	);
};

const AdminNavMenuSingleItem: React.FC<{ item: AdminNavMenuItem }> = ({
																		  item,
																	  }) => {
	return (
		<div className="admin-nav-menu-item">
			<Link href={item.url ?? '#'}>
				<div className="icon">
					{item.iconRef ? (
						<IconRef fileRef={item.iconRef} />
					) : (
						<Icon type="fa:rocket" />
					)}
				</div>
				<div className="title">{item.title}</div>
			</Link>
		</div>
	);
};

export default AdminNavMenu;
