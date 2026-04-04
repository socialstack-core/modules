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
import Tabs from "UI/Tabs";
import Form from "UI/Form";
import Button from "UI/Button";
import AddEditTemplateConfig from "Admin/Template/AddEdit/TemplateConfig";
import AddEditTemplateCanvasEditor from "Admin/Template/AddEdit/CanvasEditor";

// ========================
// Hook Imports
// ========================
import { useRouter } from "UI/Router";
import {useEffect, useState} from "react";
import Container from "UI/Container";

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
	const templateTabs = isNewTemplate 
		? [TemplateTab.Configuration]
		: Object.values(TemplateTab);
	
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

	const renderTabPanel = (tab: TemplateTab) => {
		switch (tab) {
			case TemplateTab.Configuration:
				return <>
					<AddEditTemplateInfo
						existing={content} />
					<AddEditTemplateConfig
						existing={content}
					/>
				</>;

			case TemplateTab.Design:
				return <>
					<AddEditTemplateCanvasEditor
						content={content} />
				</>;
		}
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
			]} />
			<AdminPage.ContentWrapper>
				<AdminPage.Content>
					<Tabs currentTab={currentTab} tabs={templateTabs}
						renderPanel={renderTabPanel} onChange={(tab: string) => setCurrentTab(tab.toLowerCase())} />
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