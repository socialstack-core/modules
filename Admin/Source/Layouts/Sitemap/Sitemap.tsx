import TreeView, { buildBreadcrumbs } from 'Admin/TreeView';
import { useRouter } from 'UI/Router';
import pageApi from 'Api/Page';
import AdminPage from 'Admin/AdminPage';
import Footer from 'Admin/Footer';
import Link from 'UI/Link';
//import Button from 'UI/Button';
//import { useState } from 'react';
//import ConfirmDialog from 'UI/Dialog/ConfirmDialog';

type SitemapProps = {
	noCreate?: boolean
};

export default function Sitemap(props: SitemapProps) {
	//const [ showCloneModal, setShowCloneModal] = useState(false);
	//const [ showConfirmDialog, setShowConfirmDialog ] = useState(false);
	const { pageState } = useRouter();
	const { query } = pageState;
	var path = query?.get("path") || "";

	var breadcrumbs = buildBreadcrumbs(
		'/en-admin/page',
		`Pages`,
		path,
		'/en-admin/page'
	);
	
	// function removePage(page : Page) {
	// 	return pageApi.delete(page.id).then(response => {
	// 		window.location.reload();
	// 	});
	// }

	var addUrl = window.location.pathname.replace(/\/+$/g, '') + '/add';

	return <>
		<AdminPage.SubHeader
			title={`Edit Site Pages`}
			breadcrumbs={breadcrumbs} />
		<AdminPage.ContentWrapper>
			<AdminPage.Content>
				{/*showCloneModal && <>
						<Modal visible onClose={() => setShowCloneModal(false)} title={`Save Page As`}>
							<p>
								<strong>{`Cloning from:`}</strong> <br />
								{getPageDescription(showCloneModal)}
							</p>
							<hr />
							<Form 
								onSuccess={(response) => {
									let clonedPage = structuredClone(showCloneModal);
									clonedPage.url = response.url;
									clonedPage.title = response.title;
									clonedPage.description = response.description;

									pageApi.create(clonedPage).then(response => {
										setShowCloneModal(false);
										reloadPages();
									});

								}}>

								<Input label={`Url`} id="sitemap__clone-url" type="text" name="url" required />
								<Input label={`Title`} id="sitemap__clone-title" type="text" name="title" />
								<Input label={`Description`} id="sitemap__clone-description" type="text" name="description" />

								<div className="sitemap__clone-modal-footer">
									<Button outlined variant="danger" onClick={() => setShowCloneModal(false)}>
										{`Cancel`}
									</Button>
									<input type="submit" className="btn btn-primary" value={`Save Copy`} />
								</div>
							</Form>
						</Modal>
					</>}

					{showConfirmDialog && <>
						<ConfirmDialog variant="danger" isOpen={showConfirmDialog} onClose={() => setShowConfirmDialog(false)}
							confirmCallback={() => {
								return removePage(showConfirmDialog);
							}}>
							<p>
								<strong>{`This will remove the following page:`}</strong> <br />
								{getPageDescription(showConfirmDialog)}
							</p>
							<p>
								{`Are you sure you wish to do this?`}
							</p>
						</ConfirmDialog>
					</>}

					*/}
				<TreeView onLoadData={(path) => {
					return pageApi
						.getRouterTreeNodePath(path)
						.then(resp => {
							return resp;
						});
				}} />
			</AdminPage.Content>
		</AdminPage.ContentWrapper>
		<Footer>
			{!props.noCreate && <>
				<Link href={addUrl} variant="primary">
					{`Create new`}
				</Link>
			</>}
		</Footer>
	</>;
}
