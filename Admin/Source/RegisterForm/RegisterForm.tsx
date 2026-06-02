import UserApi from "Api/User"
import { useState } from "react"
import Alert from "UI/Alert"
import Form from "UI/Form"
import Input from 'UI/Input'
import Spacer from "UI/Spacer"

export type RegisterFormProps = {
	hasUsername: boolean
}

const RegisterForm: React.FC<RegisterFormProps> = (props: RegisterFormProps): React.ReactNode => {
	
	var [failed, setFailed] = useState<PublicError|null>(null);
	const { hasUsername } = props;
	const [success, setSuccess] = useState<boolean | null>();
	var [password, setPassword] = useState('');

	const validatePasswordMatch = (value : string | boolean): PublicError | undefined => {
		if (password != value) {
			return {
				type: 'password/no-match',
				message: `The chosen passwords do not match`
			};
		}
	}

	return (
		<Form
			action={UserApi.create}
			onSuccess={response => {
				setSuccess(true)
			}}
			onValues={v => {
				setFailed(null);
				return v;
			}}
			onFailed={e => {
				setFailed(e);
			}}

			className='register-form'
		>
			<p>
				{`All fields are required`}
			</p>

			<fieldset>
				<Input 
					name="firstName" 
					placeholder={`Your first name`} 
					validate={['Required']} 
					type={'text'}
				/>
			</fieldset>

			<fieldset>
				<Input 
					name="lastName" 
					placeholder={`Your last name`} 
					validate={['Required']} 
					type={'text'}/>
			</fieldset>

			<fieldset>
				<Input 
					name="email" 
					type="email" 
					placeholder={`Email address`} 
					validate={['Required', 'EmailAddress']}/>
			</fieldset>

			{hasUsername && 
				<fieldset>
					<Input 
						name="username" 
						placeholder={`Username`} 
						validate={['Required']} 
						type={'text'}/>
				</fieldset>
			}

			<fieldset>
				<Input
					autoComplete="new-password"							
					type='password'
					name='password'
					label={`New Password`}
					placeholder={`Enter new password`}
					validate={['Required', 'Password']}
					onChange={e => { setPassword((e.target as HTMLInputElement).value); }} />
			</fieldset>

			<fieldset>
				<Input
					autoComplete="new-password"
					type='password'
					name='newPasswordConfirm'
					label={`Confirm Password`}
					placeholder={`Confirm your new password`}
					validate={['Required', validatePasswordMatch]} />
			</fieldset>


			{failed && (
				<Alert variant="danger">
					{failed.message || `Unable to create yout account`}
				</Alert>
			)}

			{success ?
				<Alert type="success">
					{`Account created! Please ask an existing admin to enable it for you.`}
				</Alert>
				:
				<div>
					{`You'll need to ask to be authorised.`}
					<Spacer height={20}/>
					<Input type="submit" label="Create My Account" />
					{`Already got an account?`} <a href="/en-admin/login">{`Login here`}</a>
				</div>
			}
		</Form>
	)

}

export default RegisterForm;