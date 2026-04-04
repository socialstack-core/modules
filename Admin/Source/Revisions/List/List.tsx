import Table from 'UI/Table';
import Time from 'UI/Time';
import Link from 'UI/Link';
import { User } from 'Api/User';

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
const List: React.FC<ListProps> = (props) => {

	// Api is expected to be an ApiEndpoints object.
	var api = require('Api/' + props.contentType).default;

	const renderEmpty = (colspan) => {
		return <>
			<tr>
				<td colspan={colspan}>
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
				<th>
					{`Author`}
				</th>
				<th>
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
				<th>
					{`Author`}
				</th>
				<th>
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
					<Link href={`/en-admin/${props.contentType.toLowerCase()}/revision/${entry.id}`} className="drafts-table__link">
						{`View`}
					</Link>
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
				source={api.revisionList}
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
				orNone={() => renderEmpty(3)}
				onCaption={renderDraftCaption}
				captionAbove
				onHeader={renderDraftHeader}
				paged
			>
				{renderDraftEntry}
			</Table>

			<Table
				source={api.revisionList}
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
				orNone={() => renderEmpty(4)}
				onCaption={renderHistoryCaption}
				captionAbove
				onHeader={renderHistoryHeader}
				paged
			>
				{renderHistoryEntry}
			</Table>
		</div>
	);
}

export default List;