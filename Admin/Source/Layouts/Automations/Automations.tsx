import Table from 'UI/Table';
import { Filter } from 'UI/Loop';
import Time from 'UI/Time';
import automationsApi, { Automation } from 'Api/AutomationController';
import { useState } from 'react';
import { ApiIncludes } from 'Api/Includes';
import { ApiList } from 'UI/Functions/WebRequest';
import AdminPage from "Admin/AdminPage";
import Button from 'UI/Button';

var _latest: Record<string, boolean> | null = null;

const Automations: React.FC<React.PropsWithChildren<{}>> = (props) => {
	
	var [running, setRunning] = useState<Record<string, boolean>>({});
	
	var runAutomation = (entry : Automation) => {
		
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

		automationsApi.execute(entry.name).then(() => {
			doneRunning();
		}).catch(e => {
			console.error(e);
			doneRunning();
		});
		
	};
	
	var renderHeader = () => {
		return <tr>
			<th>
				{`Name`}
			</th>
			<th>
				{`Schedule`}
			</th>
			<th>
				{`Last ran`}
			</th>
			<th>
				{`Actions`}
			</th>
		</tr>;		
	};

	var renderColgroups = () => {
		return [
			<col></col>,
			<col></col>,
			<col></col>
		];
	};
	
	var renderEntry = (entry: Automation) => {
		return <tr>
			<td>{entry.name}{entry.description && entry.description.length > 0 && <><br /><small>{entry.description}</small></>}</td>
			<td>{entry.cronDescription} ({entry.cron})</td>
			<td>{entry.lastTrigger ? <Time date={entry.lastTrigger}/> : `None since startup`}</td>
			<td>
				<Button disabled={running[entry.name]} sm outlined onClick={() => {
					runAutomation(entry);
				}}>
					{`Run Now`}
				</Button>
			</td>
		</tr>;
	};
	
	var renderEmpty = () => {
		return <table className="table ui-table ui-table--sm">
			<thead>
				{renderHeader()}
			</thead>
			<colgroup>
				{renderColgroups()}
			</colgroup>
			<tbody>
				<tr>
					<td colSpan={2} className="table__empty-message">
						{`No automations`}
					</td>
				</tr>
			</tbody>
		</table>;
	}

	return <>
		<AdminPage.SubHeader title={`Automations`} breadcrumbs={[
			{
				title: `Automations`
			}
		]} />
		<AdminPage.ContentWrapper>
			<AdminPage.Content>
				<Table source={(filter?: Filter<Automation>, includes?: ApiIncludes[]) => {
					return automationsApi.get() as Promise<ApiList<Automation>>;
				}}
					orNone={() => renderEmpty()}
					onHeader={renderHeader}
				>
					{renderEntry}
				</Table>
				{props.children}
			</AdminPage.Content>
		</AdminPage.ContentWrapper>
	</>;
}

export default Automations;