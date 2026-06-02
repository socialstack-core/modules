/**
 * ================================================
 * Header Account Component
 * ================================================
 * This component manages user account interactions
 * such as:
 *  - Viewing the current account
 *  - Impersonating another user (because debugging
 *    as "yourself" is too mainstream)
 *  - Ending impersonation (back to reality)
 *  - Signing out
 *
 * It uses a dropdown menu and modal to make this
 * process as user-friendly as possible, while also
 * safeguarding against users impersonating *up*
 * the food chain (no instant admin upgrades here).
 * ================================================
 */

// =========================
// UI Imports
// =========================

import Icon from "UI/Icon";             // Fancy little icons (because plain text is boring)
import Dropdown from "UI/Dropdown";     // Dropdown menu to hold user actions
import Modal from "UI/Modal";           // Pop-up modal for impersonation selection
import Alert from "UI/Alert";           // Warnings for reckless impersonation adventures
import Table from "UI/Table";           // Tabular display of users for impersonation
import Input from "UI/Input";           // Search/filter input for user list
import Button from "UI/Button";

// =========================
// Session Handling
// =========================

import { useSession } from "UI/Session"; // Custom hook to manage session state (current + real user context)

// =========================
// API Imports
// =========================

import userApi, { User } from "Api/User";   // API functions for user operations (logout, impersonate, etc.)
import { ListFilter } from "Api/Startup";   // Filtering model for fetching user lists

// =========================
// Utilities
// =========================

import { useState } from "react";          // React state management
import Debounce from "UI/Functions/Debounce"; // Debounce utility to avoid hammering API while typing


/**
 * Account Component
 *
 * @component
 * @returns {JSX.Element} A dropdown menu that allows the current user to:
 *   - Manage their account
 *   - Impersonate another user (with restrictions)
 *   - End impersonation (back to their true identity)
 *   - Return to the site
 *   - Sign out entirely
 *
 * @remarks
 * This component is the "account hub" for users in the admin interface.
 * It cleverly prevents:
 *   - Users impersonating *themselves* (pointless, really).
 *   - Role escalation (no free promotions).
 *
 * @example
 * <Account />
 */
const Account = () => {

	// =========================
	// Session context
	// =========================
	const { session, setSession } = useSession();
	const { user } = session;

	// =========================
	// Local State
	// =========================
	const [showImpersonationOpen, setShowImpersonationOpen] = useState(false); // Modal visibility toggle
	const [impersonationFilter, setImpersonationFilter] = useState<string>();   // Current filter query for users
	const [debounce] = useState<Debounce<string>>(() => {
		// Delay filter input to avoid nuking the server with requests per keystroke
		return new Debounce((query: string) => {
			setImpersonationFilter(query);
		})
	});

	// =========================
	// Derived State
	// =========================
	let listFilter: ListFilter | undefined = undefined;

	if (impersonationFilter) {
		// Magic incantation to fetch users whose 
		// FullName, Username, or Email match the query
		listFilter = {
			query: 'FullName contains ? or Username contains ? or Email contains ?',
			args: [impersonationFilter, impersonationFilter, impersonationFilter]
		} as ListFilter;
	}

	// =========================
	// Local Functions
	// =========================

	/**
	 * Render table header for user impersonation list.
	 * @returns {JSX.Element} Table header row
	 */
	const renderHeader = () => {
		return <>
			<tr>
				<th>ID</th>
				<th>User name</th>
				<th>Email address</th>
				<th>Role</th>
				<th>&nbsp;</th>
			</tr>
		</>;
	}

	/**
	 * Render a single table entry for a user.
	 * Skips current user (because inception-level
	 * impersonation is not allowed).
	 *
	 * Also blocks impersonating users with higher
	 * privileges (sorry, no "become admin" cheat here).
	 *
	 * @param {User} entry The user object to render
	 * @returns {JSX.Element | undefined} Table row or nothing if disqualified
	 */
	const renderEntry = (entry : User) => {

		// Don't include current user
		if (user?.id == entry.id) {
			return;
		}

		// Disallow role elevation
		if (entry.role && user?.role && entry.role < user.role) {
			return;
		}

		return <tr>
			<td>{entry.id}</td>
			<td>{entry.username}</td>
			<td>{entry.email}</td>
			<td>{entry.role}</td>
			<td>
				<Button sm outlined onClick={() => impersonateUser(entry.id)}>
					{`Select`}
				</Button>
			</td>
		</tr>;
	}

	/**
	 * Impersonates the selected user.
	 * Redirects the page to root once session is updated.
	 *
	 * @param {number} userId The ID of the user to impersonate
	 * @returns {Promise<void>} Resolves when impersonation completes
	 */
	const impersonateUser = (userId : uint) => {
		return userApi.impersonate(setSession, userId).then(response => {
			setSession(response as SessionResponse);
			window.location.href = '/';
		});
	}

	/**
	 * Ends current impersonation session.
	 * Reloads page to refresh privileges and UI.
	 *
	 * @returns {Promise<void>} Resolves when impersonation ends
	 */
	const endImpersonation = () => {
		return userApi.unpersonate(setSession).then(response => {
			window.location.reload();
		});
	}

	// =========================
	// Render
	// =========================
	return (
		<>
			<Dropdown
				items={[
					// My account
					{
						text: `My account`,
						href: '/en-admin/user/' + user?.id
					},
					// Impersonation options
					session.realuser ? {
						text: `End impersonation`,
						onClick: () => endImpersonation()
					} : {
						text: `Impersonate ...`,
						onClick: () => setShowImpersonationOpen(true),
					},
					{ divider: true },
					// Back to main site
					{
						text: `Return to site`,
						href: '/',
					},
					{ divider: true },
					// Sign out
					{
						text: `Sign out`,
						onClick: () => {
							userApi.logout(setSession)
								.then(() => {
									window.location.reload();
								});
						}
					}
				]}
				align="right"
				label={
					<>
						<span className="admin-page__header-greeting">
							{`Hi ${user?.firstName || user?.username}`}
						</span> <Icon type={'fr fa-user'} />
					</>
				}
			/>

			{/* Impersonation modal */}
			{showImpersonationOpen &&
				<Modal
					visible
					isLarge
					title={`Select a User to Impersonate`}
					className={"admin-page__impersonation-modal"}
					onClose={() => setShowImpersonationOpen(false)}
				>
					<Alert variant="warning">
						<h3 className={"admin-page__impersonation-title"}>
							{`Please note`}
						</h3>
						{`Selecting a user without administrative privileges will cause admin views to become inaccessible.
						To return to admin view, click the cog icon and select "End impersonation".`}
					</Alert>

					<Input
						autoFocus
						type={'text'}
						onInput={(ev) => {
							debounce.handle((ev.target as HTMLInputElement).value);
						}}
						defaultValue={impersonationFilter}
						placeholder={`Filter by ID, username or email`}
					/>

					<Table
						over={userApi}
						filter={listFilter}
						paged
						className="table-sm admin-page__impersonation-userlist"
						onHeader={ renderHeader }
					>
						{renderEntry}
					</Table>
				</Modal>
			}
		</>
	)
}

export default Account;
