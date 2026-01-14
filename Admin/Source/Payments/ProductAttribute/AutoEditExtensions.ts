import AutoFormExtensions from "../../AutoForm/AutoFormExtensions";
import {Content} from "Api/Content";

AutoFormExtensions.addAutoFormButton('ProductAttribute', 'update', {
    label: 'Edit Values',
    className: 'btn btn-primary',
    onClick: (productAttribute: Content<uint>, setPage): void => {
        if (setPage) {
            setPage('/en-admin/productattribute/' + productAttribute.id + '/values');
        }
    }
})