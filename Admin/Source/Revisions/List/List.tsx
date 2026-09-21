import Table from 'UI/Table';
import Time from 'UI/Time';
import Link from 'UI/Link';
import { User } from 'Api/User';
import Button from 'UI/Button';
import ConfirmDialog from 'UI/Dialog/ConfirmDialog';
import { useState } from 'react';

/**
 * Props for the List component.
 */
interface ListProps {
	contentType: string,
	id: number
}

const userLabel = (id: uint, user?: User) => {
	let userName = user ?
		user.fullName || user.email || user.username : '';

	return userName || `User #${id}`;
};

/**
 * The List React component.
 * @param props React props.
 */
const List: React.FC<ListProps> = <T extends Content<uint>>(props: ListProps) => {

	// Api is expected to be an ApiEndpoints object.
	var api = require('Api/' + props.contentType).default as AutoController<T, uint>;

	const [confirmRemove, setConfirmRemove] = useState<any>(null);

	const renderEmpty = (colspan: int) => {
		return <>
			<tr>
				<td colSpan={colspan}>
					<span className="ui-not-found">
						{`None found`}
					</span>
				</td>
			</tr>
		</>;
	}

	const renderDraftCaption = () => {
		return <>
			{`Drafts`}
		</>;
	};

	const renderDraftHeader = () => {
		return <>
			<tr>
				<th>
					{`Date created`}
				</th>
				<th className="drafts-table__author">
					{`Author`}
				</th>
				<th className="drafts-table__actions">
					{`Actions`}
				</th>
			</tr>
		</>;
	};

	const renderHistoryCaption = () => {
		return <>
			{`Edit history`}
		</>;
	};

	const renderHistoryHeader = () => {
		return <>
			<tr>
				<th>
					{`Date created`}
				</th>
				<th>
					{`Action`}
				</th>
				<th className="history-table__author">
					{`Author`}
				</th>
				<th className="history-table__actions">
					{`Actions`}
				</th>
			</tr>
		</>;
	};

	const renderDraftEntry = (entry: any) => {
		const createdBy = userLabel(entry.userId, entry.creatorUser);
		const realUser = entry.impersonatorUserId ? userLabel(entry.impersonatorUserId, entry.realUser) : undefined;

		return <>
			<tr className="drafts-table__row">
				<td className="drafts-table__col">
					<Time date={entry.createdUtc} />
				</td>
				<td className="drafts-table__col">
					{entry.userId ? <Link href={`/en-admin/user/${entry.userId}`}>
						{createdBy}
					</Link> : `System`}
					{
						realUser && <>
							{` impersonated by `}
							<Link href={`/en-admin/user/${entry.impersonatorUserId}`}>
								{realUser}
							</Link>
						</>
					}
				</td>
				<td className="drafts-table__col drafts-table__col--actions">
					<div className="drafts-table__col--actions-internal">
						<Link xs outlined href={`/en-admin/${props.contentType.toLowerCase()}/revision/${entry.id}`} className="drafts-table__link">
							{`View`}
						</Link>
						<Button xs outlined variant="danger" aria-label={`Remove draft`} onClick={() => setConfirmRemove(entry)} className="drafts-table__remove">
							<i className="fr fr-trash-alt"></i>
							{`Remove`}
						</Button>
					</div>
				</td>
			</tr>
		</>;
	};

	const renderHistoryEntry = (entry: any) => {
		const createdBy = userLabel(entry.userId, entry.creatorUser);
		const realUser = entry.impersonatorUserId ? userLabel(entry.impersonatorUserId, entry.realUser) : undefined;

		let action = `Other`;

		switch (entry.actionType) {
			case 1:
				action = `Created`;
				break;
			case 2:
				action = `Edited`;
				break;
			case 3:
				action = `Deleted`;
				break;
		}

		return <>
			<tr className="history-table__row">
				<td className="history-table__col">
					<Time date={entry.createdUtc} />
				</td>
				<td className="history-table__col">
					{action}
				</td>
				<td className="history-table__col">
					{entry.userId ? <Link href={`/en-admin/user/${entry.userId}`}>
						{createdBy}
					</Link> : `System`}
					{
						realUser && <>
							{` impersonated by `}
							<Link href={`/en-admin/user/${entry.impersonatorUserId}`}>
								{realUser}
							</Link>
						</>
					}
				</td>
				<td className="history-table__col history-table__col--actions">
					<Link xs outlined href={`/en-admin/${props.contentType.toLowerCase()}/revision/${entry.id}`} className="history-table__link">
						{`View`}
					</Link>
				</td>
			</tr>
		</>;

	};

	return (
		<div className="ui-revisions-list">
			<Table
				source={(filter?: ListFilter, includes?: ApiInclude[]) => filter ? api.revisionList(filter, includes) : api.revisionListAll(includes)}
				className="drafts-table"
				includes={['creatorUser', 'realUser']}
				filter={{
					query: 'ContentId=? and IsDraft=?',
					args: [props.id, true],
					sort: {
						field: 'CreatedUtc',
						direction: 'desc'
					}
				}}
				orNone={() => renderEmpty(3 as int)}
				onCaption={renderDraftCaption}
				captionAbove
				onHeader={renderDraftHeader}
				paged
			>
				{renderDraftEntry}
			</Table>

			<Table
				source={(filter?: ListFilter, includes?: ApiInclude[]) => filter ? api.revisionList(filter, includes) : api.revisionListAll(includes)} 
				className="history-table"
				includes={['creatorUser', 'realUser']}
				filter={{
					query: 'ContentId=? and IsDraft=?',
					args: [props.id, false],
					sort: {
						field: 'CreatedUtc',
						direction: 'desc'
					}
				}}
				orNone={() => renderEmpty(4 as int)}
				onCaption={renderHistoryCaption}
				captionAbove
				onHeader={renderHistoryHeader}
				paged
			>
				{renderHistoryEntry}
			</Table>
			{confirmRemove && <>
				<ConfirmDialog
					variant="danger"
					title={`Remove draft`}
					isOpen={true}
					onClose={() => setConfirmRemove(null)}
					confirmText={`Yes, remove it`}
					confirmCallback={() => {
						return api.deleteRevision(confirmRemove.id);
					}}>
					<p>
						{`Are you sure you wish to remove this draft? This cannot be undone.`}
					</p>
				</ConfirmDialog>
			</>}
		</div>
	);
}

export default List;