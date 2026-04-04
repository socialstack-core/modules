import logo from './logo.svg'
import logoSmall from './logo-small.svg'
import Image from "UI/Image";
import Button from "UI/Button";
import Link from "UI/Link";
import Account from "Admin/Header/Account";
import Icon from "UI/Icon";
import ImpersonationBanner from "UI/ImpersonationBanner";
import Popover from "UI/Popover";
import Menu from "Admin/Header/Menu";
import { useSession } from 'UI/Session';

type HeaderProps = {
}

const Header: React.FC<HeaderProps> = (props) => {
	const { session } = useSession();
	var { user, realuser } = session;
	var isImpersonating = realuser && (user.id != realuser.id);

	return <>
		<header className="admin-page__header">
			<Button className="admin-page__header-nav" popoverTarget="admin_menu">
				<svg xmlns="http://www.w3.org/2000/svg" shape-rendering="geometricPrecision" fill="none"
					stroke="currentColor" stroke-width="2" stroke-linecap="round" viewBox="0 0 24 24">
					<line x1="4" y1="12" x2="20" y2="12" />
					<line x1="4" y1="12" x2="20" y2="12" />
					<line x1="4" y1="12" x2="20" y2="12" />
				</svg>
				{`Menu`}
			</Button>
			<Popover alignment="left" blurBackground={true} underHeader={true} method="auto" closeOnInteractiveClick="when-bg-disabled" bgDisabledWidth={1024}
				className="admin-page__menu" id="admin_menu" disableScrollLock={true}>
				<Menu />
			</Popover>

			{/* logo */}
			<Link href={'/en-admin'}>
				<Image className="admin-page__header-logo" fileRef={logo} plain />
				<Image className="admin-page__header-logo--small" fileRef={logoSmall} plain />
			</Link>

			<div className="admin-page__header-actions">
				<Environment />
				<Account />
			</div>

		</header>
		{isImpersonating && <ImpersonationBanner />}
	</>;
}

const Environment = () => {
	return (
		<div className="admin-page__header-env">
			<span className="admin-page__badge admin-page__badge--stage">
				<Icon type="fa-exclamation-triangle" light />
				{`STAGE`}
			</span>
			<span className="admin-page__badge admin-page__badge--uat">
				<Icon type="fa-exclamation-triangle" light />
				{`UAT`}
			</span>
			<span className="admin-page__badge admin-page__badge--prod">
				<Icon type="fa-exclamation-triangle" light />
				{`PRODUCTION`}
			</span>
			<span className="admin-page__badge admin-page__badge--dev">
				<Icon type="fa-cog" light />
				{`DEV`}
			</span>
		</div>
	)
}

export default Header;