const PROCESS_STEPS = "process-steps";
const PROCESS_STEPS_STEP = PROCESS_STEPS + "__step";

/**
 * process steps component
 * useful to indicate position in a pre-defined set of steps, e.g. a checkout flow
 */
export default function ProcessSteps(props) {
	var { className, steps, activeStep, isComplete } = props;

	if (!steps || !steps.length) {
		return;
	}

	var processStepsClass = [PROCESS_STEPS];

	if (className) {
		processStepsClass.push(className);
	}

	processStepsStyle = {
		"grid-template-columns": "repeat(" + steps.length + ", minmax(0, 1fr))"
	};

	return <>
		<ol className={processStepsClass.join(' ')} style={processStepsStyle}>
			{steps.map((step, index) => {
				var stepClass = [PROCESS_STEPS_STEP];
				var currentStep = index + 1;
				
				if (currentStep < activeStep || isComplete) {
					stepClass.push(PROCESS_STEPS_STEP + "--completed");
				}
				
				if (activeStep == currentStep && !isComplete) {
					stepClass.push(PROCESS_STEPS_STEP + "--current");
				}
				
				return <li key={index} className={stepClass.join(' ')}>
					{step}
				</li>
			})}
		</ol>
	</>;
}

ProcessSteps.propTypes = {
};

ProcessSteps.defaultProps = {
}

ProcessSteps.icon = 'exclamation-circle';