import ws from 'UI/Functions/WebSocket';
import Alert from 'UI/Alert';

function requestFor(writer, cType, id){
	writer.writeByte(5);
	writer.writeUInt32(8);
	writer.writeUInt32(cType);
	writer.writeUInt32(id);
}

function blockRequest(count, cType, id){
	var writer = new ws.Writer();
	
	for(var i=0;i<count;i++){
		requestFor(writer);
	}
	
	return writer;
}

var wsStages = [50, 100, 500, 1000, 5000, 25000, 50000, 100000];

class WsStressTest extends React.Component {
	
	constructor(props){
		super(props);
		this.state={};
	}
	
	websocketAdvance(runResults) {
		var {running, results} = this.state;
		
		var stage = 0;
		
		if(running){
			stage = running.stage + 1;
		}
		
		if(runResults){
			if(!results){
				results = [];
			}
			var newResults = results.slice();
			newResults.push(runResults);
			this.setState({results:newResults});
			
			if(!runResults.pass){
				this.setState({running:null, score: Math.floor(runResults.score * runResults.target)});
				return;
			}
		}
		
		this.setState({running: this.runWebsocket(wsStages, stage)});
	};
	
	runWebsocket(stages, stageIndex) {
		var rps = stages[stageIndex];
		
		var blockSize = rps/20;
		var writer = blockRequest(blockSize, 1, 1);
		var c = 0;
		var total = 0;
		var target = 0;
		
		var onGotMessage = () => {
			c++;
			total++;
		};
		
		ws.getSocket().addEventListener("message", onGotMessage);
		
		// 20x per sec
		var sendI = setInterval(() => {
			ws.send(writer);
		}, 50);
		
		var countU = setInterval(() => {
			this.setState({count:c});
			c=0;
			target+=rps;
		}, 1000);
		
		var _stopped = false;
		
		var stop = () => {
			if(_stopped){
				return;
			}
			_stopped = true;
			clearInterval(sendI);
			clearInterval(countU);
			endI && clearTimeout(endI);
			ws.getSocket().removeEventListener("message", onGotMessage);
		};
		
		var endI = setTimeout(() => {
			
			stop();
			var score = total/target;
			
			if(score < 0){
				score = 0;
			}else if(score > 1){
				score = 1;
			}
			
			// Scores 95% or higher, proceed to the next stage.
			this.websocketAdvance({
				score,
				target: rps,
				pass: (score > 0.95)
			});
			
		}, 5000);
		
		return {
			intervals: [endI, countU, sendI],
			stage: stageIndex,
			target: rps,
			stop
		};
	}
	
	render(){
		var {count, running, results, score} = this.state;
		
		if(running){
			return <p>
				<center>
					<h1>
						{count}
					</h1>
					<h2>
						Responses received in the last second (5 second runtime)
					</h2>
					<p>
						{`Target is ${running.target}+`}
					</p>
					{results && results.map(result => {
						
						return <div>
							<span style={{color: 'green'}}>{result.target} Passed</span> {(result.score*100).toFixed(1)}%
						</div>;
						
					})}
				</center>
			</p>;
		}
		
		return <>
			{score ? <>
				<h2>{score} RPS</h2>
				{score < 5000 && <Alert type='info'>
					{`Lower scores are typically a measurement of your database performance. Try turning the cache on and go again.`}
				</Alert>}
				{score > 5000 && score < 50000 && <Alert type='info'>
					{`Scores in this range indicate the cache is on but the client was not able to get the throughput it needs. Confirm this by checking the CPU usage chart of your API - if it was idle throughout the test, this client was the limiting factor. Use more at once.`}
				</Alert>}
			</>	: null}
			<button className="btn btn-primary" onClick={() => this.websocketAdvance()}>Websocket GET stress test (experimental builds only)</button>
		</>;
	}
	
}

export default function StressTest(){
	
	return <p>
			<center>
				<WsStressTest />
			</center>
	</p>;
	
}