import User from '../User';
import CloseButton from 'UI/CloseButton';
import { useState, useEffect, useRef } from 'react';

function getCssValue(variable) {
	var style = getComputedStyle(document.body);
	var fallback = style.getPropertyValue('--fallback__' + variable);
	var actual = style.getPropertyValue('--' + variable);

	fallback = parseInt(fallback.replace(/[^0-9.]/g, ''), 10);
	actual = parseInt(actual.replace(/[^0-9.]/g, ''), 10);

	return isNaN(actual) ? fallback : actual;
}

function updateSize(audience, pageSize, total) {

	if (!audience) {
		return;
	}

	var rect = audience.getBoundingClientRect();
	var thumbnailHeight = getCssValue('huddle-thumbnail-height');
	var maxThumbnailsPerColumn = Math.floor(rect.height / thumbnailHeight);

	var html = document.querySelector("html");

	// resolves to data-audience-columns
	html.dataset['audienceColumns'] = total <= maxThumbnailsPerColumn ? 1 : 2;
}

export default function AudienceView(props) {
	const { users, pageSize } = props;
	const audienceRef = useRef(null);
	var [pageCount, setPageCount] = useState(users.length ? Math.ceil(users.length / pageSize) : 0);
	var [pageIndex, setPageIndex] = useState(1);

	useEffect(() => {
		var lastPageIndex = pageIndex;
		var newPageCount = users.length ? Math.ceil(users.length / pageSize) : 0;
		setPageCount(newPageCount);
		setPageIndex(Math.min(lastPageIndex, newPageCount));
	}, [users]);

	useEffect(() => {
		var audience = audienceRef.current;

		if (!audience) {
			return;
		}

		var audienceObserver;

		audienceObserver = new ResizeObserver(entries => {
			updateSize(audience, pageSize, users.length);
		});

		audienceObserver.observe(audience);
		updateSize(audience, pageSize, users.length);

		return () => {
			audienceObserver.unobserve(audience);
			audienceObserver.disconnect();
		};

	});

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
			<header className="huddle-chat__sidebar-header">
				<h2 className="huddle-chat__sidebar-heading">
					{`Audience (${users.length})`}
				</h2>
				<CloseButton isSmall callback={props.toggleAudience} />
			</header>
			<div className="huddle-chat__sidebar-body">
				<ul className="huddle-chat__sidebar-body-internal huddle-chat__sidebar-audience-members" ref={audienceRef}>
					{pageUsers.map(user => <li className="huddle-chat__sidebar-audience-member">
						<User user={user} isThumbnail node={"div"} />
					</li>)}
				</ul>
			</div>

			{pageCount > 1 && <>
				<footer className="huddle-chat__sidebar-footer">
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
