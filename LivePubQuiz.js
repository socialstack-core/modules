import webRequest from 'UI/Functions/WebRequest';
import dateTools from 'UI/Functions/DateTools';
import Loading from 'UI/Loading';
import Canvas from 'UI/Canvas';
import Input from 'UI/Input';
import Form from 'UI/Form';
import getContentTypeId from 'UI/Functions/GetContentTypeId';
import { addSeconds} from 'UI/Functions/DateTools';
import Loop from 'UI/Loop';
import webSocket from 'UI/Functions/WebSocket';

// <LivePubQuiz startTime={time} id={quizId} />

// in seconds
var questionDuration = 10;

export default class LivePubQuiz extends React.Component {
	
	// If you want to use state in your react component, uncomment this constructor:
	constructor(props){
		super(props);
		this.state = {
			score: [],
			instanceId: props.instanceId
		};
		
		this.load(props, true);
		this.aggregateScore = this.aggregateScore.bind(this);
	}
	
	componentWillUnmount(){
		this.interval && clearInterval(this.interval);
		webSocket.removeEventListener("PubQuizSubmission", this.aggregateScore);
	}
	
	componentWillReceiveProps(props){
		this.load(props);
	}
	
	aggregateScore(msg) {
		if (msg.all) {
			if (msg.type == "status") {
				if (msg.connected) {
					// Force a reload:
					this.load(this.props, true);
				}
			}
			return;
		}

		var entity = msg.entity;
		
		if(!entity){
			return;
		}
		
		var entityId = entity.id;

		if (msg.method == 'create') {
			var score = this.state.score;
			
			if (!score.find(ent => ent && ent.id == entityId)) {
				score.push(entity);
				this.setState({score});
			}

		}
	}

	load(props, force){

		if(!props.id && !force) {
			return;
		}

		if(!force && props.id && props.instanceId == this.state.instanceId){
			return;
		}

		webRequest('pubquizsubmission/list', {where:{
			ActivityInstanceId: props.instanceId
		}}).then(response => {
			this.setState({score: response.json.results});
		});
		
		webRequest('pubquizquestion/list', {where:{
			PubQuizId: props.id
		}}).then(response => {

			webSocket.addEventListener('PubQuizSubmission', this.aggregateScore, {where:{
				ActivityInstanceId: props.instanceId
			}});

			var questions = response.json.results;

			// Next, we need to establish where we are on the timing side.
			// Each question lasts for 10 seconds.
			var start = this.props.startTime || new Date();
			var startTicks = start.valueOf();
			var durationTicks = (1000 * questions.length * questionDuration);
			var end = new Date(startTicks + durationTicks);
			
			var state = {
				id: props.id,
				instanceId: props.instanceId,
				questions,
				start,
				startTicks,
				durationTicks,
				end
			}
			
			var active = this.getActiveQuestionInfo(state);
			state.active = active;
			this.setState(state);
			
			if(!active.finished && !this.interval){
				this.interval=setInterval(() => {
					// Tick!
					var next = this.getActiveQuestionInfo();

					// Let's see if this is a questions tranisition
					if(this.state.active && this.state.active.question && next && next.question && next.question.id != this.state.active.question.id) 
					{
						if(this.state.answer){
							webRequest('pubquizsubmission', {
								activityInstanceId: this.props.instanceId,
								pubQuizAnswerId: this.state.answer.id
							});
							this.setState({answer: null});
						}
					}


					this.setState({active: next});
					
					if(next.finished){
						this.interval && clearInterval(this.interval);
						
						if(this.state.active && this.state.active.question) {
							if(this.state.answer){
								webRequest('pubquizsubmission', {
									activityInstanceId: this.props.instanceId,
									pubQuizAnswerId: this.state.answer.id
								});
								
								this.setState({answer: null});
							}
						} 
						
					}
					
				}, 1000);
			}
		});
	}
	
	getActiveQuestionInfo(state){
		state = state || this.state;
		
		// Figure out the progression:
		var progressTicks = new Date().valueOf() - state.startTicks;
		var progress = progressTicks / state.durationTicks;
		
		if(progress > 1){
			return {
				finished: true
			};
		}
		
		if(progress < 0){
			return {
				before: true,
				startsInSeconds: -progressTicks / 1000
			};
		}
		
		// Actively running!
		// map progress to an array index in questions:
		var questions = state.questions;
		var indexProgression = progress * questions.length;
		var index = Math.floor(indexProgression);
		
		var timeTaken = (indexProgression - index) * questionDuration;
		
		return {
			index,
			timeTaken,
			remainingTime: questionDuration - timeTaken,
			question: questions[index]
		};
	}
	
	renderScores(){
		var score = this.state.score;
		var users = {};

		score.forEach(scr => {
			var userId = scr.userId;

			if(users[userId]){
				users[userId].scores.push(scr);
				if(scr.isCorrect) {
					users[userId].total++;
				}
			}else{
			 	users[userId] = { scores: [scr], total: scr.isCorrect ? 1:0, profile: scr.creatorUser};
			}
		})
		
		var { user } = global.app.state;
		
		if(user && !users[user.id]){
			// They didn't answer anything
			users[user.id] = {
				scores: [],
				total: 0,
				profile: user
			};
		}
		
		var result = [];

		for(var userId in users ) {
			var user = users[userId];
			result.push(<div>
				{user.profile ? user.profile.firstName + " " + user.profile.lastName : "Anonymous"}
				{": " + user.total}
			</div>);
		}

		return result;
	}

	renderFinished(){
		var {huddleId, id} = this.props;
		var {score} = this.state;
		
		return <div>
			<h1>
				Quiz finished!
			</h1>
			<p>
				Here's how everyone did:
			</p>

			{this.renderScores()}
			
			<Form
				action={"huddle/" + huddleId}
				submitLabel="End Activity"
				successMessage="Activity Ended!"
				failedMessage="We weren't able to end your activity right now"
				onSuccess={response => {
					this.props.updateHuddle && this.props.updateHuddle(response);
				}}
				onValues={values => {
					values.activityStartUtc = null;
					values.activityContentTypeId = 0;
					values.activityContentId = 0;
					return values;
				}}
			>

			</Form>
		</div>;
	}
	
	renderQuestion(active){
		var {question} = active;
		
		return <div>
			<h1>
				<Canvas>
					{question.questionJson}
				</Canvas>
			</h1>
			<h2>
				<i class="far fa-clock"></i> {Math.floor(active.remainingTime)}
			</h2>
			<div>
				{question.answers ? question.answers.map(answer => {
					
					return <p>
						<Input id = {answer.id} type="radio" name={"question_" + question.id} onChange={e => {
							console.log('Chg!');
							this.setState({
								answer
							});
						}} value={this.state.answer == answer} defaultValue={this.state.answer == answer}/>
						<Canvas>
							{answer.answerJson}
						</Canvas>
					</p>;
					
				}) : ''}
			</div>
			<p>
				Vote for what you think is the right answer. The goal is to beat everybody else!
			</p>
		</div>;
	}
	
	renderBefore(active){

		return <div>
			<h1>
				On your marks!
			</h1>
			<p>
				<i class="far fa-clock"></i> Quiz starting in {Math.floor(active.startsInSeconds)}..
			</p>
		</div>;
	}
	
	render(){
		
		var { active } = this.state;
		
		if(!active){
			return <Loading />;
		}

		return <div className="live-pub-quiz">
			{active.finished && this.renderFinished()}
			{active.before && this.renderBefore(active)}
			{active.question && this.renderQuestion(active)}
		</div>;
		
	}
}

LivePubQuiz.propTypes = {
	startTime: 'date',
	id: 'int'
};