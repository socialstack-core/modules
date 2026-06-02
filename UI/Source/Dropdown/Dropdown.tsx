/* Bootstrap based dropdown
 * ref: https://getbootstrap.com/docs/5.0/components/dropdowns/
 */

import { useState, useEffect, useRef } from "react";

let lastId = 0;

function newId() {
	lastId++;
	return `dropdown${lastId}`;
}

interface DropdownProps {
	/**
	 * Optional custom classname.
	 */
	className?: string,

	/**
	 * Style variant e.g. primary, secondary etc. If not specified primary is assumed.
	 */
	variant?: string,

	/**
	 * A title which appears when hovering the trigger button.
	 */
	title?: string,

	/**
	 * The label of the trigger button.
	 */
	label?: React.ReactNode,

	/**
	 * Optional custom arrow icon.
	 */
	arrow?: React.ReactElement,

	/**
	 * True if the dropdown should use the outline style.
	 */
	isOutline?: boolean,

	/**
	 * True if it should be a large button.
	 */
	isLarge?: boolean,

	/**
	 * True if it should be a small button.
	 */
	isSmall?: boolean,

	/**
	 * optional function to be called when pressing button (dropdown controlled by a separate button)
	 */
	splitCallback?: (event: React.MouseEvent<HTMLButtonElement>) => void,

	/**
	 * True if the menu starts open.
	 */
	initialState?: boolean,

	/**
	 * keeps the dropdown open after selecting an option (disabled by default)
	 */
	stayOpenOnSelection?: boolean,

	/**
	 * alignment of menu with respect to the button (left / right if vertical, top / bottom if horizontal)
	 */
	align?: string,

	/**
	 * which side of the button the menu will appear on (top, bottom, left or right)
	 */
	position?: string,

	/**
	 * tag to use to wrap the dropdown contents (defaults to <ul>)
	 */
	menuTag?: React.ElementType,

	/**
	 * True if the menu is disabled.
	 */
	disabled?: boolean,

	/**
	 * True if the menu should be as compact as possible.
	 */
	noMinWidth?: boolean,

	/**
	 * Items inside the dropdown
	 */
	items: (DropdownItem | null)[]
}

/**
 * Items inside the dropdown menu.
 */
export interface DropdownItem {
	/**
	 * The text on the dropdown entry.
	 */
	text?: string,
	/**
	 * Optional link target. This is only usable if you specify a href.
	 */
	target?: string,
	/**
	 * The button will be a link if this href is specified.
	 */
	href?: string,
	/**
	 * Optional icon to display. Must be an <Icon>
	 */
	icon?: React.ReactNode,
	/**
	 * Optional button click event.
	 * @param event
	 * @returns
	 */
	onClick?: (event: React.MouseEvent<HTMLButtonElement>) => void,
	/**
	 * True if this button is disabled.
	 */
	disabled?: boolean,
	/**
	 * Set to true if you want a divider to display here. Note that all other options are ignored if this is set: only a divider will be rendered as this item.
	 */
	divider?: boolean,

	/**
	 * Set if you want a header to display here. Note that all other options are ignored if this is set: only a heading will be rendered as this item.
	 */
	heading?: React.ReactNode
}

/**
 * Dropdown component
 */
const Dropdown: React.FC<DropdownProps> = (props) => {
	var { className, variant, title, label, arrow, isOutline, isLarge, isSmall,
		splitCallback, initialState,
		stayOpenOnSelection, align, position, disabled, menuTag, noMinWidth, items } = props;
	var dropdownClasses = ['dropdown'];

	if (className) {
		dropdownClasses.push(className);
	}

	if (splitCallback) {
		dropdownClasses.push('dropdown--split');
		dropdownClasses.push('btn-group');
		align = "Right";
	}

	// default to dropping down
	if (!position || position.length == 0) {
		position = "Bottom";
	}
	position = position.toLowerCase();

	if (!initialState) {
		initialState = false;
	}

	if (!align) {
		align = "";
	}

	align = align.toLowerCase();

	const MenuTag = menuTag || 'ul';

	// check for invalid alignment
	// (top/bottom position only supports L/R; left/right position only supports T/B)
	switch (position) {
		case 'top':
		case 'bottom':

			if (align == "top" || align == "bottom") {
				align = "";
			}

			break;

		case 'left':
		case 'right':

			if (align == "left" || align == "right") {
				align = "";
			}

			break;
	}

	// set default alignment
	if (!align || align.length == 0) {

		switch (position) {
			case 'top':
			case 'bottom':
				align = splitCallback ? "Right" : "Left";
				break;

			case 'left':
			case 'right':
				align = "Top";
				break;
		}

	}

	dropdownClasses.push('dropdown--align-' + align);
	dropdownClasses.push('dropdown--position-' + position);

	const [open, setOpen] = useState(initialState);
	const dropdownWrapperRef = useRef<HTMLDivElement>(null);
	const toggleRef = useRef(null);
	const dropdownRef = useRef(null);

	if (!variant) {
		variant = "primary";
	}

	var btnClass = [isOutline ? "btn ui-btn btn-outline-" + variant : "btn ui-btn btn-" + variant];

	if (isSmall) {
		btnClass.push("btn-sm");
	}

	if (isLarge) {
		btnClass.push("btn-lg");
	}

	var btnClassSplit = btnClass;
	btnClassSplit.push("dropdown-toggle");

	if (splitCallback) {
		btnClassSplit.push("dropdown-toggle-split");
	}

	const [dropdownId] = useState(newId());

	if (!label) {
		label = <>
			Dropdown
		</>;
	}

	// default arrow icon
	if (!arrow) {
		arrow = <svg className="dropdown__chevron" xmlns="http://www.w3.org/2000/svg" overflow="visible" viewBox="0 0 58 34">
			<path d="M29 34c-1.1 0-2.1-.4-2.9-1.2l-25-26c-1.5-1.6-1.5-4.1.1-5.7 1.6-1.5 4.1-1.5 5.7.1l22.1 23 22.1-23c1.5-1.6 4.1-1.6 5.7-.1s1.6 4.1.1 5.7l-25 26c-.8.8-1.8 1.2-2.9 1.2z" fill="currentColor" />
		</svg>;
	}

	function closeDropdown() {
		setOpen(false);
	}

	useEffect(() => {
		const handleClick = (event: MouseEvent) => {
			// toggle dropdown
			if (toggleRef && toggleRef.current == event.target) {

				// using if (open) proved unreliable
				if (dropdownRef && dropdownRef.current) {
					closeDropdown();
				} else {
					setOpen(true);
				}

				return;
			}

			// clicked a form control within a dropdown?
			// assume we want to keep the menu open
			var target = event.target as HTMLElement;

			if (!target) {
				return;
			}

			var targetInput = target as HTMLInputElement;

			var isFormControl = targetInput?.type == 'radio' || targetInput?.type == 'checkbox' ||
				target.nodeName.toUpperCase() == 'LABEL' && target.classList.contains("form-check-label");

			// if we want to close even if we selected an item in the dropdown (default)
			if (!stayOpenOnSelection && !isFormControl) {
				closeDropdown();
			} else {

				// only close if we clicked outside of the dropdown
				// (leave dropdown open after making a selection)
				if (dropdownWrapperRef && !dropdownWrapperRef.current?.contains(target)) {
					closeDropdown();
				}
			}
		}

		window.document.addEventListener("click", handleClick);
		
		return () => {
			window.document.removeEventListener("click", handleClick);
		};

	}, [toggleRef, dropdownRef, dropdownWrapperRef]);

	let dropdownMenuClass = ['dropdown-menu'];

	if (noMinWidth) {
		dropdownMenuClass.push('dropdown-menu--no-minwidth');
	}

	return (
		<div className={dropdownClasses.join(' ')} ref={dropdownWrapperRef}>
			{/* TODO: popper support
            <Manager>
                <Reference>
                    {() => (
                        */}

			{/* standard dropdown button */}
			{!splitCallback && (
				<button
					className={btnClassSplit.join(' ')}
					type="button"
					id={dropdownId}
					aria-expanded={open}
					aria-label={title}
					ref={toggleRef}
					disabled={disabled}
					title={title}>

					{position == "left" && <>
						<span className="dropdown__arrow">
							{arrow}
						</span>
						<span className="dropdown__label">
							{label}
						</span>
					</>}

					{position != "left" && <>
						<span className="dropdown__label">
							{label}
						</span>
						<span className="dropdown__arrow">
							{arrow}
						</span>
					</>}
				</button>
			)}

			{/* split dropdown button */}
			{splitCallback && <>
				<button
					className={btnClass.join(' ')}
					type="button"
					id={dropdownId}
					onClick={splitCallback}
					onAuxClick={splitCallback} // for middle mouse button support
					disabled={disabled}
					title={title}>
					<span className="dropdown__label">
						{label}
					</span>
				</button>
				<button
					className={btnClassSplit.join(' ')}
					type="button"
					aria-expanded={open}
					ref={toggleRef}
					disabled={disabled}
					title={`Options`}>
					<span className="dropdown__arrow">
						{arrow}
					</span>
				</button>
			</>}

			{/* dropdown contents */}
			{open && (
				<MenuTag className={dropdownMenuClass.join(' ')} data-source={className} aria-labelledby={dropdownId} ref={dropdownRef}>
					{
						items.map(menuitem => {
							if (!menuitem) {
								return null;
							}
							if (menuitem.divider) {
								return <li>
									<hr className="dropdown-divider" />
								</li>;
							} else if (menuitem.heading) {
								return <li>
									<h6 className="dropdown-header">
										{menuitem.heading}
									</h6>
								</li>;
							}

							if (!menuitem.href) {
								return <li>
									<button type="button" className="btn btn-sm dropdown-item" onClick={menuitem.onClick} title={menuitem.text} disabled={menuitem.disabled}>
										{menuitem.icon} {menuitem.text}
									</button>
								</li>;
							}

							return <li>
								<a href={menuitem.href} className="btn btn-sm dropdown-item" title={menuitem.text} target={menuitem.target}>
									{menuitem.icon} {menuitem.text}
								</a>
							</li>;
						})
					}
				</MenuTag>
			)}
			{/* TODO: popper support
                    )}
                </Reference>
                {open && (
                    <Popper
                        placement="top-start"
                        positionFixed={false}
                        modifiers={modifiers}>
                        {({ ref, style, placement, arrowProps }) => (
                            <ul className="dropdown-menu" aria-labelledby={dropdownId}>
                                {children}
                            </ul>
                        )}
                    </Popper>
                )}
            </Manager>
                */}
		</div>
	);
}

export default Dropdown;

/*
Dropdown.propTypes = {
	stayOpenOnSelection: 'bool',
	align: ['Top', 'Bottom', 'Left', 'Right'],
	position: ['Top', 'Bottom', 'Left', 'Right']
};

Dropdown.defaultProps = {
	stayOpenOnSelection: false,
	align: 'Left',
	position: 'Bottom'
}

Dropdown.icon = 'caret-square-down';
*/