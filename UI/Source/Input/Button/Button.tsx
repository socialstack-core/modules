import UIButton from 'UI/Button';

type ButtonInputType = React.ButtonHTMLAttributes<HTMLButtonElement>;

// Registering 'button' as being available
declare global {
	interface InputPropsRegistry {
		'button': ButtonInputType;
		'submit': ButtonInputType;
		'reset': ButtonInputType;
	}
}

type ButtonProps = {
	type: 'button' | 'submit' | 'reset',
	field: ButtonInputType,
	config: CustomInputTypePropsBase
};

const Button: React.FC<ButtonProps> = (props) => {
	const { config, field, type } = props;
	const { icon, label } = config;
	const { className, children, ...attribs } = field;

	var defaultLabel = `Button`;

	switch (type) {

		case 'submit':
			defaultLabel = `Submit`;
			break;

		case 'reset':
			defaultLabel = `Reset`;
			break;

	}

	return (
		<UIButton type={type} className={className} {...attribs}>
			{icon && <>
				{icon}
				<span>
					{label || children || defaultLabel}
				</span>
			</>}

			{!icon && <>
				{label || children || defaultLabel}
			</>}
		</UIButton>
	);
	
}

export default Button;
window.inputTypes['button'] = (props: CustomInputTypeProps<"button">) => <Button type="button" field={props.field} config={props} />;
window.inputTypes['submit'] = (props: CustomInputTypeProps<"submit">) => <Button type="submit" field={props.field} config={props} />;
window.inputTypes['reset'] = (props: CustomInputTypeProps<"reset">) => <Button type="reset" field={props.field} config={props} />;