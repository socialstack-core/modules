import { useEffect, useState } from "react";
import Alert from "UI/Alert";
import Button from "UI/Button";
import { ApiContent } from "UI/Functions/WebRequest";
import PasswordResetRequestApi from "Api/PasswordResetRequest";
import { User } from "Api/User";
import Loading from "UI/Loading";

export type PasswordResetResponse = {
	url: string
}

type PasswordResetProps = {
	content: User
};

const PasswordResetButton = (props: PasswordResetProps): React.ReactNode => {
	const { content } = props;

	const [url, setUrl] = useState<string|null>(null);
	const [loading, setLoading] = useState<boolean>(false);
	const userId: uint = content?.id;

	const generate = (e) => {
		e.preventDefault();
		e.stopPropagation();
		setLoading(true);

		PasswordResetRequestApi.generate(userId)
			.then((result) => {
				setLoading(false);
				setUrl(
					location.origin + 
					result.url
				)
			})

		return false;
	}

	return (
		<div className="password-reset-button">
			{loading ? (
				<Loading />
			) : <>
					<Button 
						type="button"
						variant="primary"
						onClick={generate}
						disabled={loading}
					>
							{`Generate password reset link`}
					</Button>
					{url && <>
						<Alert type="info">
							{`Send this to the user - when they open it in a browser, they'll be able to set a password and login.`}
						</Alert>
						<p>
							{url}
						</p>
					</>}
				</>
			}
		</div>
	);

}

export default PasswordResetButton;