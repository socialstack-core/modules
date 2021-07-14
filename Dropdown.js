/* Bootstrap dropdown
 * ref: https://getbootstrap.com/docs/5.0/components/dropdowns/
  
  params:
  
  label:            override label text / JSX ("dropdown" by default)
  arrow:            override arrow icon (caret SVG by default)
  variant:          Bootstrap variant to use (e.g. primary / secondary / danger / success, etc.
  splitCallback:    optional function to be called when pressing button (dropdown controlled by a separate button)

  example usage:

    <Dropdown label="Dropdown test" variant="link">
        {
            groups.map(group => {
                return (
                    <li key={group.id}>
                        <button type="button" className="btn dropdown-item" onClick={() => switchCategory(group.id)}>
                            {group.industryName}
                        </button>
				    </li>
                );
            })
                  
        }
    </Dropdown>
 
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
	var { className, variant, label, arrow, splitCallback, children, stayOpenOnSelection } = props;
	var dropdownClass = "dropdown " + className + (splitCallback ? " dropdown--split" : "");
	const [open, setOpen] = useState(false);
	const dropdownWrapperRef = useRef(null);
	const toggleRef = useRef(null);
	const dropdownRef = useRef(null);
    var menuItems, firstMenuItem, lastMenuItem;

	if (!variant) {
		variant = "primary";
	}

	var btnClass = "btn btn-" + variant;
	var btnClassSplit = btnClass + " dropdown-toggle";

	var dropdownId = newId();

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

        // if we want to close even if we selected an item in the dropdown (default)
        if (!stayOpenOnSelection) {
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
   		document.addEventListener("click", handleClick);

        if (toggleRef && toggleRef.current) {
            toggleRef.current.addEventListener("keydown", checkShiftTab);
        }

		return () => {
       		document.removeEventListener("click", handleClick);

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
		<div className={dropdownClass} ref={dropdownWrapperRef}>
            {/* TODO: popper support
            <Manager>
                <Reference>
                    {() => (
                        */}

                    {/* standard dropdown button */}
                    {!splitCallback && (
                        <button
                            className={btnClassSplit}
                            type="button"
                            id={dropdownId}
                            aria-expanded={open}
                            ref={toggleRef}>
                            <span className="dropdown__left">
                                {label}
                            </span>
                            <span className="dropdown__right">
                                {arrow}
                            </span>
                        </button>
                    )}

                    {/* split dropdown button */}
                    {splitCallback && <>
                        <button
                            className={btnClass}
                            type="button"
                            id={dropdownId}
                            onClick={splitCallback}>
                            <span className="dropdown__left">
                                {label}
                            </span>
                        </button>
                        <button
                            className={btnClassSplit}
                            type="button"
                            aria-expanded={open}
                            ref={toggleRef}>
                            <span className="dropdown__right">
                                {arrow}
                            </span>
                        </button>
                    </>}

                        {/* dropdown contents */}
                        {open && (
                            <ul className="dropdown-menu" aria-labelledby={dropdownId} ref={dropdownRef}>
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
    stayOpenOnSelection: 'bool'
};

Dropdown.defaultProps = {
    stayOpenOnSelection: false
}

Dropdown.icon='caret-square-down';
