import React, { useEffect, useLayoutEffect, useRef, useState } from 'react';
import Element from './Element';

export default function TagEditor(props) {
    var { placeholder, values, onTags, name, forceFocus} = props;
    const [terms, setTerms] = useState([]);
    const [value, setValue] = useState('');
    const [focusIndex, setFocusIndex] = useState(-1);
    const inputRef = useRef(null);
    
    const forceInputFocus = () => {
        if (forceFocus && inputRef.current && focusIndex === -1) {
             inputRef.current.focus();
        }
    };

    useLayoutEffect(() => {
        if (terms.length === 0) {
            setFocusIndex(-1);
        }
        onTags({ values: terms, name: name });
    }, [terms.length]);

    useEffect(() => {
        setTerms(values);
    }, [values]);

    useEffect(() => {
        forceInputFocus();
    }, [focusIndex, inputRef.current]);

    const onchange = (event) => {
        setValue(event.currentTarget.value);
    };

    const onkeydown = (event) => {
        const { key } = event;
        const currentValue = value.trim();
        if (key === 'Tab' && currentValue !== '') {
            event.preventDefault();
            var cleanTag = currentValue.replace(',', '').toLowerCase();
            if(! terms.includes(cleanTag)) {
                setTerms([...terms, cleanTag]);
            }
            setValue('');
            setFocusIndex(-1);
        }
    };

    const onkeyup = (event) => {
        const { key } = event;
        const currentValue = value.trim();
        const valueLength = currentValue.length;
        const currentTarget = event.currentTarget.selectionEnd || 0;
        const isEndOfText = currentTarget > valueLength;
        const isStartOfText = currentTarget === 0;
        const isPossibletermsMove = terms.length > 0;
        const isPossibleAddKeys = key === 'Enter' || key === ' ' || key === 'Tab' || key === ',';
        
        if (isPossibleAddKeys && currentValue !== '') {
            event.preventDefault();
            var cleanTag = currentValue.replace(',', '').toLowerCase();
            if(! terms.includes(cleanTag)) {
                setTerms([...terms, cleanTag]);
            }
            setValue('');
            setFocusIndex(-1);
        }
        else if (isStartOfText &&
            (key === 'Backspace' || key === 'ArrowLeft') && isPossibletermsMove) {
            event.preventDefault();
            setFocusIndex(terms.length - 1);
        }
        else if (isEndOfText && key === 'ArrowRight' && isPossibletermsMove) {
            event.preventDefault();
            setFocusIndex(0);
        }
    };
    const handleRemove = (index, focus) => {
        setTerms(terms.filter((_, i) => i !== index));
        if (focus) {
            setFocusIndex(Math.max(focusIndex - 1, 0));
        }
        else {
            forceInputFocus();
        }
    };
    const setSelectedIndex = (index) => {
        if (index < terms.length && index > -1) {
            setFocusIndex(index);
        }
        else {
            setFocusIndex(-1);
        }
    };

    return (
        
        <div className='form-control h-auto d-inline-flex flex-wrap'>
        {terms.map((item,index) => { 

                const focus = focusIndex === index;

                return <Element key={`{item}{index}`}  
                  value={item}
                  index={index}  
                  onRemove={handleRemove}
                  focus={focus}
                  onSelectedIndex={setSelectedIndex}
                  className={'badge bg-secondary bg-gradient me-1 pe-1 justify-content-between'}
                  >
                  </Element>;
        })}

        <input 
            data-testid='input-tags' 
            ref={inputRef}  
            type='text'
            className='border-0 w-auto flex-fill input-tags'
            placeholder={placeholder}
            aria-label={placeholder}
            value={value}
            onInput={onchange}
            onKeyUp={onkeyup}
            onKeyDown={onkeydown}
            name={name}/>
        </div>
    )
};
