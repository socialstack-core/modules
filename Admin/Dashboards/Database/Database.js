import Tile from 'Admin/Tile';
import Form from 'UI/Form';
import Input from 'UI/Input';
import Alert from 'UI/Alert';

/**
 * Raw database querying (developer role only).
 */
export default function Database() {
	
	var [runs, setRuns] = React.useState([]);
	
	var renderResults = (sets) => {
		
		return sets.map(set => {
			
			return <table className="table result-set table-striped">
				<thead>
					{set.fields.map(fieldName => <th>{fieldName}</th>)}
				</thead>
				<tbody>
				{set.results.map(resultRow => <tr>{resultRow.map(field => <td>{field}</td>)}</tr>)}
				</tbody>
			</table>;
			
		});
		
	};
	
	var renderRuns = () => {
		
		return runs.map(run => {
			
			return <div className="db-run">
				<p>
					{run.affectedRows >= 0 ? `Rows affected: ${run.affectedRows}` : ''}
				</p>
				{run.error ? <Alert type='error'>{`${run.error}`}</Alert> : (
					
					run.sets && run.sets.length > 0 ? renderResults(run.sets) : `OK, No result sets in this run.`
					
				)}
			</div>;
			
		});
		
	};
	
	return <div className="dashboards-database">
		<Tile>
			<Alert type='info'>
				Tip: Use your browsers JS console to perform analysis. Within a session, every result set will be available via <b>window.recordSets</b>
			</Alert>
			<Form action='monitoring/query' 
			loadingMessage={`Running query..`}
			submitLabel={`Execute Query`}
			onSuccess={response => {
				
				if(!window.recordSets){
					window.recordSets = [];
				}
				
				if(response.sets){
					window.recordSets = window.recordSets.concat(response.sets);
				}
				
				var newRuns = runs.slice();
				newRuns.unshift(response);
				setRuns(newRuns);
			}}>
			<Input type='textarea' contentType='application/sql' name='query' />
			</Form>
		</Tile>
		<Tile>
			{runs.length ? renderRuns() : <center>{`No results yet`}</center>}
		</Tile>
	</div>;
}