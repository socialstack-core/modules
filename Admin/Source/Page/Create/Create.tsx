import { getTemplates, TemplateModule } from "Admin/Functions/GetPropTypes";
import PageApi, { Page } from "Api/Page";
import TemplateSelector, { AdminTemplate } from "Admin/Template/Selector";
import { useEffect, useState } from "react";
import Alert from "UI/Alert";
import Button from "UI/Button";
import Column from "UI/Column";
import Container from "UI/Container";
import Form from "UI/Form";
import Input from "UI/Input";
import Row from "UI/Row";


const CreatePage: React.FC = (): React.ReactElement => {

	const [error, setError] = useState<string>();
	const [pageGroup, setPageGroup] = useState<uint>(0 as uint);
	const [selectedTemplate, setSelectedTemplate] = useState<AdminTemplate | undefined>();

	return (
		<>
			<Container>
				<Row className='page-create'>
					<Column size={"6"}>

						<h3>{`Create new page`}</h3>

						<Form
							action={(vals: Page) => PageApi.create(vals)}
							onSuccess={(res: Page) => {
								// redirect to the page created.
								window.location.href = `/en-admin/page/${res.id}`
							}}
							onValues={(values: Page): Page => {

								// set the template to the values if there is one.
								let pageBody : any = {};

								if (selectedTemplate) {
									if (selectedTemplate.template) {
										// It's a db template and requires Admin/Template to be loaded.
										pageBody = {
											t: "Admin/Template",
											d: {
												// *not* selectedTemplate.key
												templateKey: selectedTemplate.template.key
											}
										};
									} else if (selectedTemplate.componentName) {
										// It's a code one and is used in the page canvas like any other component:
										pageBody = {
											t: selectedTemplate.componentName
										};
									}
								}

								values.bodyJson = JSON.stringify(pageBody);
								
								return values;
							}}
							onFailed={(error) => {
								setError(error.message)
							}}
						>
							{error && <Alert variant="danger">{error}</Alert>}
							<Input
								type={'text'}
								name={'title'}
								label={`Page Title`}
							/>
							<Input
								type={'select'}
								contentType={'PageGroup'}
								name='pageGroupId'
								label={`Page Group`}
								noSelection={`None - enter a URL instead`}
								noSelectionValue={'0'}
								onChange={e => setPageGroup(parseInt((e.target as HTMLSelectElement).value) as uint)}
								clearable
							/>
							{pageGroup === 0 && <Input
								type={'text'}
								name={'url'}
								label={`Page URL`}
							/>}
							<Input
								type={'textarea'}
								name={'description'}
								label={`Page Description`}
							/>
							<TemplateSelector name='pageTemplate' templateType={1 as int} label={`Page Template`} onChange={setSelectedTemplate} />
							<Button type='submit'>{`Create page`}</Button>
						</Form>
					</Column>
				</Row>
			</Container>
		</>
	)
}



export default CreatePage;