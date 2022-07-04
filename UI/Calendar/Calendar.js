import dateTools from 'UI/Functions/DateTools';
import Row from 'UI/Row';
import Form from 'UI/Form';
import Input from 'UI/Input';
import formatTime from 'UI/Functions/FormatTime';

const months = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

export default class Calendar extends React.Component {
	// If you want to use state in your react component, uncomment this constructor:
	constructor(props){
		super(props);
		this.state = {
			offset: 0,
			activeDay: null
		};

		if (this.props.date) {
			this.state.activeDay = new Date(this.props.date);
		}
		// Was an active day passed in via the appointment?
		else if (this.props.appointment && this.props.appointment.date) {
			this.state.activeDay = new Date(this.props.appointment.date);
		}
	}

	renderCalendar() {
		var days = ['S', 'M', 'T', 'W', 'T', 'F', 'S'];
		var weeks = [];
		var weekCount = 5;
		var today = new Date();
		var monthOffset = this.state.offset;
		var startDay = new Date(today.getFullYear(), today.getMonth() + monthOffset, 1);
		var offset = startDay.getDay();

		var lastDay;

		var {activeDay} = this.state;
		var {exclusionStartUtc, exclusionEndUtc} = this.props;

		var exclusionStartDate = new Date(exclusionStartUtc);
		var exclusionEndDate = new Date(exclusionEndUtc);

		var weeksRow = days.map((day, index) => {
			return <th>{day}</th>
		});

		weeks.push(<thead><tr>{weeksRow}</tr></thead>);

		for(var i=0; i < weekCount; i++) {

			var week = days.map((day, index) => {

				var dayIndex = (i*7) + index - offset;
				var thisDay = dateTools.addDays(startDay, dayIndex);
				var dayNumber = thisDay.getDate();
				var isToday = (dayNumber == today.getDate() && thisDay.getMonth() == today.getMonth() && thisDay.getFullYear() == today.getFullYear());
				var isStartDay = (thisDay.getDate() == startDay.getDate() && thisDay.getMonth() == startDay.getMonth() && thisDay.getFullYear() == startDay.getFullYear());
				var isActiveDay = (activeDay && (thisDay.getDate() == activeDay.getDate() && thisDay.getMonth() == activeDay.getMonth() && thisDay.getFullYear() == activeDay.getFullYear()));
				var notThisMonth = (thisDay.getMonth() != startDay.getMonth() || thisDay.getFullYear() != startDay.getFullYear());
				var isBeforeToday = (today.getTime() > thisDay.getTime());
				var isInExlucion = (exclusionStartUtc && exclusionEndUtc && thisDay.getTime() > exclusionStartDate.getTime() && thisDay.getTime() < exclusionEndDate.getTime());

				lastDay = dayNumber;

				var className = "";

				// Determine the class this td should have.
				if(isActiveDay && !notThisMonth) {
					className = "active-day";
				}else if (notThisMonth) {
					className = "not-this-month";
				}else if(isToday) {
					className = "today";
				}  else if(isBeforeToday || isInExlucion) {
					className = "not-selectable";
				} else {
					className = "normal"
				}

				if (notThisMonth) {
					return <td className = {className} key={index}><span>{""}</span></td>;
				}

				if ((isBeforeToday && !isToday) || isInExlucion) {
					return <td className = {className} key={index}><span>{dayNumber}</span></td>;
				}

				return <td onClick = {() => {this.setState({activeDay: thisDay})}} className = {className} key={index}><span>{dayNumber}</span></td>;
			});

			weeks.push(<tr key = {i}>{week}</tr>);
		}

		return <table>{weeks.map((week, index) => {
			return week;
		})}</table>
	}

	renderHeader() {
		var today = new Date();
		var monthOffset = this.state.offset;
		var startDay = new Date(today.getFullYear(), today.getMonth() + monthOffset, 1);
		return <Row className = "header-row">
			<i className="arrow fas fa-chevron-left" onClick = {() => {this.setState({offset: this.state.offset - 1})}}></i>
			<h6 className="month">{months[startDay.getMonth()] + " " + startDay.getFullYear()}</h6>
			<i className="arrow fas fa-chevron-right" onClick = {() => {this.setState({offset: this.state.offset + 1})}}></i>
		</Row>
	}

	renderForm() {
		return <Form
			action="livesupportmessage"
			onSuccess={response => {
				this.setState({ submitting: false});
			}}
			onFailed={response => {
				this.setState({ submitting: false, failure: response.message || 'Unable to send your message at the moment' });
			}}
			onValues={
				values => {
					values.inReplyTo = this.props.replyTo;
					this.setState({ submitting: true, failure: false });
					values.liveSupportChatId = this.props.chat.id;
					values.message = formatTime(this.state.activeDay, "eu-readable");
					values.messageType = 8;
					values.hiddenDatePayload = this.state.activeDay;
					return values;
				}
			}
			className="message-form"
		>

			<button className="btn btn-primary send-message" type="submit" disabled={!this.state.activeDay} title={"Send message"}>
				<span>Enter</span>
			</button>
		</Form>
	}

	renderUpdateForm() {
		return <Form
			action={"meetingappointment/" + this.props.appointment.id}
			onSuccess={response => {
				this.setState({ submitting: false});
				this.props.onUpdate && this.props.onUpdate(response);
			}}
			onFailed={response => {
				this.setState({ submitting: false, failure: response.message || 'Unable to send your message at the moment' });
			}}
			onValues={
				values => {
					values.date = dateTools.isoConvert(this.state.activeDay);
					return values;
				}
			}
			className="message-form"
		>

			<button className="btn btn-primary send-message" type="submit" disabled={!this.state.activeDay} title={"Send message"}>
				<span>Update</span>
			</button>
		</Form>
	}

	render(){
		return <section className="calendar">
			<p className="hint">Select a date and press enter</p>
			<div className="calendar__inner">
				{this.renderHeader()}
				{this.renderCalendar()}
			</div>
			{this.props.appointment ? this.renderUpdateForm() : this.renderForm()}
		</section>;

	}

}

Calendar.propTypes = {
	exclusionStartUtc: "string",
	exclusionEndUtc: "string",
};
