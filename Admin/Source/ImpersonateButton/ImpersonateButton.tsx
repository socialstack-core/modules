import { useEffect, useState } from "react";
import Alert from "UI/Alert";
import Button from "UI/Button";
import { ApiContent } from "UI/Functions/WebRequest";
import userApi from "Api/User";
import { User } from "Api/User";
import Loading from "UI/Loading";
import {useRouter} from "UI/Router";
import {useSession} from "UI/Session";

type ImpersonateProps = {
	content: User
};

const ImpersonateButton = (props: ImpersonateProps): React.ReactNode => {
	const { content } = props;
	const { setPage } = useRouter();
	const { session, setSession } = useSession();
	const [loading, setLoading] = useState<boolean>(false);
	const userId: uint = content?.id;

	const impersonateUser = (e) => {
		e.preventDefault();
		e.stopPropagation();
		setLoading(true);

		userApi.impersonate(setSession, userId)
			.then((result) => {
				setLoading(false);
				setPage(
					location.origin
				)
			})

		return false;
	}

	return (
		<div className="admin-impersonate-button">
			{loading ? (
				<Loading />
			) : <>
					<Button 
						type="button"
						variant="primary"
						onClick={impersonateUser}
						disabled={loading}
					>
							{`Impersonate`}
					</Button>
				</>
			}
		</div>
	);

}

export default ImpersonateButton;