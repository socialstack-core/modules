import Input from "UI/Input";
import Form from "UI/Form";
import Alert from "UI/Alert";
import Loop from "UI/Loop";
import Add from "UI/Comments/Add";
import getContentTypeId from "UI/Functions/GetContentTypeId";

export default class List extends React.Component{
	constructor(props){
		super(props);
		this.state={};
	}
	
    render(){

        let {
			contentId,
			contentType,
			on
		} = this.props;
        
        if(!contentType && on){
			contentId = on.id;
			contentType = on.type;
		}
		
		if(!contentType){
            // Missing required props.
            console.log("contentType not set");
			return null;
        }
		
		var contentTypeId = getContentTypeId(contentType);
		
        return(
            <div className = "comment-add">
				<Loop over='comment/list' filter={
					{
						where:{
							ContentId: on.id,
							ContentTypeId: contentTypeId,
							RootParentCommentId: 0
						},
						sort: {
							field: 'Order'
						}
					}
				}>
					{comment => {
						
						return <li style={{marginLeft: (comment.depth * 100) + 'px'}}>
							{comment.bodyJson}
							<Add contentId={contentId} contentTypeId={contentTypeId} parentCommentId={comment.id} />
						</li>;
						
					}}
				</Loop>
				<Add contentId={contentId} contentTypeId={contentTypeId} />
        </div>);
    }
}

List.propTypes={
	contentId: 'string',
	contentType: 'string'
};
List.icon='comment';
