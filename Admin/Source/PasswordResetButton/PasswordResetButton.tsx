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
					<Button className="password-reset-button__generate"
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
						<pre className="password-reset-button__code">
							{url}
							<Button outlined onClick={() => {
								navigator.clipboard
										 .writeText(url)
										 .then(() => {
											 alert(`Copied text to clipboard!`)
										 })
										.catch((err) => {
											alert(`Failed to copy text: ${err}`)
										});
							}}>
								<svg xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" viewBox="0 0 24 24">
									<rect width="14" height="14" x="8" y="8" rx="2" ry="2" />
									<path d="M4 16a2 2 0 01-2-2V4c0-1.1.9-2 2-2h10a2 2 0 012 2" />
								</svg>
								<span className="sr-only">
									{`Copy code to clipboard`}
								</span>
							</Button>
						</pre>
					</>}
				</>
			}
		</div>
	);

}

export default PasswordResetButton;