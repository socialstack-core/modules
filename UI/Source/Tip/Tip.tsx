import anchorPositioningPolyfillJs from './static/anchor-positioning-polyfill.js';
import interestForPolyfillJs from './static/interestfor.min.js';
import { lazyLoad } from 'UI/Functions/WebRequest';
import { getUrl } from 'UI/FileRef';
import { useEffect } from 'react';

// @ts-nocheck
/**
 * Base props for the Tip component.
 */
interface TipBaseProps {
	/**
	 * optionally override position-anchor setting (default is based on tip ID).
	 * Note: triggering element should have a style setting of anchor-name matching this value
	 */
	anchor?: string;

	/**
	 * Content for the tooltip.
	 */
	children: React.ReactNode
}

// @ts-nocheck
/**
 * Props for the Tip component.
 */
type TipIdProps =
	| {
		/**
		 * base ID for this tooltip (appends "_tip").
		 * Note: triggering element should have matching interestfor / aria-describedby attributes
		 */
		baseId: string;
		id?: string;
	}
	| {
		baseId?: string;
		/**
		 * optionally provide the ID.
		 * Note: triggering element should have matching interestfor / aria-describedby attributes
		 */
		id: string;
	};

type TipProps = TipBaseProps & TipIdProps;

/**
 * The Tip React component.
 * @param props React props.
 */
const Tip: React.FC<TipProps> = (props) => {
	const { baseId, id, anchor, children } = props;
	const tipId = baseId ? `${baseId}_tip` : id;
	const style = {
		'position-anchor': anchor ? `--${anchor}` : `--${tipId}`
	};

	function isAnchorPositioningSupported() {
		return "anchorName" in document.documentElement.style;
	}

	function supportsInterestFor() {
		return HTMLAnchorElement.prototype.hasOwnProperty('interestForElement')
			|| 'interestfor' in HTMLAnchorElement.prototype
			|| (() => {
				const a = document.createElement('a');
				return 'interestForElement' in a;
			})();
	}

	useEffect(() => {

		// lazy-load polyfills if required
		if (!isAnchorPositioningSupported()) {
			// ref: https://github.com/oddbird/css-anchor-positioning
			/*
			window.ANCHOR_POSITIONING_POLYFILL_OPTIONS = {
				elements: undefined,
				excludeInlineStyles: false,
				roots: [document],
				useAnimationFrame: false,
			};
			*/
			import(getUrl(anchorPositioningPolyfillJs)!);
		}

		if (!supportsInterestFor()) {
			lazyLoad(getUrl(interestForPolyfillJs)!);
		}

	}, []);

	return (
		<div popover="hint" id={tipId} className="ui-tip" style={style}>
			{children}
		</div>
	);
}

export default Tip;