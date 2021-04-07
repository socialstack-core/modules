import PageRouter from 'UI/PageRouter';
import webRequest from 'UI/Functions/WebRequest';
import { SessionProvider } from "UI/Session";

/**
 * The root component. It stores state for us, such as the currently logged in user.
 * The instance of this component is available in the global scope as simply 'app'.
 * You can also navigate via app.setState({url: ..}) too, or alternatively, use a relative path anchor tag.
 */
export default function App() {
	
	return <SessionProvider>
		<PageRouter />
	</SessionProvider>;
	
}