import Loop from 'UI/Loop';
import Canvas from 'UI/Canvas';
import Column from 'UI/Column';
import Row from 'UI/Row';
import Time from 'UI/Time';
import Spacer from 'UI/Spacer';
import Reactions from 'UI/Reactions';
import UserSignpost from 'UI/User/Signpost';
import Tags from 'UI/Tags';

/**
 * Loads the contents of a forum.
 */

export default (props) =>
<Loop over='forumthread/list' filter={{where: {Id: props.id}}} {...props}>
{props.children.length ? props.children : thread => 
        <div class = "thread-view">
            <Column>
            <h1>{thread.title}</h1>
            <Tags on={thread} />
            </Column>
            <div class = "first-post">
                <Row>
                    <Column size = "9">
                        {thread.creatorUser && (
							<UserSignpost user={thread.creatorUser} />
						)}
                    </Column>
                    <Spacer height = "30"/>
                    <Column size = "3">
                        Last Edited <Time date = {thread.editedUtc} />
                    </Column>
                </Row>
                <Row>
                    <Column size = "12">
						<div className="thread-body">
							<Canvas>
								{thread.bodyJson}
							</Canvas>
						</div>
                    </Column>
                </Row>
                <Row>
					<Column>
						<Reactions on={thread} />
					</Column>
					
				</Row>
            </div>
			<Spacer height='20' />
        </div>
}
</Loop>