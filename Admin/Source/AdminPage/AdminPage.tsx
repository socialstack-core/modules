import AdminPageSubHeader from 'Admin/AdminPage/SubHeader';
import AdminPageContentWrapper from 'Admin/AdminPage/ContentWrapper';
import AdminPageNotice from 'Admin/AdminPage/Notice';
import AdminPageFilters from 'Admin/AdminPage/Filters';
import AdminPageContent from 'Admin/AdminPage/Content';
import AdminPageFeedback from 'Admin/AdminPage/Feedback';

type AdminPageProps = React.PropsWithChildren<{
	children?: React.ReactNode
}>;

const AdminPageRoot: React.FC<AdminPageProps> = (props) => {
	const { children } = props;

	return <>
		<div className="admin-page__internal">
			{children}
		</div>
	</>;
}

const AdminPage: React.FC<AdminPageProps> & {
	SubHeader: typeof AdminPageSubHeader,
	ContentWrapper: typeof AdminPageContentWrapper,
	Notice: typeof AdminPageNotice,
	Filters: typeof AdminPageFilters,
	Content: typeof AdminPageContent,
	Feedback: typeof AdminPageFeedback,
} = Object.assign(AdminPageRoot, {
	SubHeader: AdminPageSubHeader,
	ContentWrapper: AdminPageContentWrapper,
	Notice: AdminPageNotice,
	Filters: AdminPageFilters,
	Content: AdminPageContent,
	Feedback: AdminPageFeedback,
});

export default AdminPage;