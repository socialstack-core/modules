import Loop from 'UI/Loop';
import Canvas from 'UI/Canvas';
import Row from 'UI/Row';
import Column from 'UI/Column';
import Spacer from 'UI/Spacer';
import Time from 'UI/Time';
import Reactions from 'UI/Reactions';
import UserSignpost from 'UI/User/Signpost';

/**
 * A list of forum replies
 */
export default class List extends React.Component {

	constructor(props){
		super(props);
		this.state={};
	}

	render() {

		var {threadId, children} = this.props;

		return (
			<Loop over='forumreply/list' filter={threadId ? {where: {ThreadId: threadId}} : undefined} {...this.props}>
				{children.length ? children : reply => 
					<div className= "reply">
						<Row>
							<Column size = "9">
								{reply.creatorUser && (
									<UserSignpost user={reply.creatorUser} />
								)}
							</Column>
							<Spacer height = "30"/>
							<Column size = "3">
								Last Edited <Time date = {reply.editedUtc} />
							</Column>
						</Row>
						<hr/>
						<Row>
							<Column size = "12">
								<div className="reply-body">
									<Canvas>
										{reply.bodyJson}
									</Canvas>
								</div>
							</Column>
						</Row>
						<Row>
							<Column>
								<Reactions on={reply} />
							</Column>
							
						</Row>
						<Spacer height='20' />
					</div>
				}
			</Loop>
		);
	}
}

List.propTypes = {
    threadId: 'int'
};
List.defaultProps = {};

