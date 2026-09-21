import { useState } from 'react';
import Time from 'UI/Time';
import Link from 'UI/Link';
import Loading from 'UI/Loading';
import Badge from 'UI/Badge';
import Dialog from 'UI/Dialog';
import Button from 'UI/Button';
import { Permalink } from 'Api/Pages';
import PageApi, { Page } from 'Api/Page';
import useApi from 'UI/Functions/UseApi';
import permalinkApi from 'Api/Permalink';

interface ListProps {
	content: any;
	contentType: string;
}

const List: React.FC<ListProps> = (props: ListProps) => {

	const [links] = useApi(() => permalinkApi.getPermalinks({
		contentType: props.contentType,
		contentId: props.content?.id
	}, [permalinkApi.includes.creatorUser]), [props.content?.id, props.contentType]);

	var results = links?.results;

	var primaryEntry = results?.find((entry: Permalink) => entry.target?.startsWith('primary:'));
	var primaryTarget = primaryEntry?.target || null;

	var truncatedTarget: string | null = null;
	if (primaryTarget) {
		var firstColon = primaryTarget.indexOf(':');
		if (firstColon !== -1) {
			var secondColon = primaryTarget.indexOf(':', firstColon + 1);
			truncatedTarget = secondColon !== -1 ? primaryTarget.substring(0, secondColon) : primaryTarget;
		} else {
			truncatedTarget = primaryTarget;
		}
	}

	const [pageResponse] = useApi(() => {
		if (!primaryTarget) {
			return Promise.resolve(null);
		}
		return PageApi.list({
			query: "Key=[?]",
			args: [[primaryTarget, truncatedTarget]]
		});
	}, [primaryTarget, truncatedTarget]);

	const [showTemplateDialog, setShowTemplateDialog] = useState(false);
	const [cloning, setCloning] = useState(false);

	if (!results || results.length === 0) {
		return (
			<div className="permalink-list">
				<span className="ui-not-found">None found</span>
			</div>
		);
	}

	var matchedPage: Page | null = null;
	var isContentSpecific = false;

	if (pageResponse?.results) {
		matchedPage = pageResponse.results.find((p: Page) => p.key === primaryTarget) || null;
		if (matchedPage) {
			isContentSpecific = true;
		} else {
			matchedPage = pageResponse.results.find((p: Page) => p.key === truncatedTarget) || null;
		}
	}

	function handleEditClick() {
		if (!matchedPage) {
			return;
		}

		if (isContentSpecific) {
			window.location.href = `/en-admin/page/${matchedPage.id}?context=` + (primaryEntry.url || '');
		} else {
			setShowTemplateDialog(true);
		}
	}

	function handleClonePage() {
		if (!matchedPage || !primaryTarget) {
			return;
		}

		setCloning(true);

		var clonedPage: any = { ...matchedPage };
		delete clonedPage.id;
		clonedPage.key = primaryTarget;

		PageApi.create(clonedPage)
			.then((newPage: Page) => {
				window.location.href = `/en-admin/page/${newPage.id}?context=` + (primaryEntry.url || '');
			});
	}
	
	return (
		<div className="permalink-list">
			<table className="table ui-table table-hover permalink-table">
				<thead>
					<tr>
						<th>URL</th>
						<th>Target</th>
						<th>Date created</th>
					</tr>
				</thead>
				<tbody>
					{results.map((entry: Permalink) => (
						<tr className="permalink-table__row" key={entry.id}>
							<td className="permalink-table__col">
								<Link href={entry.url || undefined}>
									{entry.url}
								</Link>
							</td>
							<td className="permalink-table__col permalink-table__col--target">
								<code>{entry.target}</code>
							</td>
							<td className="permalink-table__col">
								<Time date={entry.createdUtc} />
							</td>
						</tr>
					))}
				</tbody>
			</table>
			{primaryTarget && (
				<div className="permalink-list__page">
					{!pageResponse ? (
						<Loading small message="Looking up page..." />
					) : matchedPage ? (
						<div className="permalink-list__page-info">
							<div className="permalink-list__page-detail">
								<Badge xs variant={isContentSpecific ? "primary" : "info"}>
									{isContentSpecific ? "Dedicated page" : "Template page"}
								</Badge>
								<span className="permalink-list__page-title">{matchedPage.title}</span>
							</div>
							<Link xs outlined variant="primary" href="#" onClick={(e: any) => {
								e.preventDefault();
								handleEditClick();
							}}>
								{`Edit page`}
							</Link>
						</div>
					) : null}
				</div>
			)}
			{showTemplateDialog && matchedPage && (
				<Dialog
					isOpen={showTemplateDialog}
					onClose={() => {
						setShowTemplateDialog(false);
						setCloning(false);
					}}
					title={`Shared template page`}
				>
					<p>
						{`The page `}
						<strong>{matchedPage.title}</strong>
						{` is a shared template used by multiple content items. Editing it directly will affect all content that uses this template.`}
					</p>
					<p>{`Would you like to create a dedicated page for this content instead?`}</p>
					<Dialog.Footer>
						<Button outlined onClick={() => {
							if (matchedPage) {
								window.location.href = `/en-admin/page/${matchedPage.id}?context=` + (primaryEntry.url || '');
							}
						}}>
							{`Continue to edit template`}
						</Button>
						<Button disabled={cloning} onClick={handleClonePage}>
							{cloning ? `Cloning...` : `Clone and edit`}
						</Button>
					</Dialog.Footer>
				</Dialog>
			)}
		</div>
	);
}

export default List;
