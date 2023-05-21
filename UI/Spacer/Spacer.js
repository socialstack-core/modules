/*
Just an invisible space of a specified height. The default is 20px.
*/
import { useEffect, useRef } from 'react';

export default function Spacer(props) {
	let { height, mobileHeight, hidden } = props;

	if (hidden) {
		return;
	}

	const spacerRef = useRef(null);

	height = parseInt(height, 10);
	mobileHeight = parseInt(mobileHeight, 10);

	if (isNaN(height)) {
		height = 20;
	}

	if (isNaN(mobileHeight)) {
		mobileHeight = height;
	}

	useEffect(() => {

		if (spacerRef.current) {
			spacerRef.current.style.setProperty('--spacer-mobile-height', `${mobileHeight}px`);
		}

	});

	return <div className="spacer-container" data-theme={props['data-theme']}>
		<div className="spacer" style={{ height: `${height}px` }} ref={spacerRef} />
	</div>;
}

Spacer.propTypes = {
	height: 'int',
	mobileHeight: 'int',
	hidden: 'bool'
};

Spacer.icon = 'sort';