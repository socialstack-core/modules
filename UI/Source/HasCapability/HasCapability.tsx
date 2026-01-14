import { useEffect, useState } from 'react';
import hasCapability from 'UI/Functions/HasCapability';
import { useSession } from 'UI/Session';

/**
 * Props for the HasCapability component.
 */
interface HasCapabilityProps {

	/**
	 * The name of the capability.
	 */
	called: string

	/**
	 * Displays the content if the capability is not granted. Displays nothing until the cap is loaded.
	 */
	invert?: boolean
}

/**
	Displays its content only if the named capability is actually granted.
*/
const HasCapability: React.FC<React.PropsWithChildren<HasCapabilityProps>> = (props) => {
	
	const [loaded, setLoaded] = useState(false);
	const [granted, setGranted] = useState(false);
	const { session } = useSession();
	
	useEffect(() => {
		hasCapability(props.called, session).then(grant => {
			grant != granted && setGranted(grant);
			setLoaded(true);
		});
	}, [props.called]);

	if (!loaded) {
		return null;
	}

	var g = granted;
	props.invert && (g=!g);
	return g ? props.children : null;
};

export default HasCapability;