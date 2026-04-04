import Input from 'UI/Input';

const CheckboxFilter: React.FC<any> = (props) => {
    const { field, onInputRef, noSelection } = props;
    
    const handleRef = (ref: HTMLSelectElement) => {
        if (onInputRef && ref) {
            onInputRef(ref);
            (ref as any).onGetValue = (val: string) => {
                if (val === "true") return true;
                if (val === "false") return false;
                return val;
            };
        }
    };

    const noSelectionText = (field?.noSelection) || noSelection || `Please select...`;

    return (
        <Input 
            {...props} 
            type="select" 
            onInputRef={handleRef}
        >
            <option value="">{noSelectionText}</option>
            <option value="true">{`Yes`}</option>
            <option value="false">{`No`}</option>
        </Input>
    );
};

export default CheckboxFilter;
