import {Template} from "Api/Template";
import Alert from "UI/Alert";
import Input from "UI/Input";

// ========================
// Types
// ========================

type AddEditTemplateCanvasEditorProps = {
	content?: Template;
	onCanvasChange?: (source: string) => void;
};

const AddEditTemplateCanvasEditor: React.FC<AddEditTemplateCanvasEditorProps> = (props) => {
	
	const { content, onCanvasChange } = props;
	
	if (!content) {
		return (<Alert variant="danger">{`No template supplied`}</Alert>)
	}
	
	const body = JSON.parse(content.bodyJson)
	
	
	return (
		<div className={'canvas-editor-container'}>
			<Input
				key={body.templateKey ?? body.t}
				type={'canvas'}
				value={content.bodyJson}
				name={'bodyJson'}
				onCanvasChange={onCanvasChange}
			/>
		</div>
	)
}

export default AddEditTemplateCanvasEditor;