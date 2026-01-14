import logo from './logo.svg'
import burgerIcon from './burger-icon.svg'
import Image from "UI/Image";
import Button from "UI/Button";
import Link from "UI/Link";
import UserCard from "Admin/Header/UserCard";
import Icon from "UI/Icon";
import ImpersonationBanner from "UI/ImpersonationBanner";
import { useSession } from 'UI/Session';

const Logo = () => {
	return (
		<div className={'logo'}>
			<Link href={'/en-admin'}>
				<Image fileRef={logo} />
			</Link>
		</div>
	)
}

type HeaderProps = {
	navOpen: boolean;
	setNavOpen: (navOpen: boolean) => void;
}

const Header: React.FC<HeaderProps> = (props) => {
	const { navOpen, setNavOpen } = props; 

	const { session } = useSession();
	var { user, realuser } = session;
	var isImpersonating = realuser && (user.id != realuser.id);
	
	return <>
		<header className={'admin-top-header'}>
			<div className={'nav-toggle'}>
				<Button onClick={() => setNavOpen(!navOpen)}>
					<Image fileRef={burgerIcon} />
					{`Menu`}
				</Button>
			</div>
			<Logo />
			<Environment />
			<div className={'right-items'}>
				<UserCard />
			</div>
		</header>
		{isImpersonating && <ImpersonationBanner />}
	</>;
}

const Environment = () => {
	return (
		<div className={'environments'}>
			<span className="admin-page__badge admin-page__badge--stage badge bg-warning">
				<Icon type="fa-exclamation-triangle" light />
				{`STAGE`}
			</span>
			<span className="admin-page__badge admin-page__badge--uat badge bg-warning">
				<Icon type="fa-exclamation-triangle" light />
				{`UAT`}
			</span>
			<span className="admin-page__badge admin-page__badge--prod badge bg-danger">
				<Icon type="fa-exclamation-triangle" light />
				{`PRODUCTION`}
			</span>
			<span className="admin-page__badge admin-page__badge--dev badge bg-warning">
				<Icon type="fa-exclamation-triangle" light />
				{`DEV`}
			</span>
		</div>
	)
}

export default Header;