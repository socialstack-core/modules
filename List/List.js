import Input from "UI/Input";
import Form from "UI/Form";
import Alert from "UI/Alert";
import Loop from "UI/Loop";
import Add from "UI/Comments/Add";
import getContentTypeId from "UI/Functions/GetContentTypeId";
import Row from 'UI/Row';
import sinceDate from 'UI/Functions/SinceDate';
import Modal from 'UI/Modal';
import Report from 'UI/Comments/Report';

export default class List extends React.Component{
	constructor(props){
		super(props);
		this.state={
			comments: []
		};
	}
	
	replyToggle(comment) {
		console.log("reply clicked");

		// Let's find out which comments are toggled.
		var comments = this.state.comments;

		console.log(comments);
		comments[comment.id] = !!!comments[comment.id];
		console.log(comments)
		this.setState({comments});
	}
	
	delete() {

	}

    render(){

        let {
			contentId,
			contentType,
			on
		} = this.props;

		var {
			comments,
			deleteComment,
			reportComment
		} = this.state;
        
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
            <div className = "comments">
				<Modal className = "comment-delete-modal" title = "Delete comment" visible = {deleteComment} onClose = {() => {this.setState({deleteComment: null})}}>
					Are you sure you want to delete your comment?
					<Row className = "comment-delete-buttons">
						<div className = "form-group">
							<button className = "btn btn-secondary" onClick = {() => {this.setState({deleteComment: null})}}>Nevermind</button>
							<button className = "btn btn-danger" onClick = {() => {this.delete()}}>Delete it!</button>
						</div>

					</Row>
				</Modal>
				<Modal classname = "comment-report-modal" title = "Report comment" visible = {reportComment} onClose = {() => {this.setState({reportComment: null})}}>
					Thanks for submitting a report. Please provide a short description of what is happening and someone will investigate.

				</Modal>
				<Add contentId={contentId} contentTypeId={contentTypeId} />
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

						var isUser = (global.app.state.user && comment.creatorUser.id == global.app.state.user.id);

						return <li style={{marginLeft: (comment.depth * 100) + 'px'}}>
							<Row className = "user-info"><b className = "user-name">{comment.creatorUser.username}</b> {sinceDate(comment.createdUtc)}</Row>
							{comment.bodyJson}
							<Row className = "comment-actions">
								<button onClick = {() => {this.replyToggle(comment)}} className = "btn"><i class="far fa-comment-alt-lines"></i> Reply</button> 
								{isUser && <button className = "btn"><i class="fas fa-pencil"></i> Edit</button>}
								{!isUser && <button className = "btn"><i class="far fa-flag"></i> Report</button>}
								{isUser && <button onClick = {() => {this.setState({deleteComment: comment})}} className = "btn"><i class="far fa-trash-alt"></i> Delete</button>}
							</Row>
							<Add onClose = {() => {this.replyToggle(comment)}} visible = {comments[comment.id]} contentId={contentId} contentTypeId={contentTypeId} parentCommentId={comment.id} />
						</li>;
						
					}}
				</Loop>
        </div>);
    }
}

List.propTypes={
	contentId: 'string',
	contentType: 'string'
};
List.icon='comment';
