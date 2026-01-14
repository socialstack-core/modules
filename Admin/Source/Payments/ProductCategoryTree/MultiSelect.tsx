import { useEffect, useRef, useState} from "react";
import Input from 'UI/Input'

export type MultiSelectOption = {
    value: string,
    count: number,
    valueId: int
}

export type MultiSelectBoxProps = {
    onSetValue: (value: int, added: boolean) => void;
    defaultText: string;
    value: int[],
    options: MultiSelectOption[]
}

export const MultiSelectBox = (props: MultiSelectBoxProps) => {
    const { value, onSetValue } = props;
    const [isOpen, setIsOpen] = useState(false);
    const ref = useRef<HTMLDivElement>(null);

    useEffect(() => {
        
        const clickListener = (ev: MouseEvent) => {
            const target = ev.target as HTMLDivElement;
            
            if (ref.current && ref.current.contains(target)) {
                // noop.
                return;
            }
            if (ref.current == target) {
                setIsOpen(cur => !cur);
                return;
            }
            setIsOpen(false);
        }
        
        window.addEventListener('click', clickListener);
        
        return () => {
            window.removeEventListener('click', clickListener)
        }
        
    }, []);
    
    return (
        <div 
            className={'multi-select'} 
            ref={ref}
            onClick={() => {
                if (!isOpen) {
                    setIsOpen(true);
                }
            }}
        >
            <div onClick={() => setIsOpen(!isOpen)} className={'multi-select-title'}>
                {props.defaultText}
                {isOpen ? <i className={'fas fa-chevron-up'}/> : <i className={'fas fa-chevron-down'}/>}
            </div>
            {isOpen && (
                <div className={'multi-select-options'}>
                    {(!props.options || props.options.length == 0) && <p>{`No options available`}</p>}
                    {props.options && props.options.map((option: MultiSelectOption) => {
                        return (
                            <li>
                               <Input 
                                    type={'checkbox'} 
                                    label={option.value + ' (' + option.count + ')'}
                                    onChange={(event) => {
                                        if ((event.target as HTMLInputElement).checked) {
                                            // Is it already in there?
                                            if (value.includes(option.valueId)) {
                                                return;
                                            }

                                            onSetValue(option.valueId, true);
                                        } else {
                                            // Unchecked - remove this one.
                                            onSetValue(option.valueId, false);
                                        }
                                    }}
                                    checked={value.includes(option.valueId)}
                               />
                            </li>
                        )
                    })}
                </div>
            )}
        </div>
    )
}