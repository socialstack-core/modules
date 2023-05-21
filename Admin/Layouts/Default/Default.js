import Loop from 'UI/Loop';
import Landing from 'Admin/Layouts/Landing';
import LoginForm from 'Admin/LoginForm';
import Tile from 'Admin/Tile';
import logout from 'UI/Functions/Logout';
import getRef from 'UI/Functions/GetRef';
import { useSession, useRouter, useTheme } from 'UI/Session';
import Dropdown from 'UI/Dropdown';
import { useState } from 'react';
import store from 'UI/Functions/Store';
import Modal from 'UI/Modal';
import Alert from 'UI/Alert';
import webRequest from 'UI/Functions/WebRequest';

export default props => {

    const { session, setSession } = useSession();
    const { pageState, setPage } = useRouter();
    const [menuOpen, setMenuOpen] = useState(false);
    const [showImpersonationOpen, setShowImpersonationOpen] = useState(false);
    const [colourScheme, setColourScheme] = useState(() => {
        var scheme = store.get('colour_scheme') || 'auto';
        updateSchemeVars(scheme);

        return scheme;
    });
    const { adminLogoRef } = useTheme();
    var url = pageState ? pageState.url : '';

    function updateSchemeVars(scheme) {
        var html = window.SERVER ? undefined : document.querySelector("html");

        if (!html) {
            return;
        }

        html.setAttribute("data-theme-variant", scheme)
    }

    function updateScheme(scheme) {
        updateSchemeVars(scheme);
        store.set('colour_scheme', scheme);
        setColourScheme(scheme);
    }

    if (session.loadingUser) {
        return <Landing>
            {`Logging in..`}
        </Landing>;
    }

    var { user, realUser, role } = session;

    var isImpersonating = realUser && (user.id != realUser.id);

    if (!user) {
        // Login page
        return <Landing>
            <Tile>
                <LoginForm noRedirect />
            </Tile>
        </Landing>;
    }

    if (!role || !role.canViewAdmin) {
        return <Landing>
            <Tile>
                <p>
                    {`Hi ${user.firstName} - you'll need to ask an existing admin to grant you permission to login here.`}
                </p>
                <a href={'#'} className="btn btn-secondary" onClick={() => logout('/en-admin/', setSession, setPage)}>
                    {`Logout`}
                </a>
            </Tile>
        </Landing>;
    }

    var dropdownLabelJsx = <>
        {user.fullname || user.username || user.email} <span className="avatar">{user.avatarRef ? getRef(user.avatarRef, { size: 32 }) : null}</span>
    </>;

    function impersonateUser(userId) {
        return webRequest('user/' + userId + '/impersonate').then(response => {
            setSession(response.json);
            window.location = '/';
        });
    }

    function endImpersonation() {
        return webRequest('user/unpersonate').then(response => {
            window.location.reload();
        });
    }

    function renderHeader(allContent) {
        return <>
            <th>{`ID`}</th>
            <th>{`User name`}</th>
            <th>{`Email address`}</th>
            <th>{`Role`}</th>
            <th>&nbsp;</th>
        </>;
    }

    function renderEntry(entry) {

        // don't include current user account
        if (user.id == entry.id) {
            return;
        }

        // disallow role elevation
        if (entry.role < user.role) {
            return;
        }

        return <>
            <td>
                {entry.id}
            </td>
            <td>
                {entry.username}
            </td>
            <td>
                {entry.email}
            </td>
            <td>
                {entry.role}
            </td>
            <td>
                <button type="button" className="btn btn-sm btn-outline-primary"
                    onClick={() => impersonateUser(entry.id)}>
                    {`Select`}
                </button>
            </td>
        </>;
    }

    return (
        <>
            <div className="admin-page__header">
                <div className="admin-page__nav">
                    <button className="btn btn-sm admin-page-menu" type="button" onClick={() => setMenuOpen(!menuOpen)}>
                        <i className={menuOpen ? "fa fa-fw fa-times" : "fa fa-fw fa-bars"} />
                    </button>
                    <a className="admin-page-logo" href='/en-admin/'>
                        {/*getRef(adminLogoRef || logo, { attribs: { height: '38' } })*/}
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 682.027 147.322">
                            <g fill="currentColor">
                                <path d="M184.821 99.201q-5.926 0-11.36-1.552-5.432-1.623-8.748-4.163l3.88-8.608q3.176 2.258 7.48 3.74 4.374 1.41 8.819 1.41 3.387 0 5.433-.634 2.116-.706 3.104-1.905.988-1.2.988-2.752 0-1.976-1.552-3.104-1.553-1.2-4.093-1.905-2.54-.777-5.644-1.412-3.034-.705-6.138-1.693-3.034-.988-5.574-2.54t-4.163-4.09q-1.552-2.54-1.552-6.492 0-4.234 2.258-7.69 2.328-3.529 6.914-5.575 4.657-2.116 11.642-2.116 4.656 0 9.172 1.128 4.515 1.06 7.973 3.246l-3.528 8.678q-3.457-1.975-6.915-2.892-3.457-.988-6.773-.988t-5.433.776q-2.116.776-3.034 2.046-.917 1.2-.917 2.822 0 1.905 1.552 3.105 1.553 1.13 4.093 1.834 2.54.706 5.574 1.411 3.104.706 6.138 1.623 3.104.917 5.644 2.47 2.54 1.552 4.093 4.092 1.622 2.54 1.622 6.42 0 4.163-2.328 7.62-2.328 3.458-6.985 5.574-4.586 2.117-11.642 2.117zm52.776 0q-5.856 0-10.866-1.905-4.938-1.905-8.607-5.362-3.599-3.458-5.645-8.114-1.975-4.657-1.975-10.16 0-5.504 1.975-10.16 2.046-4.657 5.715-8.114 3.67-3.457 8.608-5.362 4.94-1.905 10.724-1.905 5.857 0 10.725 1.905 4.94 1.905 8.537 5.362 3.669 3.457 5.715 8.114 2.046 4.586 2.046 10.16 0 5.503-2.046 10.23-2.046 4.657-5.715 8.114-3.598 3.387-8.537 5.292-4.868 1.905-10.654 1.905zm-.07-9.737q3.315 0 6.067-1.129 2.822-1.129 4.94-3.245 2.116-2.117 3.245-5.01 1.2-2.892 1.2-6.42t-1.2-6.42q-1.13-2.894-3.246-5.01-2.046-2.117-4.868-3.246-2.823-1.129-6.139-1.129-3.316 0-6.138 1.13-2.752 1.128-4.868 3.245-2.117 2.116-3.316 5.01-1.13 2.892-1.13 6.42 0 3.457 1.13 6.42 1.2 2.893 3.245 5.01 2.117 2.116 4.94 3.245 2.821 1.13 6.137 1.13zm59.266 9.737q-5.715 0-10.654-1.835-4.868-1.905-8.466-5.362-3.599-3.457-5.645-8.114-1.975-4.656-1.975-10.23 0-5.574 1.975-10.23 2.046-4.658 5.645-8.115 3.668-3.457 8.537-5.29 4.868-1.906 10.654-1.906 6.42 0 11.57 2.257 5.222 2.188 8.75 6.491l-7.338 6.774q-2.54-2.893-5.645-4.304-3.104-1.482-6.773-1.482-3.457 0-6.35 1.13-2.893 1.128-5.01 3.245-2.116 2.116-3.315 5.01-1.13 2.892-1.13 6.42t1.13 6.42q1.199 2.893 3.316 5.01 2.116 2.116 5.01 3.245 2.892 1.13 6.35 1.13 3.668 0 6.772-1.412 3.105-1.482 5.645-4.445l7.338 6.773q-3.528 4.304-8.75 6.562-5.15 2.258-11.64 2.258zm28.081-.847V48.965h11.43v49.39zm16.651 0l22.014-49.389h11.29l22.082 49.39h-11.994l-18.062-43.604h4.515l-18.132 43.603zm11.007-10.583l3.034-8.678h25.4l3.104 8.678zm49.53 10.583V48.965h11.43v40.076h24.765v9.313zm58.561.847q-5.926 0-11.36-1.552-5.432-1.623-8.748-4.163l3.88-8.608q3.175 2.258 7.48 3.74 4.374 1.41 8.82 1.41 3.385 0 5.432-.634 2.116-.706 3.104-1.905.988-1.2.988-2.752 0-1.976-1.552-3.104-1.553-1.2-4.093-1.905-2.54-.777-5.644-1.412-3.034-.705-6.138-1.693-3.034-.988-5.574-2.54t-4.163-4.092q-1.552-2.54-1.552-6.49 0-4.235 2.257-7.69 2.33-3.53 6.915-5.576 4.657-2.116 11.642-2.116 4.656 0 9.172 1.128 4.515 1.06 7.973 3.246l-3.528 8.678q-3.457-1.975-6.915-2.892-3.457-.988-6.773-.988t-5.433.776q-2.116.776-3.034 2.046-.917 1.2-.917 2.822 0 1.905 1.552 3.105 1.553 1.13 4.093 1.834 2.54.706 5.573 1.411 3.105.706 6.14 1.623 3.103.917 5.643 2.47 2.54 1.552 4.092 4.092 1.623 2.54 1.623 6.42 0 4.163-2.328 7.62-2.328 3.458-6.985 5.574-4.586 2.117-11.642 2.117zm38.312-.847V58.28H483.13v-9.314h43.04v9.314h-15.805v40.075zm24.412 0l22.014-49.389h11.288l22.084 49.39H566.74l-18.062-43.604h4.515L535.06 98.354zm11.007-10.583l3.034-8.678h25.4l3.104 8.678zm72.46 11.43q-5.715 0-10.653-1.835-4.869-1.905-8.467-5.362-3.598-3.457-5.645-8.114-1.975-4.656-1.975-10.23 0-5.574 1.975-10.23 2.047-4.658 5.645-8.115 3.67-3.457 8.537-5.29 4.868-1.906 10.654-1.906 6.42 0 11.571 2.257 5.221 2.188 8.75 6.491l-7.339 6.774q-2.54-2.893-5.644-4.304-3.105-1.482-6.774-1.482-3.457 0-6.35 1.13-2.892 1.128-5.009 3.245-2.117 2.116-3.316 5.01-1.129 2.892-1.129 6.42t1.129 6.42q1.2 2.893 3.316 5.01 2.117 2.116 5.01 3.245 2.892 1.13 6.35 1.13 3.668 0 6.773-1.412 3.104-1.482 5.644-4.445l7.338 6.773q-3.528 4.304-8.749 6.562-5.15 2.258-11.642 2.258zm38.312-12.136l-.635-13.194 23.636-24.906h12.7l-21.307 22.93-6.35 6.774zm-10.23 11.29v-49.39h11.36v49.39zm33.796 0l-17.569-21.52 7.48-8.114 23.424 29.633z" />
                                <path d="M49.026.021C40.973-.362 33.265 4.474 30.362 12.45L1.146 92.72c-3.572 9.815 1.488 20.668 11.303 24.24l80.272 29.217c9.815 3.572 20.667-1.489 24.24-11.304l29.216-80.27c3.572-9.815-1.49-20.668-11.304-24.24L54.603 1.146A18.908 18.908 0 0049.028.021zM72.29 25.103c7.787.023 15.905 1.91 23.303 5.421 2.378 1.13 4.633 2.41 6.487 3.646.928.618 1.74 1.202 2.543 1.92.402.357.804.73 1.295 1.366.49.636 1.46 1.433 1.46 4.127 0 1.827-.317 1.987-.442 2.308-.125.32-.213.495-.293.654-.16.317-.287.538-.43.78a44.832 44.832 0 01-1.01 1.62 154.813 154.813 0 01-2.99 4.418l-7.38 10.553-7.01-4.352c-3.436-2.132-7.46-3.777-10.972-4.625-3.513-.848-6.576-.687-7.33-.436-.638.213-1.133 1.131-1.376 1.54.46.286 1.698 1.565 4.32 2.38 9.128 2.841 13.852 4.312 17.08 5.505 3.23 1.194 5.196 2.334 7.14 3.604 7.225 4.716 11.609 11.912 12.425 20.45 1.38 14.45-7.678 27.004-22.101 31.47-1.728.533-2.98.633-4.732.821-1.752.188-3.745.337-5.748.443-2.003.105-4.003.166-5.77.163-1.764-.003-3.036.025-4.793-.316-9.85-1.912-19.307-6.378-26.94-12.744-.93-.775-2.092-.806-3.31-4.068-.608-1.63-.354-3.872.07-4.944.423-1.072.75-1.466.97-1.808 1.298-2.012 3.05-4.054 5.175-6.665a220.906 220.906 0 012.922-3.524c.412-.484.762-.89 1.088-1.247.163-.178.306-.335.54-.564.118-.114.24-.24.545-.483.153-.122.34-.275.757-.519s.868-.842 3.105-.842c2.563 0 2.32.445 2.598.573.278.128.406.202.52.268.233.13.37.22.52.315.303.192.609.398.969.645.72.494 1.623 1.135 2.597 1.843 7.006 5.096 12.019 6.895 16.543 6.89 1.979 0 2.94-.182 3.288-.3.346-.117.396-.1 1.103-.825.13-.132.102-.11.184-.2.005-.135.01-.113.01-.332 0-1.346.1-.758-.09-.94-.191-.185-1.657-1.115-4.833-2.098-2.307-.714-6.911-2.135-10.22-3.154-7.78-2.396-13.633-4.938-18.073-8.976-4.44-4.039-6.824-9.58-7.624-15.556-1.545-11.544 3.097-22.288 12.589-28.445 6.076-3.942 13.533-5.783 21.32-5.76z" opacity=".569" />
                                <path d="M30.477 12.037a18.44 18.44 0 00-18.44 18.44v83.287a18.44 18.44 0 0018.44 18.44h83.288a18.44 18.44 0 0018.439-18.44V30.477a18.44 18.44 0 00-18.44-18.44zM72.45 31.124a49.86 49.86 0 017.966.707c7.667 1.29 16.189 5.015 20.804 9.094l.644.57-3.48 4.958a4226.3 4226.3 0 00-4.37 6.23l-.886 1.272-1.592-1.062c-2.244-1.499-4.135-2.556-6.236-3.487-6.306-2.792-12.88-3.832-17.215-2.724-1.62.415-2.781 1.066-3.925 2.203-1.615 1.606-2.184 3-2.188 5.352-.003 2.036.478 3.102 2.089 4.63 1.983 1.882 3.524 2.513 12.09 4.947 9.94 2.824 12.848 3.898 16.493 6.093 4.947 2.98 7.87 6.328 9.46 10.831.874 2.477 1.364 6.264 1.218 9.422-.223 4.84-1.233 8.184-3.5 11.581-1.11 1.664-3.732 4.348-5.576 5.71-3.512 2.59-8.187 4.486-12.71 5.152-2.78.41-8.085.63-10.972.453-8.29-.506-17.907-4.267-25.342-9.91-1.774-1.346-4.328-3.506-4.328-3.66 0-.345 2.282-3.323 6.837-8.92 2.438-2.996 3.05-3.68 3.217-3.593.114.06 1.102.768 2.196 1.576 4.592 3.39 6.485 4.58 10.144 6.382 2.83 1.392 4.83 2.107 6.664 2.38 2.92.433 7.86.365 9.789-.135 1.034-.267 2.302-1.06 3.444-2.15 1.71-1.636 2.12-2.587 2.12-4.935 0-3.746-1.503-5.805-5.488-7.52-1.653-.71-3.827-1.408-8.061-2.585-6.791-1.889-10.56-3.136-14.051-4.654-9.843-4.279-13.74-10.223-13.716-20.927.006-2.865.204-4.397.859-6.648 3.05-10.484 13.783-16.715 27.602-16.633z" />
                            </g>
                        </svg>
                    </a>
                    <span class="admin-page__badge admin-page__badge--stage badge bg-warning">
                        <i class="fal fa-fw fa-exclamation-triangle"></i>
                        {`STAGE`}
                    </span>
                    <span class="admin-page__badge admin-page__badge--uat badge bg-warning">
                        <i class="fal fa-fw fa-exclamation-triangle"></i>
                        {`UAT`}
                    </span>
                    <span class="admin-page__badge admin-page__badge--prod badge bg-danger">
                        <i class="fal fa-fw fa-exclamation-triangle"></i>
                        {`PRODUCTION`}
                    </span>
                </div>

                <div className="admin-page-user">
                    {/* colour scheme */}
                    <div className="btn-group btn-group-sm" role="group" aria-label={`Colour scheme options`}>
                        <input type="radio" className="btn-check" name="colour_scheme" id="colour_scheme_light" autocomplete="off"
                            checked={colourScheme == 'light' ? 'checked' : undefined} onChange={() => updateScheme('light')} />
                        <label className="btn btn-outline-primary" htmlFor="colour_scheme_light">
                            <i className="fa fa-fw fa-sun"></i>
                        </label>

                        <input type="radio" className="btn-check" name="colour_scheme" id="colour_scheme_dark" autocomplete="off"
                            checked={colourScheme == 'dark' ? 'checked' : undefined} onChange={() => updateScheme('dark')} />
                        <label className="btn btn-outline-primary" htmlFor="colour_scheme_dark">
                            <i className="fa fa-fw fa-moon"></i>
                        </label>

                        <input type="radio" className="btn-check" name="colour_scheme" id="colour_scheme_auto" autocomplete="off"
                            checked={colourScheme == 'auto' ? 'checked' : undefined} onChange={() => updateScheme('auto')} />
                        <label className="btn btn-outline-primary" htmlFor="colour_scheme_auto">
                            <i className="fa fa-fw fa-adjust"></i>
                        </label>
                    </div>

                    <label htmlFor="user_dropdown" className="user-label">
                        {`Logged in as`}
                    </label>
                    <Dropdown isSmall className="admin-page-logged-user" id="user_dropdown" label={dropdownLabelJsx} variant="link" align="Right">
                        <li>
                            {!isImpersonating && <>
                                <button type="button" className="btn dropdown-item" onClick={() => setShowImpersonationOpen(true)}>
                                    {`Impersonate ...`}
                                </button>
                            </>}
                            {isImpersonating && <>
                                <button type="button" className="btn dropdown-item" onClick={() => endImpersonation()}>
                                    {`End impersonation`}
                                </button>
                            </>}
                        </li>
                        <li>
                            <a href="/" className="btn dropdown-item">
                                {`Return to site`}
                            </a>
                        </li>
                        <li>
                            <hr class="dropdown-divider" />
                        </li>
                        <li>
                            <button type="button" className="btn dropdown-item" onClick={() => logout('/en-admin/', setSession, setPage)}>
                                {`Logout`}
                            </button>
                        </li>
                    </Dropdown>
                </div>
            </div>
            {props.children}
            {menuOpen &&
                <div className="admin-page__menu-open" onClick={() => setMenuOpen(false)}>
                    <div className="admin-drawer">
                        <Loop over='adminnavmenuitem/list' filter={{ sort: { field: 'Title' } }} asUl>
                            {item =>
                                <a href={item.target} className={
                                    item.target == '/en-admin/' ?
                                        (url == item.target ? 'active' : '') :
                                        (url.startsWith(item.target) ? 'active' : '')}>
                                    {getRef(item.iconRef, { className: 'fa-fw' })}
                                    {item.title}
                                </a>
                            }
                        </Loop>
                    </div>
                </div>
            }
            {showImpersonationOpen &&
                <Modal visible isLarge title={`Select a User to Impersonate`} className={"admin-page__impersonation-modal"}
                    onClose={() => setShowImpersonationOpen(false)}>
                <Alert variant="warning">
                    <h3 className={"admin-page__impersonation-title"}>
                        {`Please note`}
                    </h3>
                    {`Selecting a user without administrative privileges will cause admin views to become inaccessible. To return to admin view, click the cog icon and select "End impersonation".`}
                </Alert>
                <Loop asTable over={'user/list'} paged className="table-sm admin-page__impersonation-userlist">
                    {[
                        renderHeader,
                        renderEntry
                    ]}
                </Loop>
                </Modal>
            }
        </>
    );
}