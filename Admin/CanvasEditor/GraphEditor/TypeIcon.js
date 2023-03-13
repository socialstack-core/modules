import { getType, colorAsHsl } from './Types';

export default function TypeIcon(props){
	var {type, className} = props;

	var iconClass = ['type-icon'];
	iconClass.push(className)
	
	var iconColour = [...type.color];
	
	if(iconColour[2] < 0.1){
		iconColour[2] = 0.1;
	}
	
	var lightest = colorAsHsl(iconColour);
	
	if(type.name == 'execute'){
		return <span className={iconClass.join(' ')} title={type.name} onMouseDown={() => props.onClick(0)} onMouseUp={() => props.onClick(1)}>
			<svg version='1.1' xmlns='http://www.w3.org/2000/svg' x='0px' y='0px' viewBox='0 0 200 200' width='40px' height='40px'>
				<g class='cube' transform='translate(60, 47.8)'>
					<path fill={lightest} d='M0,80 0,0 80,40 z' /> 
				</g>
			</svg>
		</span>;
	}
	
	iconColour[2] *= 0.9;
	var mid = colorAsHsl(iconColour);
	iconColour[2] *= 0.9;
	var darkest = colorAsHsl(iconColour);
	
	var cube = <g>
		<path fill={lightest} d='M40,46.2 0,23.1 40,0 80,23.1 z' />
		<path fill={mid} d='M0,23.1 40,46.2 40,92.4 0,69.3 z' />
		<path fill={darkest} d='M40,46.2 80,23.1 80,69.3 40,92.4 z' />
	</g>;
	
	return <span className={iconClass.join(' ')} title={type.name} onMouseDown={() => props.onClick(0)} onMouseUp={() => props.onClick(1)}>
		<svg version='1.1' xmlns='http://www.w3.org/2000/svg' x='0px' y='0px' viewBox='0 0 200 200' width='40px' height='40px'>
			{type.isArray && <>
				<g class='cube' transform='translate(85, 30)'>
					{cube}
				</g>
				<g class='cube' transform='translate(60, 77.8)'>
					{cube}
				</g>
			</>}

			{!type.isArray && <>
				<g class='cube' transform='translate(60, 47.8)'>
					{cube}
				</g>
			</>}
		</svg>
	</span>;
}