const PROCESS_STEPS = "process-steps";
const PROCESS_STEPS_STEP = PROCESS_STEPS + "__step";

/**
 * process steps component
 * useful to indicate position in a pre-defined set of steps, e.g. a checkout flow
 */
export default function ProcessSteps(props) {
	var { steps, activeStep } = props;

	return <>
		<ol className={PROCESS_STEPS}>
			{steps.map((step, index) => {
				var stepClass = [PROCESS_STEPS_STEP];
				var currentStep = index + 1;
				
				if (currentStep < activeStep) {
					stepClass.push(PROCESS_STEPS_STEP + "--completed");
				}
				
				if (activeStep == currentStep) {
					stepClass.push(PROCESS_STEPS_STEP + "--active");
				}
				
				<li key={index} className={stepClass.join(' ')}>
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