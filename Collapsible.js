export default function Collapsible (props) {
	var isOpen = props && props.open;
	var noContent = !props.children;
	var expanderLeft = props.expanderLeft;
	var hasButtons = props.buttons && props.buttons.length;

	if (noContent) {
		isOpen = false;
	}

	// NB: include "open" class in addition to [open] attribute as we may be using a polyfill to render this
	var detailsClass = isOpen ? "collapsible open" : "collapsible";
	var summaryClass = noContent ? "collapsible-summary no-content" : "collapsible-summary";
	var iconClass = expanderLeft || hasButtons ? "collapsible-icon collapsible-icon-left" : "collapsible-icon";

	if (noContent) {
		iconClass += " invisible";
	}

	return <details className={detailsClass} open={isOpen} onClick={noContent ? (e) => { e.preventDefault(); } : false}>
		<summary className={summaryClass}>
			{(expanderLeft || hasButtons) &&
				<div className={iconClass}>
					{/* NB: icon classes injected dynamically via CSS */}
					<i className="far fa-fw"></i>
				</div>
			}
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
			{hasButtons &&
				<div className="buttons">
					{
						props.buttons.map(button => {
							return <button type="button" className="btn btn-sm btn-outline-primary" onClick={button.onClick} title={button.text}>
								<i className={button.icon}></i>
								<span className="sr-only">{button.text}</span>
							</button>;
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