import Row from 'UI/Row';
import Modal from 'UI/Modal';
import webRequest from 'UI/Functions/WebRequest';
import Alert from 'UI/Alert';

export default class Delete extends React.Component{
	constructor(props){
		super(props);
		this.state={};
	}

    delete(comment) {
        var {onDeleted} = this.props;

        webRequest("comment/" + comment.id, {Deleted: true}).then(response => {
            // Good to go!
            // Call the onDeleted callback;
            console.log("deleted success");
            onDeleted && onDeleted();
        }).catch(error => {
            this.setState({error})
        });
    }


    render(){
        var {comment, onClose} = this.props;


        return(
            <Modal className = "comment-delete-modal" title = "Delete comment" visible = {comment} onClose = {() => {onClose && onClose()}}>
                Are you sure you want to delete your comment?
                <Row className = "comment-delete-buttons">
                    <div className = "form-group">
                        <button className = "btn btn-secondary" onClick = {() => {onClose && onClose()}}>Nevermind</button>
                        <button className = "btn btn-danger" onClick = {() => {this.delete(comment)}}>Delete it!</button>
                    </div>
                </Row>

            </Modal>
        );
    }
}
