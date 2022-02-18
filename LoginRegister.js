import RegisterForm from 'UI/RegisterForm';
import LoginForm from 'UI/LoginForm';
import { useSession, useRouter } from 'UI/Session';
import Row from 'UI/Row';
import Column from 'UI/Column';
import { useState, useEffect } from 'react';

export default function LoginRegister(props) {
	const { setPage } = useRouter();
	const [view, setView] = useState();

	const { session } = useSession();
	const user = session.user;

	useEffect(() => {
		if (user && user.Role != 3 && user.Role != 4) {
			setPage("/");
		}
	});
	
	return (
		<div className="login-register">
			{!view 
				? <div className="login-register-buttons">
					<Row>
						<Column>
							<button className="btn btn-primary" onClick={e => setView("login")}>
								Login
							</button>
						</Column>
						<Column>
							<button className="btn btn-primary" onClick={e => setView("register")}>
								Register
							</button>
						</Column>
					</Row>
				</div>
				: <div className="back-button">
					<button className="btn btn-primary" onClick={e => setView(null)}>
						Back
					</button>
				</div>
			}
			{view === "login" &&
				<div className="login">
					<LoginForm noRegister />
				</div>
			}
			{view === "register" &&
				<div className="register">
					<RegisterForm noLogin />
				</div>
			}
		</div>
	);
}


LoginRegister.propTypes = {

};

// use defaultProps to define default values, if required
LoginRegister.defaultProps = {

}

// icon used to represent component when adding to a page via /en-admin/page
// see https://fontawesome.com/icons?d=gallery for available classnames
// NB: exclude the "fa-" prefix
LoginRegister.icon='sign-in'; // fontawesome icon
