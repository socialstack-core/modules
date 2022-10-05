import User from '../User';

export default function PinnedView(props) {
	var { users, huddleClient, showDebugInfo } = props;

	if (!users || !users.length) {
		return;
	}

	return <ul className="huddle-chat__pinned" data-users={users ? users.length : undefined}>
		{users.map(user => {
			return <User className="huddle-chat__pinned-user" key={user.id} user={user} isThumbnail isPinned huddleClient={huddleClient} showDebugInfo={showDebugInfo} />;
		})}
	</ul>;
}