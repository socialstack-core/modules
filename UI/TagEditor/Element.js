import React, { useLayoutEffect, useRef, useState } from 'react';
import Cancel from './Cancel';

export default function Element(props) {
    const [focus, setFocus] = useState(false);
    
    const onclick = (e) => {
        e.preventDefault();
        props.onRemove(props.index, focus);
    };
    
    const ref = useRef(null);
    
    useLayoutEffect(() => {
        if (ref.current && props.focus) {
            ref.current.focus();
        }
    }, [props.focus]);

    const onFocus = () => {
        setFocus(true);
    }

    const onBlur = () => {
        setFocus(false);
    }

    const onKeyDown = (e) => {
        const { key } = e;
        e.preventDefault();
        if (key === 'Backspace' || key === 'Delete') {
            props.onRemove(props.index, props.focus);
        }
        else if (key === 'ArrowLeft') {
            props.onSelectedIndex(props.index - 1);
        }
        else if (key === 'ArrowRight') {
            props.onSelectedIndex(props.index + 1);
        }
    };

    return (
	
	<>
		<div data-testid='tag-element'
            ref={ref}
            tabIndex={-1}
            className='badge bg-primary bg-gradient me-1 pe-1 justify-content-between'
            onKeyDown={ onKeyDown }
            onFocus={ onFocus }      
            onBlur={ onBlur }
            >

            {props.value}

            {/* dont use button here :=)*/}
            <span 
                data-testid='tag-clean-element'
                aria-label='remove path fragment'
                tabIndex={-1}
                className='border-0 bg-transparent ps-1 pe-0 cancel-wrapper'
                style={{ outline: 0 }}
                onClick={onclick}>
            <Cancel/>
            </span>
        </div>
	</>
	)
};
