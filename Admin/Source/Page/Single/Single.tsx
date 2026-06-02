import AutoEdit from "Admin/Layouts/AutoEdit";
import CreatePage from "Admin/Page/Create";
import { useTokens } from "UI/Token";
import { Page } from 'Api/Page';

type PageEditorProps = {
    content?: Page,
    tabs?: { name: string; key: string }[]
};

const PageEditor: React.FC = (props: PageEditorProps) => {
    const { content, tabs } = props;
    const isEditPage = !!content;

    if (isEditPage) {

        return (
            <AutoEdit
                contentType= "Page"
                singular= "Page"
                content={content}
                tabs={tabs}
                plural= "pages"
            />
        )
    }
	
    return (
        <div className='page-editor'>
            <CreatePage />
        </div>
    )

}

export default PageEditor;