export type DefaultInputType = React.InputHTMLAttributes<HTMLInputElement>;

// Registering 'text' and 'email' as being available types with a basic textual value.
declare global {
	interface InputPropsRegistry {
		text: DefaultInputType;
		email: DefaultInputType;
		url: DefaultInputType;
		hidden: DefaultInputType;
		number: DefaultInputType;
	}
}

interface DefaultProps {
	type: keyof InputPropsRegistry,
	field: DefaultInputType,
	config: CustomInputTypePropsBase
}

/**
 * The fallback input renderer which is used when a type is not recognised or is just a simple text field.
 * @param props
 * @returns
 */
const Default: React.FC<DefaultProps> = (props) => {
	const { config, field, type } = props;
	const { help, helpFieldId, validationFailure, icon, onInputRef } = config;
	const { className, onChange, ...attribs } = field;
	const id = config.id || field.id;

	const fieldMarkup = (
		<input
			type={type}
			id={id}
			ref={(el) => onInputRef && onInputRef(el as HTMLElement)}
			className={(className || "form-control ui-form-control") + (validationFailure ? ' is-invalid' : '')}
			aria-describedby={help ? helpFieldId : undefined}
			onInput={onChange}
			{...attribs}
		/>
	);

	if (icon) {
		return <div className="input-wrapper">
			{fieldMarkup}
			{icon}
		</div>;
	}

	return fieldMarkup;
};

window.inputTypes['text'] = (props: CustomInputTypeProps<"text">) => <Default type="text" field={props.field} config={props} />;
window.inputTypes['url'] = (props: CustomInputTypeProps<"url">) => <Default type="url" field={props.field} config={props} />;
window.inputTypes['email'] = (props: CustomInputTypeProps<"email">) => <Default type="email" field={props.field} config={props} />;
window.inputTypes['number'] = (props: CustomInputTypeProps<"number">) => <Default type="number" field={props.field} config={props} />;
window.inputTypes['hidden'] = (props: CustomInputTypeProps<"hidden">) => <Default type="hidden" field={props.field} config={props} />;
export default Default;