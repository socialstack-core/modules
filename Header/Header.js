import Dropdown from 'UI/Dropdown';

export default function Header(props) {
	
	var { title, description, start, end, showingAudience, showingConversation, disableChat, disableAudience } = props;

	var dropdownReactionsJsx = <>
		<i className="fal fa-smile"></i> <span className="dropdown-label-internal">Reactions</span>
	</>;

	var dropdownOptionsJsx = <>
		<i className="fal fa-cog"></i> <span className="dropdown-label-internal">Options</span>
	</>;

	var audienceClass = showingAudience ? "btn huddle-chat__header-audience btn--active" : "btn huddle-chat__header-audience";
	var conversationClass = showingConversation ? "btn huddle-chat__header-conversation btn--active" : "btn huddle-chat__header-conversation";

	return <header className="huddle-chat__header">
		<span className="huddle-chat__header-info">
			<h1 className="huddle-chat__header-title" data-clamp="1">
				{title}
			</h1>
			<p className="huddle-chat__header-description" data-clamp="1">
				{description}
			</p>
		</span>

		<span className="huddle-chat__header-controls">
			{/* audience */}
			{!disableAudience && <>
				<button type="button" className={audienceClass} onClick={() => props.toggleAudience()}>
					<i className="fal fa-users"></i>
				</button>
			</>}

			{/* conversation */}
			{!disableChat && <>
				<button type="button" className={conversationClass} onClick={() => props.toggleConversation()}>
					<i className="fal fa-comments"></i>
				</button>
			</>}

			{/* reactions */}
			{/* TODO */}
			{true && <>
			<Dropdown className="huddle-chat__header-reactions" label={dropdownReactionsJsx} variant="link" align="right">
				<li>
					<button type="button" className="btn dropdown-item">
							<i className="fal fa-fw fa-thumbs-up"></i> {`Like`}
					</button>
				</li>
				<li>
					<button type="button" className="btn dropdown-item">
							<i className="fal fa-fw fa-heart"></i> {`Heart`}
					</button>
				</li>
				<li>
					<button type="button" className="btn dropdown-item">
							<i className="fal fa-fw fa-sign-language"></i> {`Applause`}
					</button>
				</li>
				<li>
					<button type="button" className="btn dropdown-item">
							<i className="fal fa-fw fa-laugh-beam"></i> {`Laugh`}
					</button>
				</li>
				<li>
					<button type="button" className="btn dropdown-item">
							<i className="fal fa-fw fa-surprise"></i> {`Surprised`}
					</button>
				</li>
				<li>
					<hr class="dropdown-divider" />
				</li>
				<li>
					<button type="button" className="btn dropdown-item">
							<i className="fal fa-fw fa-hand-paper"></i> {`Raise hand`}
					</button>
				</li>
			</Dropdown>
			</>}

			{/* options */}
			{/* TODO */}
			{true && <>
			<Dropdown className="huddle-chat__header-options" label={dropdownOptionsJsx} variant="link" align="right">
				<li>
					<button type="button" className="btn dropdown-item">
							<i className="fal fa-fw"></i> {`Other jaw-dropping feature`}
					</button>
				</li>
				<li>
					<hr class="dropdown-divider" />
				</li>
				<li>
					<button type="button" className="btn dropdown-item">
							<i className="fal fa-fw fa-microphone-slash"></i> {`Disable all incoming audio`}
					</button>
				</li>
				<li>
					<button type="button" className="btn dropdown-item">
							<i className="fal fa-fw fa-video-slash"></i> {`Disable all incoming video`}
					</button>
				</li>
				{/*
			<li>
				<hr class="dropdown-divider" />
			</li>
			<li>
				<button type="button" className="btn dropdown-item" onClick={() => logout('/en-admin/', setSession, setPage)}>
					{`Logout`}
				</button>
			</li>
			*/}
			</Dropdown>
			</>}
		</span>

		{/*
device settings
call health
meeting notes
gallery
gallery at top
full screen
start recording
help

spotlight
		 */}

		{/* start / end time */}
		{/* active time / time remaining? */}
	</header>;	
}