import User from '../User';
import { useState, useEffect } from 'react';

export default function AudienceView(props) {
	const { users, pageSize, isDemo } = props;
	var [pageCount, setPageCount] = useState(users.length ? Math.ceil(users.length / pageSize) : 0);
	var [pageIndex, setPageIndex] = useState(1);

	useEffect(() => {
		var lastPageIndex = pageIndex;
		var newPageCount = users.length ? Math.ceil(users.length / pageSize) : 0;
		setPageCount(newPageCount);
		setPageIndex(Math.min(lastPageIndex, newPageCount));
	}, [users]);

	function prevAttendees() {
		setPageIndex(pageIndex - 1);
    }

	function nextAttendees() {
		setPageIndex(pageIndex + 1);
	}

	if (!pageCount) {
		return;
	}

	var pageUsers = users.slice((pageIndex - 1) * pageSize, pageIndex * pageSize);

	return (
		<aside className="huddle-chat__sidebar huddle-chat__sidebar--scrollable huddle-chat__audience">
			<header class="huddle-chat__sidebar-header">
				<h2 class="huddle-chat__sidebar-heading">
					Audience ({users.length})
				</h2>
			</header>
			<div className="huddle-chat__sidebar-body">
				<ul className="huddle-chat__sidebar-body-internal huddle-chat__sidebar-audience-members">
					{pageUsers.map(user => <li className="huddle-chat__sidebar-audience-member">
						<User user={user} isThumbnail node={"div"} isDemo />
					</li>)}
				</ul>
			</div>

			{pageCount > 1 && <>
				<footer class="huddle-chat__sidebar-footer">
					<button type="button" className="btn btn-outline-primary huddle-chat__audience-prev" title="Previous" aria-label="Previous page of attendees"
						disabled={pageIndex == 1 ? "disabled" : undefined} onClick={() => prevAttendees()}>
						<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 474.133 474.133">
							<path d="M166.038 237.067l185.234 185.947-21.589 21.486-206.822-207.433L329.683 29.633l21.589 21.589z" fill="currentColor" />
						</svg>
					</button>
					<span className="huddle-chat__audience-page">
						{pageIndex} of {pageCount}
					</span>
					<button type="button" className="btn btn-outline-primary huddle-chat__audience-next" title="Next" aria-label="Next page of attendees"
						disabled={pageIndex == pageCount ? "disabled" : undefined} onClick={() => nextAttendees()}>
						<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 474.133 474.133">
							<path d="M122.861 51.222l185.234 185.845L122.86 423.014 144.45 444.5l206.822-207.433L144.45 29.633z" fill="currentColor" />
						</svg>
					</button>
				</footer>
			</>}
		</aside>
	);
}
