import Executor from 'UI/Functions/GraphRuntime/Executor';

export default class If extends Executor {
	
	constructor(){
		super();
	}

	getNumericValue(op) {
		let val = parseInt(op, 10);

		return isNaN(val) ? 0 : val;
	}

	go() {
		// Takes two inputs and outputs a boolean based on the selected operation.
		var props = {};
		
		this.readValue('operanda', props);
		this.readValue('operandb', props);
		this.readValue('operandType', props);
		
		return this.onValuesReady(props, () => {
			var { operanda, operandb, operandType } = props;

			// operations requiring numbers
			switch (operandType) {
				case 'lessThan':
				case 'moreThan':
				case 'lessThanEqual':
				case 'moreThanEqual':
					operanda = getNumericValue(operanda);
					operandb = getNumericValue(operandb);
					break;
			}

			switch (operandType) {
				case 'lessThan':
					return operanda < operandb;

				case 'moreThan':
					return operanda > operandb;

				case 'lessThanEqual':
					return operanda <= operandb;

				case 'moreThanEqual':
					return operanda >= operandb;

				case 'equalTo':
					return operanda == operandb;

				default:
					return false;
			}

		});
	}
	
}