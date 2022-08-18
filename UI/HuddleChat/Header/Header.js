import Dropdown from 'UI/Dropdown';
import Modal from 'UI/Modal';
import Emoji from 'UI/Emoji';
import AvTest from '../AvTest';
import { isoConvert, toLocaleUTCTimeString, isSameDay } from "UI/Functions/DateTools";
import { useSession } from 'UI/Session';
import { useState } from 'react';

export default function Header(props) {
	const { session } = useSession();
	const [ deviceSettingsShown, setDeviceSettingsShown ] = useState(false);

	var {
		huddleClient,
		title, description, users,
		notifications, showingNotifications,
		showingAudience, showingConversation,
		disableChat, disableAudience, disableReactions, disableOptions,
		bandwidth, noiseCancellation,
		recordMode
	} = props;

	if (!bandwidth) {
		bandwidth = 3;
	}

	if (!title || !title.length) {
		title = `Huddle Meeting`;
    }
	
	var huddleType = huddleClient.huddle.huddleType;
	var recordingOn = recordMode == 1;
	
	var start = isoConvert(huddleClient.huddle.startTimeUtc);
	var end = isoConvert(huddleClient.huddle.estimatedEndTimeUtc);

	var formattedStart = toLocaleUTCTimeString(start, session.locale ? session.locale.code : undefined, {
		weekday: 'short',
		day: 'numeric',
		month: 'short',
		hour: 'numeric',
		minute: '2-digit',
		//hour12: true,
	});

	var formattedEnd = toLocaleUTCTimeString(end, session.locale ? session.locale.code : undefined,
		isSameDay(start, end) ?
			{
				hour: 'numeric',
				minute: '2-digit',
				//hour12: true,
				timeZoneName: 'short'
			} :
			{
				weekday: 'short',
				day: 'numeric',
				month: 'short',
				hour: 'numeric',
				minute: '2-digit',
				//hour12: true,
				timeZoneName: 'short'
			}
	);

	var dropdownBurgerJsx = <>
		<i className="fal fa-bars"></i> <span className="dropdown-label-internal">
			{`Menu`}
		</span>
	</>;

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

	var emptyHuddle = !users || users.length <= 1;

	var notificationsTitle = (notifications && notifications.length) ? (showingNotifications ? `Hide notifications` : `Show notifications`) : `No notifications`;
	var notificationsClass = showingNotifications ? "btn huddle-chat__header-notifications btn--active" : "btn huddle-chat__header-notifications";
	var audienceClass = showingAudience ? "btn huddle-chat__header-audience btn--active" : "btn huddle-chat__header-audience";
	var conversationClass = showingConversation ? "btn huddle-chat__header-conversation btn--active" : "btn huddle-chat__header-conversation";

	var controlsClass = ["huddle-chat__header-controls"];

	if (disableAudience) {
		controlsClass.push("huddle-chat__header-controls--no-audience");
    }

	if (disableChat) {
		controlsClass.push("huddle-chat__header-controls--no-chat");
	}

	if (disableReactions) {
		controlsClass.push("huddle-chat__header-controls--no-reactions");
	}

	if (disableOptions) {
		controlsClass.push("huddle-chat__header-controls--no-options");
	}

	function notificationsButton(isBurger) {
		return <>
			<button type="button" className={isBurger ? 'dropdown-item' : notificationsClass}
				onClick={() => props.toggleNotifications()}
				title={isBurger ? undefined : notificationsTitle}
				disabled={notifications && notifications.length ? undefined : 'disabled'}>
				{isBurger && <>
					<i className="fal fa-fw fa-bell"></i>
					{notificationsTitle} {notifications && notifications.length > 0 && <span className="badge bg-danger">{notifications.length}</span>}
				</>}

				{!isBurger && <>
					<span className="fa-layers fa-fw">
						<i className="fal fa-bell"></i>
						{notifications && notifications.length > 0 && <>
							<span className="fa-layers-counter">
								{notifications.length > 99 ? '99+' : notifications.length}
							</span>
						</>}
					</span>
				</>}
			</button>
		</>;
    }

	function audienceButton(isBurger) {
		return <>
			<button type="button" className={isBurger ? 'dropdown-item' : audienceClass}
				onClick={() => props.toggleAudience()} 
				title={isBurger ? undefined : (showingAudience ? `Hide audience` : `Show audience`)}
				disabled={emptyHuddle ? "disabled" : undefined}>
				<i className="fal fa-fw fa-users"></i>
				{isBurger && (showingAudience ? `Hide audience` : `Show audience`)}
			</button>
		</>;
	}

	function chatButton(isBurger) {
		return <>
			<button type="button" className={isBurger ? 'dropdown-item' : conversationClass}
				onClick={() => props.toggleConversation()} 
				title={isBurger ? undefined : (showingConversation ? `Hide conversation` : `Show conversation`)}
				disabled={emptyHuddle ? "disabled" : undefined}>
				<i className="fal fa-fw fa-comments"></i>
				{isBurger && (showingConversation ? `Hide conversation` : `Show conversation`)}
			</button>
		</>;
	}

	{/* TODO */ }
	function reactionsDropdown(isBurger) {
		return <>

			{isBurger && <>
				<li>
					<h6 className="dropdown-header">
						{`Reactions`}
					</h6>
				</li>
				<li>
					<div className="dropdown-reactions">
						<button type="button" className="btn">
							<Emoji symbol={'0x1F44D'} label={`Thumbs up`} /> {`Like`}
						</button>
						<button type="button" className="btn">
							<Emoji symbol={'0x1F497'} label={`Heart`} /> {`Heart`}
						</button>
						<button type="button" className="btn">
							<Emoji symbol={'0x1F44F'} label={`Clapping hands`} /> {`Applause`}
						</button>
						<button type="button" className="btn">
							<Emoji symbol={'0x1F602'} label={`Laughing face`} /> {`Laugh`}
						</button>
						<button type="button" className="btn">
							<Emoji symbol={'0x1F632'} label={`Astonished face`} /> {`Surprised`}
						</button>
						<button type="button" className="btn">
							<Emoji symbol={'0x270B'} label={`Raised hand`} /> {`Raise hand`}
						</button>
					</div>
				</li>
			</>}

			{!isBurger && <>
				<Dropdown className={isBurger ? 'dropdown-item' : 'huddle-chat__header-reactions'}
					label={dropdownReactionsJsx} variant="link" align="right"
					disabled={emptyHuddle ? "disabled" : undefined}>
					<li>
						<button type="button" className="btn dropdown-item">
							<Emoji symbol={'0x1F44D'} label={`Thumbs up`} /> {`Like`}
						</button>
					</li>
					<li>
						<button type="button" className="btn dropdown-item">
							<Emoji symbol={'0x1F497'} label={`Heart`} /> {`Heart`}
						</button>
					</li>
					<li>
						<button type="button" className="btn dropdown-item">
							<Emoji symbol={'0x1F44F'} label={`Clapping hands`} /> {`Applause`}
						</button>
					</li>
					<li>
						<button type="button" className="btn dropdown-item">
							<Emoji symbol={'0x1F602'} label={`Laughing face`} /> {`Laugh`}
						</button>
					</li>
					<li>
						<button type="button" className="btn dropdown-item">
							<Emoji symbol={'0x1F632'} label={`Astonished face`} /> {`Surprised`}
						</button>
					</li>
					<li>
						<hr className="dropdown-divider" />
					</li>
					<li>
						<button type="button" className="btn dropdown-item">
							<Emoji symbol={'0x270B'} label={`Raised hand`} /> {`Raise hand`}
						</button>
					</li>
				</Dropdown>
			</>}

		</>;
    }

	function optionsDropdown(isBurger) {
		return <>
			{isBurger && <>
				<li>
					<h6 className="dropdown-header">
						{`Options`}
					</h6>
				</li>
				<li>
					<button type="button" className="btn dropdown-item" onClick={() => setDeviceSettingsShown(true)}>
						<i className="fal fa-fw fa-cog"></i> {`Device settings ...`}
					</button>
				</li>
				<li>
					<hr className="dropdown-divider" />
				</li>
				<li>
					<div className="form-check dropdown-item">
						<input className="form-check-input" type="radio" name="bandwidthPreference" id="bandwidthHigh"
							checked={bandwidth == 3 ? true : undefined}
							onChange={(e) => {
								console.log("bandwidth high - input");
							}} />
						<label className="form-check-label" htmlFor="bandwidthHigh">
							{`High bandwidth (prefer quality)`}
						</label>
					</div>
				</li>
				<li>
					<div className="form-check dropdown-item">
						<input className="form-check-input" type="radio" name="bandwidthPreference" id="bandwidthLow"
							checked={bandwidth == 2 ? true : undefined}
							onChange={(e) => {
								console.log("bandwidth low - input");
							}} />
						<label className="form-check-label" htmlFor="bandwidthLow">
							{`Low bandwidth (prefer speed)`}
						</label>
					</div>
				</li>
				<li>
					<div className="form-check dropdown-item">
						<input className="form-check-input" type="radio" name="bandwidthPreference" id="bandwidthOptimised"
							checked={bandwidth == 1 ? true : undefined}
							onChange={(e) => {
								console.log("bandwidth optimized");
							}} />
						<label className="form-check-label" htmlFor="bandwidthOptimised">
							{`Optimised bandwidth (high for screensharing only)`}
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
							{`Noise cancellation`}
						</label>
					</div>
				</li>
			</>}

			{!isBurger && <>
				<Dropdown className='huddle-chat__header-options' label={dropdownOptionsJsx} variant="link" align="right">
					<li>
						<button type="button" className="btn dropdown-item" onClick={() => setDeviceSettingsShown(true)}>
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
						<div className="form-check dropdown-item">
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
						<div className="form-check dropdown-item">
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
						<div className="form-check dropdown-item">
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
								{`Noise cancellation`}
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

		</>;
	}

	return <>
		<header className="huddle-chat__header">
			<span className="huddle-chat__header-info">
				<h1 className="huddle-chat__header-title" data-clamp="1">
					{recordingOn && <>
						<i className="fas fa-fw fa-circle" title={`Recording in progress`}></i>
					</>}
					{title}
				</h1>
				{description && description.length && <>
					<p className="huddle-chat__header-description" data-clamp="1">
						{description}
					</p>
				</>}
				<p className="huddle-chat__header-time">
					<i className="fal fa-fw fa-calendar"></i>
					<time datetime={start.toISOString()}>{formattedStart}</time>&ndash;<time datetime={end.toISOString()}>{formattedEnd}</time>
				</p>
			</span>

			<span className={controlsClass.join(' ')}>
				{/* burger menu */}
				<Dropdown className="huddle-chat__header-burger" label={dropdownBurgerJsx} variant="link" align="right">
					<li>
						{notificationsButton(true)}
					</li>
					{(!disableAudience || !disableChat) && <>
						<li>
							<hr className="dropdown-divider" />
						</li>
					</>}
					{!disableAudience && <>
						<li>
							{audienceButton(true)}
						</li>
					</>}
					{!disableChat && <>
						<li>
							{chatButton(true)}
						</li>
					</>}
					{(!disableAudience || !disableChat) && (!disableReactions || !disableOptions) && <>
						<li>
							<hr className="dropdown-divider" />
						</li>
					</>}
					{!disableReactions && reactionsDropdown(true)}
					{(!disableReactions && !disableOptions) && <>
						<li>
							<hr className="dropdown-divider" />
						</li>
					</>}
					{!disableOptions && optionsDropdown(true)}
				</Dropdown>

				{/* notifications */}
				{notificationsButton(false)}

				{/* audience */}
				{!disableAudience && audienceButton(false)}

				{/* conversation */}
				{!disableChat && chatButton(false)}

				{/* reactions */}
				{!disableReactions && reactionsDropdown(false)}

				{/* options */}
				{!disableOptions && optionsDropdown(false)}
			</span>

			{/*
	call health
	meeting notes
	gallery
	gallery at top
	full screen
	help

	spotlight
			 */}

		{/* active time / time remaining? */}
		</header>
		{deviceSettingsShown && <>
			<Modal visible className="device-settings-modal" title={`Device Settings`} onClose={() => setDeviceSettingsShown(false)}>
				<AvTest onDeviceSelect={(newHints) => {
					/* TODO */
				}} huddleReadyCallback={() => {/* TODO */}}
					displayName={{/* TODO */}}
					onChangeName={newName => {/* TODO */}} />
				<footer>
					<button type="button" className="btn btn-primary" onClick={() => {
						/* TODO */
						setDeviceSettingsShown(false);
					}}>
						{`OK`}
					</button>
					<button type="button" className="btn btn-outline-primary" onClick={() => setDeviceSettingsShown(false)}>
						{`Cancel`}
					</button>
				</footer>
			</Modal>
		</>}
	</>;
}