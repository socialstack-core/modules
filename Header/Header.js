import Dropdown from 'UI/Dropdown';

export default function Header(props) {
	
	var {
		title, description,
		start, end,
		showingAudience, showingConversation,
		disableChat, disableAudience, disableReactions, disableOptions,
		bandwidth, noiseCancellation } = props;

	if (!bandwidth) {
		bandwidth = 3;
    }

	var dropdownReactionsJsx = <>
		<i className="fal fa-smile"></i> <span className="dropdown-label-internal">
			{`Reactions`}
		</span>
	</>;

	var dropdownOptionsJsx = <>
		<i className="fal fa-cog"></i> <span className="dropdown-label-internal">
			{`Options`}
		</span>
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
				<button type="button" className={audienceClass} onClick={() => props.toggleAudience()}
					title={showingAudience ? `Hide audience` : `Show audience` }>
					<i className="fal fa-users"></i>
				</button>
			</>}

			{/* conversation */}
			{!disableChat && <>
				<button type="button" className={conversationClass} onClick={() => props.toggleConversation()}
					title={showingConversation ? `Hide conversation` : `Show conversation`}>
					<i className="fal fa-comments"></i>
				</button>
			</>}

			{/* reactions */}
			{/* TODO */}
			{!disableReactions && <>
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
			{!disableOptions && <>
				<Dropdown className="huddle-chat__header-options" label={dropdownOptionsJsx} variant="link" align="right">
					<li>
						<button type="button" className="btn dropdown-item">
							<i className="fal fa-fw fa-cog"></i> {`Device settings ...`}
						</button>
					</li>
					<li>
						<hr className="dropdown-divider" />
					</li>
					<li>
						<h6 className="dropdown-header">
							{`Bandwidth preferences`}
						</h6>
					</li>
					<li>
						<div class="form-check dropdown-item">
							<input className="form-check-input" type="radio" name="bandwidthPreference" id="bandwidthHigh"
								checked={bandwidth == 3 ? true : undefined}
								onChange={(e) => {
									console.log("bandwidth high - input");
								}} />
							<label className="form-check-label" htmlFor="bandwidthHigh">
								{`High (prefer quality)`}
							</label>
						</div>
					</li>
					<li>
						<div class="form-check dropdown-item">
							<input className="form-check-input" type="radio" name="bandwidthPreference" id="bandwidthLow"
								checked={bandwidth == 2 ? true : undefined}
								onChange={(e) => {
									console.log("bandwidth low - input");
								}} />
							<label className="form-check-label" htmlFor="bandwidthLow">
								{`Low (prefer speed)`}
							</label>
						</div>
					</li>
					<li>
						<div class="form-check dropdown-item">
							<input className="form-check-input" type="radio" name="bandwidthPreference" id="bandwidthOptimised"
								checked={bandwidth == 1 ? true : undefined}
								onChange={(e) => {
									console.log("bandwidth optimized");
								}} />
							<label className="form-check-label" htmlFor="bandwidthOptimised">
								{`Optimised (high for screensharing only)`}
							</label>
						</div>
					</li>
					<li>
						<hr className="dropdown-divider" />
					</li>
					<li>
						<div className="form-check form-switch dropdown-item">
							<input className="form-check-input" type="checkbox" role="switch" id="noiseCancellation" checked={noiseCancellation ? true : undefined}
								onChange={(e) => {
									console.log("noise cancellation - input");
									//props.toggleNoiseCancellation();
								}} />
							<label className="form-check-label" htmlFor="noiseCancellation">
								{noiseCancellation ? `Noise cancellation on` : `Noise cancellation off`}
							</label>
						</div>
					</li>
					{/*
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
					 */}

					{/*
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