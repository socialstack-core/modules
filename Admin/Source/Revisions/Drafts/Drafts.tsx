import Table from 'UI/Table';
import Time from 'UI/Time';
import Link from 'UI/Link';

/**
 * Props for the Drafts component.
 */
interface DraftsProps {
	contentType: string,
	hideTitle?: boolean
}

/**
 * The Drafts React component.
 * @param props React props.
 */
const Drafts: React.FC<DraftsProps> = (props) => {

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

	const renderDraftEntry = (entry: any) => {
		return <tr>
			<td><Time date={entry.createdUtc} /></td>
			<td>{
				entry.creatorUser ? entry.creatorUser.fullName || entry.creatorUser.username : `Unspecified`
			}</td>
			<td>
				<Link href={'/en-admin/' + props.contentType.toLowerCase() + '/revision/' + entry.id}>
					{`View`}
				</Link>
			</td>
		</tr>
	};

	return (
		<div className="ui-revisions-list">
			{!props.hideTitle && <>
				<h2 className="ui-page__subtitle">
					{`Drafts`}
				</h2>
			</>}
			<Table
				source={api.revisionList}
				className="drafts-table"
				includes={['creatorUser']}
				filter={{
					query: 'ContentId=? and IsDraft=?',
					args: [0, true],
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
		</div>
	);
}

export default Drafts;