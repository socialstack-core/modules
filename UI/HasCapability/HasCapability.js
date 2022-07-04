import hasCapability from 'UI/Functions/HasCapability';
import { useSession } from 'UI/Session';

/*
Displays its content only if the named capability is actually granted.
*/
export default props => {
	
	const [granted, setGranted] = React.useState(false);
	const { session } = useSession();
	
	React.useEffect(() => {
		hasCapability(props.called, session).then(grant => {
			grant != granted && setGranted(grant);
		});
	});
	
	var g = granted;
	props.invert && (g=!g);
	return g ? props.children : null;
}