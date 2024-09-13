import Dropdown from 'UI/Dropdown';
import Canvas from 'UI/Canvas';
import getRef from 'UI/Functions/GetRef';
import { useRef, useState, useEffect } from 'react';

export default function Collapsible(props) {
	var { className, compact, noMinWidth, defaultClick, alwaysOpen, hidden, dropdownTitle } = props;
	var noContent = props.noContent || !props.children;
	var [isOpen, setOpen] = useState(noContent ? false : !!props.open);
	const detailsRef = useRef();

	var expanderLeft = props.expanderLeft;
	var hasInfo = props.info;
	var hasJsx = props.jsx;
	var hasJson = props.json;
	var hasButtons = props.buttons && props.buttons.length;

	// NB: include "open" class in addition to [open] attribute as we may be using a polyfill to render this
	var detailsClass = isOpen ? "collapsible open" : "collapsible";
	var summaryClass = noContent ? "btn collapsible-summary no-content" : "btn collapsible-summary";
	var iconClass = expanderLeft || hasButtons ? "collapsible-icon collapsible-icon-left" : "collapsible-icon";

	if (compact) {
		detailsClass += " collapsible--compact";
	}

	if (hidden) {
		detailsClass += " collapsible--hidden";
	}

	if (noContent) {
		iconClass += " invisible";
	}

	if (className) {
		detailsClass += " " + className;
	}

	let largeIcon;

	// check: is the icon prop given a reference (rather than a string)?
	if (props.icon) {
		let parsedRef = getRef.parse(props.icon);

		if (parsedRef.fileType == 'svg') {
			largeIcon = getRef(props.icon);
		} else {
			largeIcon = getRef.isIcon(props.icon) ?
				getRef(props.icon, { className: 'fa-2x fa-fw collapsible__large-icon' }) :
				<i className={"far fa-2x fa-fw collapsible__large-icon " + props.icon}></i>;
		}

	}

	function toggleEvent(e) {

		if (typeof props.onOpen == 'function' && e.newState == "open") {
			props.onOpen(e);
		}

		if (typeof props.onClose == 'function' && e.newState == "closed") {
			props.onClose(e);
		}

	}

	useEffect(() => {

		if (detailsRef && detailsRef.current) {
			detailsRef.current.addEventListener("toggle", toggleEvent);
		}

		return () => {

			if (detailsRef && detailsRef.current) {
				detailsRef.current.removeEventListener("toggle", toggleEvent);
			}

		};


	}, []);

	return <details className={detailsClass} open={isOpen} ref={detailsRef} onClick={(e) => {
		if (e.defaultPrevented || (e.target.nodeName != 'SUMMARY' && e.target.nodeName != 'DETAILS')) {
			return;
		}

		if (!alwaysOpen) {
			props.onClick && props.onClick();
			e.preventDefault();
			e.stopPropagation();
			!noContent && setOpen(!isOpen);
		} else {
			e.preventDefault();
			e.stopPropagation();
		}

	}}>
		<summary className={summaryClass} onClick={defaultClick && !alwaysOpen ? (e) => {
			if (e.defaultPrevented || (e.target.nodeName != 'SUMMARY' && e.target.nodeName != 'DETAILS')) {
				return;
			}

			defaultClick(e);
		} : undefined}>
			{(expanderLeft || hasButtons) && !alwaysOpen &&
				<div className={iconClass}>
					{/* NB: icon classes injected dynamically via CSS */}
					<i className="far fa-fw"></i>
				</div>
			}
			{largeIcon}
			<h4 className="collapsible-title">
				{!hasJson && props.title}
				{hasJson && <Canvas>{props.json}</Canvas>}
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
			{(hasInfo || hasJsx || hasButtons) &&
				<div className="buttons">
					{hasInfo && <span className="info">{props.info}</span>}
					{hasJsx && <span className="jsx">
						{props.jsx}
					</span>}
					{hasButtons &&
						props.buttons.map(button => {
							var variant = button.variant || 'primary';
							var btnClass = 'btn btn-sm btn-outline-' + variant;

							// split button
							if (button.children && button.children.length) {
								var dropdownJsx = <>
									<i className={button.icon}></i>
									<span className={button.showLabel ? '' : 'sr-only'}>
										{button.text}
									</span>
								</>;

								return <>
									<Dropdown label={dropdownJsx} variant={'outline-' + variant} isSmall noMinWidth={noMinWidth}
										disabled={button.disabled} splitCallback={button.onClick} title={dropdownTitle}>
										{
											button.children.map(menuitem => {

												if (menuitem.separator || menuitem.divider) {
													return <li>
														<hr class="dropdown-divider" />
													</li>;
												}

												if (menuitem.onClick instanceof Function) {
													return <li>
														<button type="button" className="btn btn-sm dropdown-item" onClick={menuitem.onClick} title={menuitem.text} disabled={menuitem.disabled}>
															<i className={menuitem.icon}></i> {menuitem.text}
														</button>
													</li>;
												}

												return <li>
													<a href={menuitem.onClick} className="btn btn-sm dropdown-item" title={menuitem.text} disabled={menuitem.disabled} target={menuitem.target}>
														<i className={menuitem.icon}></i> {menuitem.text}
													</a>
												</li>;
											})
										}
									</Dropdown>
								</>;

							}

							// standard button
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