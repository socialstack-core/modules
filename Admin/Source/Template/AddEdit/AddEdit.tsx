// ========================
// API Imports
// ========================
import TemplateApi, { Template } from "Api/Template";

// ========================
// Admin Imports
// ========================
import AddEditTemplateInfo from "Admin/Template/AddEdit/TemplateInfo";
import AdminPage from "Admin/AdminPage";
import Footer from "Admin/Footer";

// ========================
// UI Imports
// ========================
import { TabsWrapper, TabsPanelsWrapper, TabsPanelWrapper } from "UI/Tabs";
import Form from "UI/Form";
import Button from "UI/Button";
import AddEditTemplateConfig from "Admin/Template/AddEdit/TemplateConfig";
import AddEditTemplateCanvasEditor from "Admin/Template/AddEdit/CanvasEditor";

// ========================
// Hook Imports
// ========================
import { useRouter } from "UI/Router";

// ========================
// Types
// ========================
type AddEditPageProps = {
	content?: Template;
};

export type CanvasNode = {
	// UI/Input, UI/Button etc..
	t: string,
	// props. passed to the component
	d: Record<string, any>,
	// any non-roots children
	c: CanvasNode[] | CanvasNode,
	// roots children.
	r: Record<string, CanvasNode>
}
enum TemplateTab {
	Configuration = `Configuration`,
	Design = `Design`,
}

/**
 * AddEditPage Component
 *
 * Provides a form interface for creating or editing a `Template`.
 *
 * Features:
 * - Determines if the form should create a new template or update an existing one.
 * - On successful create, navigates to the new template's page.
 * - Displays form sections using tabbed navigation.
 */
const AddEditPage: React.FC<AddEditPageProps> = ({ content }) => {

	// if the content object is empty/undefined
	// create and assign it, all exists checks happen on the 
	// ID field, so as long as that isn't assigned
	// this will continue to work as expected.
	if (!content) {
		content = {} as Template;
	}

	const isNewTemplate = !content.id;

	const tabCanvases = [{
		name: `Configuration`,
		key: 'configuration'
	}];

	if (!isNewTemplate) {
		tabCanvases.push({
			name: `Design`,
			key: 'design'
		});
	}

	const renderTabs = () => {
		return <TabsWrapper fullWidth>

			{/* tab panels */}
			<TabsPanelsWrapper>
				<TabsPanelWrapper id="tab-panel1">
					<AddEditTemplateInfo
						existing={content} />
					<AddEditTemplateConfig
						existing={content}
					/>
				</TabsPanelWrapper>
				<TabsPanelWrapper id="tab-panel2">
					<AddEditTemplateCanvasEditor
						content={content} />
				</TabsPanelWrapper>
			</TabsPanelsWrapper>
		</TabsWrapper>;
	}

	// ========================
	// Hooks
	// ========================
	const { setPage, updateQuery, pageState } = useRouter();

	// ========================
	// Load BodyJSON and add fallback guards
	// ========================

	// tab handling
	const currentTab = (pageState.query?.get("currentTab") || TemplateTab.Configuration).toLowerCase();

	const setCurrentTab = (target: string) => {
		updateQuery({ currentTab: target });
	};

	// ========================
	// Render
	// ========================
	return <>
		<Form
			action={(
				content?.id ?
					// if the content is existing, perform an update
					(payload: Template) => TemplateApi.update(content.id, payload) :
					// otherwise, create it.
					TemplateApi.create
			)}
			onSuccess={(response) => {
				const isNewlyCreated = content?.id != response.id;

				if (isNewlyCreated) {
					setPage('/en-admin/template/' + response.id + '?newlyCreated=true');
				} else {
					updateQuery({
						saved: "true"
					})
				}
			}}
		>
			<AdminPage.SubHeader title={`Add / Edit Template`} breadcrumbs={[
				{
					href: '/en-admin/template',
					title: `Templates`
				},
				{
					title: `Add / Edit Template`
				}
			]} tabCanvases={tabCanvases} currentTab={currentTab} setCurrentTab={setCurrentTab} />
			<AdminPage.ContentWrapper>
				<AdminPage.Content>
					{renderTabs()}
				</AdminPage.Content>
			</AdminPage.ContentWrapper>

			{pageState.query.has("saved") && <>
				<AdminPage.Feedback variant="success">
					{`Successfully saved changes`}
				</AdminPage.Feedback>
			</>}

			{pageState.query.has("newlyCreated") && <>
				<AdminPage.Feedback variant="success">
					{`Successfully created new template #${content?.id}`}
				</AdminPage.Feedback>
			</>}

			<Footer>
				<Button type="submit">
					{`Save template`}
				</Button>
			</Footer>
		</Form>
	</>;

}

export default AddEditPage;