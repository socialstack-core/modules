import { useSession } from 'UI/Session';
import Icon from 'UI/Icon';

/**
 * Props for the impersonation banner component.
 */
interface ImpersonationBannerProps {
};

/**
 * The impersonation banner React component.
 * @param props React props.
 */
const ImpersonationBanner: React.FC<ImpersonationBannerProps> = (props) => {
	const { session } = useSession();
	var { user, realuser } = session;
	var isImpersonating = realuser && (user.id != realuser.id);

	if (!isImpersonating) {
		return;
	}

	return (
		<div className="ui-impersonation-banner">
			<Icon type="fa-mask" />
			{`You are impersonating user`} <strong>{user.firstName || user.username || user.email}</strong>
		</div>
	);
}

export default ImpersonationBanner;