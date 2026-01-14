import { DefaultInputType } from 'UI/Input/Default';
import Icon from 'UI/Icon';
import { useState } from 'react';

type PasswordInputType = DefaultInputType & {
	visible?: boolean;
	noVisibilityButton?: boolean;
}

// Registering 'password' as being available
declare global {
	interface InputPropsRegistry {
		'password': PasswordInputType;
	}
}

const Password: React.FC<CustomInputTypeProps<"password">> = (props) => {
	const { field, onInputRef, helpFieldId, validationFailure } = props;
	const { onChange, className, noVisibilityButton, visible, ...attribs } = field;
	let [pwVisible, setPwVisible] = useState(false);

	if (visible !== undefined) {
		pwVisible = visible;
	}

	return <>
		<div className="input-group">
			<input
				ref={(el: HTMLInputElement) => onInputRef && onInputRef(el)}
				id={field.id}
				className={(className || "form-control ui-form-control") + (validationFailure ? ' is-invalid' : '')}
				aria-describedby={helpFieldId}
				type={pwVisible ? 'text' : 'password'}
				onInput={onChange}
				{...attribs}
			/>
			{!noVisibilityButton && (
				<span className="input-group-text clickable" onClick={() => {
					setPwVisible(!pwVisible);
				}}>
					{pwVisible ? <Icon type='fa-eye-slash' /> : <Icon type='fa-eye' /> }
				</span>
			)}
		</div>
	</>;
}

export default Password;
window.inputTypes['password'] = Password;