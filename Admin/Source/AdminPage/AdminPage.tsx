import AdminPageSubHeader from 'Admin/AdminPage/SubHeader';
import AdminPageContentWrapper from 'Admin/AdminPage/ContentWrapper';
import AdminPageNotice from 'Admin/AdminPage/Notice';
import AdminPageFilters from 'Admin/AdminPage/Filters';
import AdminPageContent from 'Admin/AdminPage/Content';
import AdminPageFeedback from 'Admin/AdminPage/Feedback';

type AdminPageProps = React.PropsWithChildren<{
	children?: React.ReactNode
}>;

function AdminPageRoot(props: AdminPageProps) {
	const { children } = props;

	return <>
		<div className="admin-page__internal">
			{children}
		</div>
	</>;
}

AdminPageRoot.SubHeader = AdminPageSubHeader;
AdminPageRoot.ContentWrapper = AdminPageContentWrapper;
AdminPageRoot.Notice = AdminPageNotice;
AdminPageRoot.Filters = AdminPageFilters;
AdminPageRoot.Content = AdminPageContent;
AdminPageRoot.Feedback = AdminPageFeedback;

const AdminPage = AdminPageRoot;
export default AdminPage;