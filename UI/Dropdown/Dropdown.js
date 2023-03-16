/* Bootstrap dropdown
 * ref: https://getbootstrap.com/docs/5.0/components/dropdowns/
  
  params:
  
  label:            override label text / JSX ("dropdown" by default)
  arrow:            override arrow icon (caret SVG by default)
  title:            optional title attribute (useful if no text label visible)
  variant:          Bootstrap variant to use (e.g. primary / secondary / danger / success, etc.
  isOutline:        outlined version (defaults to solid colour)
  isLarge:			renders large size version
  isSmall:			renders small size version
  splitCallback:    optional function to be called when pressing button (dropdown controlled by a separate button)
  stayOpenOnSelection: keeps the dropdown open after selecting an option (disabled by default)
  align:            alignment of menu with respect to the button (left / right if vertical, top / bottom if horizontal)
  position:         which side of the button the menu will appear on (top, bottom, left or right)


  example usage:

    <Dropdown label="Dropdown test" variant="link">
        {
            groups.map(group => {
                return (
                    <li key={group.id}>
                        {href && <>
                            <a href={href} className="dropdown-item">
                                {group.industryName}
                            </a>
                        </>}
                        {!href && <>
                            <button type="button" className="btn dropdown-item" onClick={() => switchCategory(group.id)}>
                                {group.industryName}
                            </button>
                        </>}
				    </li>
                );
            })
                  
        }
    </Dropdown>
 
    to insert a heading in the dropdown, use:
	<li>
		<h6 class="dropdown-header">
			Dropdown header
		</h6>
	</li>
 
    to insert a divider in the dropdown, use:
    <li>
        <hr class="dropdown-divider">
    </li>

 */

import { useState, useEffect, useRef } from "react";
import { tabNext, tabPrev } from "UI/Functions/Tabbable";

// TODO: popper support
//import { Manager, Reference, Popper } from 'UI/Popper';

let lastId = 0;

function newId() {
    lastId++;
    return `dropdown${lastId}`;
}

export default function Dropdown(props) {
    var { className, variant, title, label, arrow, isOutline, isLarge, isSmall, splitCallback, children, stayOpenOnSelection, align, position, disabled } = props;
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

    if (!align) {
        align = "";
    }
    align = align.toLowerCase();

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

	const [open, setOpen] = useState(false);
	const dropdownWrapperRef = useRef(null);
	const toggleRef = useRef(null);
	const dropdownRef = useRef(null);
    var menuItems, firstMenuItem, lastMenuItem;

	if (!variant) {
		variant = "primary";
	}

    var btnClass = [isOutline ? "btn btn-outline-" + variant : "btn btn-" + variant];
	
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
        arrow = <>
            <svg className="dropdown__chevron" xmlns="http://www.w3.org/2000/svg" overflow="visible" viewBox="0 0 58 34">
                <path d="M29 34c-1.1 0-2.1-.4-2.9-1.2l-25-26c-1.5-1.6-1.5-4.1.1-5.7 1.6-1.5 4.1-1.5 5.7.1l22.1 23 22.1-23c1.5-1.6 4.1-1.6 5.7-.1s1.6 4.1.1 5.7l-25 26c-.8.8-1.8 1.2-2.9 1.2z" fill="currentColor"/>
            </svg>
        </>;
    }

    function handleClick(event) {

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
        var target = event.target;
        var isFormControl = target.type == 'radio' || target.type == 'checkbox' ||
            target.nodeName.toUpperCase() == 'LABEL' && target.classList.contains("form-check-label");

        // if we want to close even if we selected an item in the dropdown (default)
        if (!stayOpenOnSelection && !isFormControl) {
            closeDropdown();
        } else {

            // only close if we clicked outside of the dropdown
            // (leave dropdown open after making a selection)
            if (dropdownWrapperRef && !dropdownWrapperRef.current.contains(event.target)) {
                closeDropdown();
            }

        }

	}

    function checkShiftTab(event) {

        // if we're shift-tabbing back from the trigger, close the dropdown
        if (event.keyCode === 9 && event.shiftKey) {
            closeDropdown();
        }

        // cursor down
        if (event.keyCode === 40) {
            event.preventDefault();
            tabNext();
        }

    }

    function checkCursorKey(event) {

        // cursor up
        if (event.keyCode === 38 && event.target != firstMenuItem) {
            event.preventDefault();
            tabPrev();
        }

        // cursor down
        if (event.keyCode === 40 && event.target != lastMenuItem) {
            event.preventDefault();
            tabNext();
        }

    }

    function checkTab(event) {

        // if we're tabbing past this item, close the dropdown
        if (event.keyCode === 9 && !event.shiftKey) {
            closeDropdown();
        }

    }

    function closeDropdown() {

        if (menuItems) {
            menuItems.forEach(menuItem => {
                menuItem.removeEventListener("keydown", checkCursorKey);
            });
        }

        if (lastMenuItem) {
            lastMenuItem.removeEventListener("keydown", checkTab);
        }

        menuItems = undefined;
        firstMenuItem = undefined;
        lastMenuItem = undefined;

        setOpen(false);
    }

	useEffect(() => {

        if (dropdownWrapperRef && dropdownWrapperRef.current) {
            dropdownWrapperRef.current.ownerDocument.addEventListener("click", handleClick);
        }

        if (toggleRef && toggleRef.current) {
            toggleRef.current.addEventListener("keydown", checkShiftTab);
        }

		return () => {
            if (dropdownWrapperRef && dropdownWrapperRef.current) {
                dropdownWrapperRef.current.ownerDocument.removeEventListener("click", handleClick);
            }

            if (toggleRef && toggleRef.current) {
                toggleRef.current.removeEventListener("keydown", checkShiftTab);
            }

        };
        
	}, []);

    useEffect(() => {

        if (!dropdownRef || !dropdownRef.current) {
            return;
        }

        // bind to each dropdown item
        // (checks for up/down cursor key usage)
        menuItems = dropdownRef.current.querySelectorAll("li > .dropdown-item");
        
        menuItems.forEach(menuItem => {
            menuItem.addEventListener("keydown", checkCursorKey);
        });

        if (open && dropdownRef) {
            firstMenuItem = dropdownRef.current.querySelector("li:first-child > .dropdown-item");

            // find last item and bind to tab event
            // (as tabbing past this should close the dropdown)
            lastMenuItem = dropdownRef.current.querySelector("li:last-child > .dropdown-item");

            if (lastMenuItem) {
                lastMenuItem.addEventListener("keydown", checkTab);
            }

        }

    }, [open]);

    return (
        <div title={title} className={dropdownClasses.join(' ')} ref={dropdownWrapperRef} onClick={(e) => {
            // necessary if hosted within UI/Collapsible
            e.stopPropagation();
            handleClick(e);
        }}>
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
                            disabled={disabled}>

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
                            disabled={disabled}>
                            <span className="dropdown__label">
                                {label}
                            </span>
                        </button>
                        <button
                            className={btnClassSplit.join(' ')}
                            type="button"
                            aria-expanded={open}
                            ref={toggleRef}
                            disabled={disabled}>
                            <span className="dropdown__arrow">
                                {arrow}
                            </span>
                        </button>
                    </>}

                        {/* dropdown contents */}
                        {open && (
                            <ul className="dropdown-menu" data-source={className} aria-labelledby={dropdownId} ref={dropdownRef}>
                                {children}
                            </ul>
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

Dropdown.icon='caret-square-down';
