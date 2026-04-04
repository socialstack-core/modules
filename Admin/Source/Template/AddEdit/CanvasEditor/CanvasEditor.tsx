import {Template} from "Api/Template";
import Alert from "UI/Alert";
import Input from "UI/Input";

// ========================
// Types
// ========================

type AddEditTemplateCanvasEditorProps = {
	content?: Template;
};

const AddEditTemplateCanvasEditor: React.FC<AddEditTemplateCanvasEditorProps> = (props) => {
	
	const { content } = props;
	
	if (!content) {
		return (<Alert variant="danger">{`No template supplied`}</Alert>)
	}
	
	return (
		<div className={'canvas-editor-container'}>
			<Input
				type={'canvas'}
				name={'bodyJson'}
				defaultValue={content.bodyJson}
			/>
		</div>
	)
}

export default AddEditTemplateCanvasEditor;