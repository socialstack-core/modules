// ========================
// API Imports
// ========================
import TemplateApi, { Template } from "Api/Template";

// ========================
// Admin Imports
// ========================
import Tabs from "Admin/Tabs";
import AddEditTemplateInfo from "Admin/Template/AddEdit/TemplateInfo";

// ========================
// UI Imports
// ========================
import Form from "UI/Form";
import Button from "UI/Button";
import Alert from "UI/Alert";
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
	
    // ========================
    // Hooks
    // ========================
    const { setPage, updateQuery, pageState } = useRouter();
	
	// ========================
	// Load BodyJSON and add fallback guards
	// ========================
	
	// defined as let as can be overriden by changing the parent. 
	let [bodyJson, setBodyJson] = useState<CanvasNode>(JSON.parse(content?.bodyJson ?? '{}') ?? {});
	
    // ========================
    // Render
    // ========================
    return (
        <div className={'add-edit-template-page'}>
            <Form 
                action={
                    (
                        content?.id ?
                            // if the content is existing, perform an update
                            (payload: Template) => TemplateApi.update(content.id, payload) :
                            // otherwise, create it.
                            TemplateApi.create
                    )
                }
                onSuccess={(response) => {
                    const isNewlyCreated = content?.id != response.id;
                    
                    if (isNewlyCreated) {
                        setPage('/en-admin/template/' + response.id + '?newlyCreated=true');
                    } 
                    else {
                        updateQuery({
                            saved: "true"
                        })
                    }
                }}
            >
                {pageState.query.has("saved") && <Alert variant={'success'}>{`Successfully saved changes`}</Alert>}
                {pageState.query.has("newlyCreated") && <Alert variant={'success'}>{`Successfully created new template #${content?.id}`}</Alert> }
                <Tabs 
                    tabs={[
                        {
                            label: `Configuration`,
                            content: (
								<div>
									<AddEditTemplateInfo 
										existing={content}/>
									<AddEditTemplateConfig 
										existing={content} 
										onParentChange={(parent: Template | string) => {
											if (typeof parent === 'string') {
												// it's a string here, basic file based template
												
												// let's change the root component
												bodyJson.t = parent;
												
												// reset the props.
												bodyJson.d = {};
												
												// ignore the children though, 
												// a user will probably expect the children to be the same
												// and would probably find it rather annoying if the children are wiped.
											} 
											else {
												// we gets a parent template.
												
												// assign it "Admin/Template" as its referencing
												// another DB template.
												bodyJson.t = "Admin/Template";
												
												// clear the props, and assign template key
												bodyJson.d = {
													templateKey: parent.key,
												}

												// ignore the children though, 
												// a user will probably expect the children to be the same
												// and would probably find it rather annoying if the children are wiped.
											}

											// clear the roots, 
											bodyJson.r = {};
											
											// update state, this allows the canvas editor to be
											// reloaded "live" with the new template.
											setBodyJson({...bodyJson});
										}}
									/>
								</div>
							)
                        },
                        {
                            label: `Design`,
							content: <AddEditTemplateCanvasEditor 
								onCanvasChange={(source: string) => {
									setBodyJson(JSON.parse(source));
									
									content.bodyJson = source;
								}} 
								content={{...content, bodyJson: JSON.stringify(bodyJson)}}/>
                        }
                    ]}
					currentTab={pageState.query.get("currentTab")}
					onTabChange={(newTab: string) => {
						updateQuery({
							currentTab: newTab,
						})
					}}
                />
                <footer className="admin-page__footer">
					<Container style={{ justifyItems: "right" }}>
                    	<Button type={'submit'}>{`Save template`}</Button>
					</Container>
                </footer>
            </Form>
        </div>
    )

}

export default AddEditPage;