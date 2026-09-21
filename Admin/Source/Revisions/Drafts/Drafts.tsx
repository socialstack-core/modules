import Table from 'UI/Table';
import Time from 'UI/Time';
import Link from 'UI/Link';
import Button from 'UI/Button';
import ConfirmDialog from 'UI/Dialog/ConfirmDialog';
import { useState } from 'react';

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

	const [confirmRemove, setConfirmRemove] = useState<any>(null);

	const renderEmpty = () => {
		return <>
			<tr>
				<td colSpan={3}>
					<span className="ui-not-found">
						{`None found`}
					</span>
				</td>
			</tr>
		</>;
	}

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

	const renderDraftEntry = (entry: any) => {
		return <>
			<tr className="drafts-table__row">
				<td className="drafts-table__col">
					<Time date={entry.createdUtc} />
				</td>
				<td className="drafts-table__col">
					{entry.creatorUser ? entry.creatorUser.fullName || entry.creatorUser.username : `Unspecified`}
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

	return (
		<div className="ui-revisions-list">
			{!props.hideTitle && <>
				<h2 className="ui-page__subtitle">
					{`Drafts`}
				</h2>
			</>}
			<Table
				source={(filter?: ListFilter, includes?: ApiInclude[]) => filter ? api.revisionList(filter, includes) : api.revisionListAll(includes)}
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

export default Drafts;