import Tile from 'Admin/Tile';
import Loop from 'UI/Loop';
import Time from 'UI/Time';
import Default from 'Admin/Layouts/Default';
import webRequest from 'UI/Functions/WebRequest';


var _latest = null;

export default function Automations (props) {
	
	var [running, setRunning] = React.useState({});
	
	var runAutomation = (entry) => {
		
		entry.lastTrigger = new Date().toISOString();
		
		var newRunning = {...running};
		newRunning[entry.name] = true;
		_latest = newRunning;
		setRunning(newRunning);
		
		var doneRunning = () => {
			var run = {..._latest};
			delete run[entry.name];
			setRunning(run);
		};
		
		webRequest('automation/' + entry.name + '/run').then(() => {
			doneRunning();
		}).catch(e => {
			console.error(e);
			doneRunning();
		});
		
	};
	
	var renderHeader = () => {
		
		return [
			<th>
				{`Name`}
			</th>,
			<th>
				{`Schedule`}
			</th>,
			<th>
				{`Last ran`}
			</th>,
			<th>
				{`Actions`}
			</th>
		];
		
	};

	var renderColgroups = () => {
		return [
			<col></col>,
			<col></col>,
			<col></col>
		];
	};
	
	var renderEntry = (entry) => {
		return [
			<td>{entry.name}</td>,
			<td>{entry.cronDescription} ({entry.cron})</td>,
			<td>{entry.lastTrigger ? <Time date={entry.lastTrigger}/> : `None since startup`}</td>,
			<td>
				<button disabled={running[entry.name]} className="btn btn-primary" onClick={() => {
					
					runAutomation(entry);
					
				}}>Run Now</button>
			</td>
		];
	};
	
	var renderEmpty = () => {
		return <table className="table">
			<thead>
				<tr>
					{renderHeader()}
				</tr>
			</thead>
			<colgroup>
				{renderColgroups()}
			</colgroup>
			<tbody>
				<tr>
					<td colspan={2} className="table__empty-message">
						{`No automations`}
					</td>
				</tr>
			</tbody>
		</table>;
	}

	return <Default>
		
		<div className="admin-page">
			<header className="admin-page__subheader">
				<div className="admin-page__subheader-info">
					<h1 className="admin-page__title">
						{`Automations`}
					</h1>
					<ul className="admin-page__breadcrumbs">
						<li>
							<a href={'/en-admin/'}>
								{`Admin`}
							</a>
						</li>
						<li>
							{`Automations`}
						</li>
					</ul>
				</div>
			</header>
			<div className="admin-page__content">
				<div className="admin-page__internal">
					<Loop asTable over={"automation/list"}
						orNone={() => renderEmpty()}>
						{[
							renderHeader,
							renderColgroups,
							renderEntry
						]}
					</Loop>
					{props.children}
				</div>
				{/*
				<footer className="admin-page__footer">
					{selectedCount > 0 ? this.renderBulkOptions(selectedCount) : null}
					{this.props.create && <>
						<a href={addUrl} className="btn btn-primary">
							{`Create`}
						</a>
					</>}
				</footer>
				 */}
			</div>
		</div>
	</Default>;

}

Automations.propTypes = {
	children: true
};