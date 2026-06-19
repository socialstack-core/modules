import {Page} from 'Api/Page';
import Input from 'UI/Input';

type UrlEditorProps = {
	currentContent?: Page;
	defaultValue?: uint;
	name?: string;
};

export default function UrlEditor(props: UrlEditorProps) {
	if(props.currentContent?.pageGroupId){
		return null;
	}
	
	return <Input {...props} type='text' />;
}
