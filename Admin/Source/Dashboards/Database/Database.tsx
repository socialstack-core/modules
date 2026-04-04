import Tile from "Admin/Tile";
import { useState } from "react"
import Alert from "UI/Alert";
import Form from "UI/Form";
import Input from "UI/Input";
import stdOutApi from "Api/StdOutController";
import AdminPage from "Admin/AdminPage";
import Html from "UI/Html";

declare global {
    var recordSets: RecordSet[];
}

export type RecordSet = {
    fields: string[],
    results: Record<string,string|boolean|number>[]
}

const renderResults = (sets:RecordSet[]) => {
		
    return sets.map((set: RecordSet) => {
        
		return <table className="table ui-table ui-table--sm table-striped admin-dashboard__database-resultset">
			<thead>
				<tr>
					{set.fields.map((fieldName: string) =>
						<th>
							{fieldName}
						</th>
					)}
				</tr>
            </thead>
			<tbody>
				{set.results.map((resultRow: Record<string, any>) =>
					<tr>
						{resultRow.map((field: string) =>
							<td>
								{field}
							</td>
						)}
					</tr>
				)}
            </tbody>
        </table>;
    });
    
};

const renderRuns = (runs: any) => {
        
    return runs.map((run: any) => {
        
        return <div className="admin-dashboard__database-run">
            <p>
                {run.affectedRows >= 0 ? `Rows affected: ${run.affectedRows}` : ''}
            </p>
            {run.error ? <Alert type='error'>{`${run.error}`}</Alert> : (
                run.sets && run.sets.length > 0 ? renderResults(run.sets) : `OK, No result sets in this run.`
            )}
        </div>;
        
    });
    
};
    

const Database: React.FC<{}> = (): React.ReactNode => {
	const [runs, setRuns] = useState<any[]>([]);

	return <>
		<AdminPage.SubHeader title={`Database Query`} breadcrumbs={[
			{
				title: `Database Query`
			}
		]} />
		<AdminPage.ContentWrapper>
			<AdminPage.Content>
				<div className="admin-dashboard admin-dashboard--database">
					<Alert variant='info'>
						<Html>
							{`Tip: Use your browsers JS console to perform analysis. Within a session, every result set will be available via <code>window.recordSets</code>`}
						</Html>
					</Alert>
					<Form
						action={(values: any) => stdOutApi.runQuery({ query: values.query })}
						loadingMessage={`Running query..`}
						submitLabel={`Execute Query`}
						onSuccess={response => {
							var run = JSON.parse(response) as any;

							if (!window.recordSets) {
								window.recordSets = [];
							}

							if (run.sets) {
								window.recordSets = window.recordSets.concat(run.sets);
							}

							var newRuns = runs.slice();
							newRuns.unshift(run);
							setRuns(newRuns);
						}}
					>
						<Input
							type='sql'
							name='query'
						/>
					</Form>
				</div>
				{runs.length ? renderRuns(runs) : <center>{`No results yet`}</center>}
			</AdminPage.Content>
		</AdminPage.ContentWrapper>
	</>;

}

export default Database;