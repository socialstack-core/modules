import { useState } from 'react';

interface TextareaInputType extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
	showLength?: boolean
}

// Registering 'textarea' as being available
declare global {
	interface InputPropsRegistry {
		'textarea': TextareaInputType;
	}
}

const Textarea: React.FC<CustomInputTypeProps<"textarea">> = (props) => {
	const [length, setLength] = useState(0);
	const { field, onInputRef } = props;

	return (<>
		<textarea
			{...field}
			ref={(el: HTMLTextAreaElement) => onInputRef && onInputRef(el as HTMLElement)}
			className={(field.className || "form-control ui-form-control") + (props.validationFailure ? ' is-invalid' : '')}
			onInput={e => {
				var ele = e.target as HTMLTextAreaElement;
				setLength(ele.textLength);
				field.onInput && field.onInput(e);
				field.onChange && field.onChange(e as React.ChangeEvent<HTMLTextAreaElement>);
			}}
		/>
		{field.maxLength && field.showLength && <>
			<div className="textarea-char-count">
				{(length ? length : field.defaultValue ? field.defaultValue.toString().length : 0) + "/" + field.maxLength}
			</div>
		</>}
	</>);

};

export default Textarea;
window.inputTypes['textarea'] = Textarea;
