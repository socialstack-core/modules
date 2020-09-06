import webRequest from 'UI/Functions/WebRequest';
import dateTools from 'UI/Functions/DateTools';
import Loading from 'UI/Loading';
import Canvas from 'UI/Canvas';
import Input from 'UI/Input';
import Form from 'UI/Form';
import getContentTypeId from 'UI/Functions/GetContentTypeId';
import { addSeconds} from 'UI/Functions/DateTools';
import Loop from 'UI/Loop';

// <LivePubQuiz startTime={time} id={quizId} />

// in seconds
var questionDuration = 10;

export default class LivePubQuiz extends React.Component {
	
	// If you want to use state in your react component, uncomment this constructor:
	constructor(props){
		super(props);
		this.state = {
			score: {}
		};
		
		this.load(props);
	}
	
	componentWillUnmount(){
		this.interval && clearInterval(this.interval);
	}
	
	componentWillReceiveProps(props){
		this.load(props);
	}
	
	load(props){

		if(!props.id) {
			return;
		}

		if(props.id && props.id == this.state.id && props.start == this.state.start){
			return;
		}
		
		webRequest('pubquizquestion/list', {where:{
			PubQuizId: props.id
		}}).then(response => {

			var questions = response.json.results;

			// Next, we need to establish where we are on the timing side.
			// Each question lasts for 10 seconds.
			var start = this.props.startTime || new Date();
			var startTicks = start.valueOf();
			var durationTicks = (1000 * questions.length * questionDuration);
			var end = new Date(startTicks + durationTicks);
			
			var state = {
				id: props.id,
				questions,
				start,
				startTicks,
				durationTicks,
				end
			}
			
			var active = this.getActiveQuestionInfo(state);
			state.active = active;
			this.setState(state);
			
			if(!active.finished){
				this.interval=setInterval(() => {
					// Tick!
					var next = this.getActiveQuestionInfo();

					// Let's see if this is a questions tranisition
					if(this.state.active && this.state.active.question && next && next.question && next.question.id != this.state.active.question.id) 
					{
						var radios = document.getElementsByName('question_'+this.state.active.question.id);

						for (var i = 0, length = radios.length; i < length; i++) {
							if (radios[i].checked) {
								var value = radios[i].id;
								break;
							}
						}

						webRequest('pubquizsubmission', {
							activityInstanceId: this.props.instanceId,
							pubQuizAnswerId: value
						});
					}


					this.setState({active: next});
					
					if(next.finished){
						
						if(this.state.active && this.state.active.question) {
							var radios = document.getElementsByName('question_'+this.state.active.question.id);

							for (var i = 0, length = radios.length; i < length; i++) {
								if (radios[i].checked) {
									var value = radios[i].id;
									break;
								}
							}

							webRequest('pubquizsubmission', {
								activityInstanceId: this.props.instanceId,
								pubQuizAnswerId: value
							}).then(response => {
								/*
								// Since this the last submission, let's get the answers as well
								webRequest('pubquizsubmission/list',{
									ActivityInstanceId : this.props.instanceId
								}).then(response => {
									console.log("submissions for quiz:");
									console.log(response);
								})

								*/
							});
						} 
						this.interval && clearInterval(this.interval);
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
	
	renderFinished(){
		var {huddleId, id} = this.props;
		var {score} = this.state;

		console.log(score);

		return <div>
			<h1>
				Quiz finished!
			</h1>
			<p>
				Here's how you did
			</p>
			<h3>
				Scores
			</h3>
			
			{!this.state.score && <Loop
				over = {"pubquizsubmission/list"}
				filter = {{where : {ActivityInstanceId : this.props.instanceId}}}
			>
				{submission => {
					var score = this.state.score;

					// Was the creatorUser of this submission
					var creator = submission.creatorUser;

					// Do we have an entry for this user?
					if (!score[creator.id]) {
						creator.score = 0;
						score[creator.id] = creator;
					}

					var answer = submission.pubQuizAnswer;

					if(answer.isCorrect) {
						score[creator.id].score = score[creator.id].score + 1;
					}

					this.setState({score: score});
				}}
			</Loop>}

			<Form
				action={"huddle/" + huddleId}
				submitLabel="Restart"
				successMessage="Activity restarted!"
				failedMessage="We weren't able to restart your activity right now"
				onSuccess={response => {
					this.props.updateHuddle && this.props.updateHuddle(response);
				}}
				onValues={values => {
					values.activityStartUtc = addSeconds(new Date(), 10);
					values.activityContentTypeId = getContentTypeId("PubQuiz");
					values.activityContentId = id
					return values;
				}}
			>

			</Form>
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
						<Input id = {answer.id} type="radio" name={"question_" + question.id} />
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