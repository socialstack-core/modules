import Loop from 'UI/Loop';
import Column from 'UI/Column';
import Row from 'UI/Row';
import url from 'UI/Functions/Url';
import Time from 'UI/Time';
import Tags from 'UI/Tags';
import Spacer from 'UI/Spacer';

/**
 * A list of forum threads
 */

export default class List extends React.Component {

    // If you want to use state in your react component, uncomment this constructor:
    /* 
    constructor(props){
        super(props);
        this.state = {};
    }
    */

	render() {
		
		var {forumId, children, showHeader} = this.props;

		return (
			<div className='thread-list'>
				<div className="thread-list-header">
					<a className="btn btn-dark" href={'/forum/create/' + forumId}>Create a thread</a>
				</div>
				
				<div>
					{showHeader ? 
						<div>
							<Spacer height='20'/>
							<Row className="forum-list-header">
								<Column size="6"><h5>Topic</h5></Column>
								<Column size="3"><h5>Author</h5></Column>	
								<Column size="3"><h5>Last Edited</h5></Column>	
							</Row>
						</div> : undefined

					}
					
					<Loop over='forumthread/list' filter={forumId ? {where: {ForumId: forumId}} : undefined} {...this.props}>
					{children && children.length ? children : thread => 
						<a href={url(thread)}>
							<div>
							<Row>
								<Column size="6">
									<h2>{thread.title}</h2>
									<Tags on={thread} />
								</Column>
								<Column size="3">
									{thread.creatorUser ? thread.creatorUser.username : ""}
								</Column>
								<Column size="3">
									<Time date = {thread.editedUtc} />
								</Column>
							</Row>
							</div>
							<hr></hr>
						</a>
					}
					</Loop>
				</div>
			</div>
		);
	}
}

List.propTypes = {
	showHeader: 'boolean',
	forumId: 'int'
};
List.defaultProps = {
	showHeader: true
};
List.icon = 'align-center';
