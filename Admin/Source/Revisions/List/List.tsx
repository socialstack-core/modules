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

	const renderEmpty = () => {
		return <tr>
			{`None found`}
		</tr>;
	}

	const renderDraftHeader = () => {
		return <>
			<th>
				{`Date created`}
			</th>
			<th>
				{`Author`}
			</th>
			<th>
				{`Actions`}
			</th>
		</>;
	};

	const renderHistoryHeader = () => {
		return <>
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
		</>;
	};

	const renderDraftEntry = (entry: any) => {
		const createdBy = userLabel(entry.userId, entry.creatorUser);
		const realUser = entry.impersonatorUserId ? userLabel(entry.impersonatorUserId, entry.realUser) : undefined;

		return <tr>
			<td><Time date={entry.createdUtc} /></td>
			<td>
				{entry.userId ? <Link href={'/en-admin/user/' + entry.userId}>
					{createdBy}
				</Link> : `System`}
				{
					realUser && <>
						{` impersonated by `}
						<Link href={'/en-admin/user/' + entry.impersonatorUserId}>
							{realUser}
						</Link>
					</>
				}
			</td>
			<td>
				<Link href={'/en-admin/' + props.contentType.toLowerCase() + '/revision/' + entry.id}>
					{`View`}
				</Link>
			</td>
		</tr>
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

		return <tr>
			<td><Time date={entry.createdUtc} /></td>
			<td>
				{action}
			</td>
			<td>
				{entry.userId ? <Link href={'/en-admin/user/' + entry.userId}>
					{createdBy}
				</Link> : `System`}
				{
					realUser && <>
						{` impersonated by `}
						<Link href={'/en-admin/user/' + entry.impersonatorUserId}>
							{realUser}
						</Link>
					</>
				}
			</td>
			<td>
				<Link href={'/en-admin/' + props.contentType.toLowerCase() + '/revision/' + entry.id}>
					{`View`}
				</Link>
			</td>
		</tr>
	};

	return (
		<div className="ui-revisions-list">
			<h2 className="ui-page__subtitle">
				{`Drafts`}
			</h2>
			<Table
				source={api.revisionList}
				includes={['creatorUser', 'realUser']}
				filter={{
					query: 'ContentId=? and IsDraft=?',
					args: [props.id, true],
					sort: {
						field: 'CreatedUtc',
						direction: 'desc'
					}
				}}
				orNone={() => renderEmpty()}
				onHeader={renderDraftHeader}
				paged
			>
				{renderDraftEntry}
			</Table>
			<h2 className="ui-page__subtitle">
				{`Edit history`}
			</h2>
			<Table
				source={api.revisionList}
				includes={['creatorUser', 'realUser']}
				filter={{
					query: 'ContentId=? and IsDraft=?',
					args: [props.id, false],
					sort: {
						field: 'CreatedUtc',
						direction: 'desc'
					}
				}}
				orNone={() => renderEmpty()}
				onHeader={renderHistoryHeader}
				paged
			>
				{renderHistoryEntry}
			</Table>
		</div>
	);
}

export default List;