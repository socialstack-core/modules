import CloseButton from 'UI/CloseButton';

export default function Status(props) {
	const { message, showingStatus } = props;

	return <>
		{showingStatus && <>
			<div className="huddle-chat__status">
				<p className="huddle-chat__status-message">
					{message}
				</p>
				<CloseButton className="huddle-chat__status-close" callback={() => props.toggleStatus()} />
			</div>
		</>}
	</>;
}
