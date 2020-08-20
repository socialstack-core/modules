import webRequest from 'UI/Functions/WebRequest';
import dateTools from 'UI/Functions/DateTools';
import Loading from 'UI/Loading';
import Canvas from 'UI/Canvas';
import Input from 'UI/Input';
import Form from 'UI/Form';

// <LivePubQuiz startTime={time} id={quizId} />

// in seconds
var questionDuration = 10;

export default class LivePubQuiz extends React.Component {
	
	// If you want to use state in your react component, uncomment this constructor:
	constructor(props){
		super(props);
		this.state = {
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
		if(props.id == this.state.id){
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
					this.setState({active: next});
					
					if(next.finished){
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
		return <div>
			<h1>
				Quiz finished!
			</h1>
			<p>
				Here's how you did
			</p>
			<p>
				Scores TBD
			</p>
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
				{Math.floor(active.remainingTime)}
			</h2>
			<div>
				{question.answers ? question.answers.map(answer => {
					
					return <p>
						<Input type="radio" name={"question_" + question.id} />
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
				Quiz starting in {active.startsInSeconds}..
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