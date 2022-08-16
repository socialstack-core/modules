import Container from 'UI/Container';
import Row from 'UI/Row';
import Col from 'UI/Column';
import ProcessSteps from 'UI/ProcessSteps';
import { getSteps } from '../Steps.js';

export default function Wrapper(props) {
	const { className, showSteps, activeStep, isComplete, children } = props;

	return <>
		{showSteps && <>
			<Container className="container--no-shadow">
				<Row>
					<Col size={12}>
						<ProcessSteps className="subscription-process" steps={getSteps()} activeStep={activeStep} isComplete={isComplete} />
					</Col>
				</Row>
			</Container>
		</>}
		<Container>
			<Row>
				<Col size={12}>
					<div className={className}>
						{children}
					</div>
				</Col>
			</Row>
		</Container>
	</>;
}
