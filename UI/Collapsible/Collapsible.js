export default function Collapsible (props) {
	var { className, compact, defaultClick } = props;
	var noContent = !props.children;
	var [isOpen, setOpen] = React.useState(noContent ? false : !!props.open);
	
	var expanderLeft = props.expanderLeft;
	var hasInfo = props.info;
	var hasButtons = props.buttons && props.buttons.length;
	
	// NB: include "open" class in addition to [open] attribute as we may be using a polyfill to render this
	var detailsClass = isOpen ? "collapsible open" : "collapsible";
	var summaryClass = noContent ? "btn collapsible-summary no-content" : "btn collapsible-summary";
	var iconClass = expanderLeft || hasButtons ? "collapsible-icon collapsible-icon-left" : "collapsible-icon";

	if (compact) {
		detailsClass += " collapsible--compact";
    }

	if (noContent) {
		iconClass += " invisible";
	}
	
	if (className) {
		detailsClass += " " + className;
	}
	
	return <details className={detailsClass} open={isOpen} onClick={(e) => {
			props.onClick && props.onClick();
			e.preventDefault();
			e.stopPropagation();
			!noContent && setOpen(!isOpen);
		}}>
		<summary className={summaryClass} onClick={defaultClick ? (e) => defaultClick(e) : undefined}>
			{(expanderLeft || hasButtons) &&
				<div className={iconClass}>
					{/* NB: icon classes injected dynamically via CSS */}
					<i className="far fa-fw"></i>
				</div>
			}
			{props.icon && <>
				<i className={"far fa-2x fa-fw collapsible__large-icon " + props.icon}></i>
			</>}
			<h4 className="collapsible-title">
				{props.title}
				{props.subtitle &&
					<small>
						{props.subtitle}
					</small>
				}
			</h4>
			{!expanderLeft && !hasButtons &&
				<div className={iconClass}>
					<i className="far fa-chevron-down"></i>
				</div>
			}
			{(hasInfo || hasButtons) &&
				<div className="buttons">
					{hasInfo && <span className="info">{props.info}</span>}
					{hasButtons &&
						props.buttons.map(button => {
							var variant = button.variant || 'primary';
							var btnClass = 'btn btn-sm btn-outline-' + variant;

							if (button.onClick instanceof Function) {
								return <button type="button" className={btnClass} onClick={button.onClick} title={button.text} disabled={button.disabled}>
									<i className={button.icon}></i>
									<span className={button.showLabel ? '' : 'sr-only'}>
										{button.text}
									</span>
								</button>;
							}
							
							return <a href={button.onClick} className={btnClass} title={button.text} disabled={button.disabled} target={button.target}>
								<i className={button.icon}></i>
								<span className={button.showLabel ? '' : 'sr-only'}>
									{button.text}
								</span>
							</a>;

						})
					}
				</div>
			}
		</summary>
		{!noContent &&
			<div className="collapsible-content">
				{props.children}
			</div>
		}
	</details>;
}