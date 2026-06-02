import { useSession } from 'UI/Session';
import { useTokens } from 'UI/Token';
import Button from 'UI/Button';

/**
 * Props for the link component.
 */
interface LinkProps extends React.AnchorHTMLAttributes<HTMLAnchorElement> {
	/**
	 * True if the link is disabled.
	 */
	disabled?: boolean,

	/**
	 * optional additional class name(s)
	 */
	className?: string,

	/**
	 * True if the link should be the extra small style.
	 */
	xs?: boolean,

	/**
	 * True if the link should be the small style.
	 */
	sm?: boolean,

	/**
	 * True if the link should be the regular style.
	 */
	md?: boolean,

	/**
	 * True if the link should be the large style.
	 */
	lg?: boolean,

	/**
	 * True if the link should be the extra large style.
	 */
	xl?: boolean,

	/**
	 * True if the link should prevent wrapping text inside it.
	 */
	noWrap?: boolean,

	/**
	 * True if the link should be the outlined style.
	 */
	outlined?: boolean,

	/**
	 * The style variant, "primary", "secondary" etc. no style (i.e. basic link) assumed.
	 */
	variant?: string,

	/**
	 * True if the link should be opened within a new tab.
	 */
	external?: boolean,
}

const Link: React.FC<React.PropsWithChildren<LinkProps>> = ({
	children, className, href, variant, external,
	disabled, outlined, noWrap, xs, sm, md, lg, xl, ...attribs
}) => {
	const { session } = useSession();
	const { locale } = session;

	var children = children;
	var url = useTokens(href || '');

	if (url) {
		// if url contains :// it must be as-is (which happens anyway).
		if (url[0] == '/') {
			if (url.length > 1 && url[1] == '/') {
				// as-is
			} else {
				var prefix = window.urlPrefix || '';

				if (prefix) {
					if (prefix[0] != '/') {
						// must return an absolute url
						prefix = '/' + prefix;
					}

					if (prefix[prefix.length - 1] == "/") {
						url = url.substring(1);
					}

					let disablePrefix = "/" + locale?.code?.toLowerCase() == prefix && locale?.isRedirected && locale?.permanentRedirect;

					if (!disablePrefix) {
						url = prefix + url;
					}
				}
			}
		}

		if (url != "/") {

			// strip any trailing slashes
			while (url.endsWith("/")) {
				url = url.slice(0, -1);
			}

		}

	}

	var classes = className ? className.split(" ") : [];
	var styleAsButton = (variant && variant.length) || outlined;

	// sizing
	if (styleAsButton) {
		return <>
			{/* @ts-ignore */}
			<Button className={className} href={href} variant={variant} disabled={disabled}
				outlined={outlined} allowWrap={false} xs={xs} sm={sm} md={md} lg={lg} xl={xl} {...attribs}>
				{children}
			</Button>
		</>;
	}

	if (xs) {
		classes.unshift("ui-link--xs");
	}

	if (sm) {
		classes.unshift("ui-link--sm");
	}

	if (md) {
		classes.unshift("ui-link--md");
	}

	if (lg) {
		classes.unshift("ui-link--lg");
	}

	if (xl) {
		classes.unshift("ui-link--xl");
	}

	if (noWrap) {
		classes.unshift("ui-link--nowrap");
	}

	classes.unshift("ui-link");

	var linkClass = classes.join(" ");

	return <a href={url} inert={disabled ? true : undefined} className={linkClass}
		target={external ? "_blank" : undefined}
		rel={external ? "noopener noreferrer" : undefined}
		{...attribs}>
		{children}
	</a>;
}

export default Link;
