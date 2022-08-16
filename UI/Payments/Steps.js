const StepsEnum = Object.freeze({
	SUBSCRIPTION_TYPE: `Subscription type`,
	PAYMENT_METHOD: `Payment method`,
	FINALISE_SUBSCRIPTION: `Finalise subscription`
});

const getStepsEnum = () => {
	return StepsEnum;
}

const getSteps = () => {
	var processSteps = [];

	for (const [key, value] of Object.entries(StepsEnum)) {
		processSteps.push(value);
	}

	return processSteps;
}

const getStepIndex = (requestedStep) => {
	return Object.entries(StepsEnum).findIndex(step => step[1] == requestedStep) + 1;
}

export {
	getStepsEnum,
	getSteps,
	getStepIndex
}
