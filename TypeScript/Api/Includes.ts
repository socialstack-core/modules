/**
 * This file was automatically generated. DO NOT EDIT.
 */

// TYPES
// INCLUDES
export class ApiIncludes {
    private text: string = '';

    constructor(existing: string = '', addition: string = ''){
        this.text = (existing.length != 0) ? existing : '';
        if (addition.length != 0) {
             if (this.text != ''){
                this.text += '.'
             }
             this.text += addition;
        }
    }

    toString(){ return this.text }
}
export class TerminalIncludes extends ApiIncludes {}
export class ApiGlobalIncludes extends ApiIncludes {

    get all(){ 
          return new TerminalIncludes(this.toString(), '*'); 
    }
    get recentdraft() {
        return new TerminalIncludes(this.toString(), 'recentdraft');
    }
    get primaryurl() {
        return new TerminalIncludes(this.toString(), 'primaryurl');
    }
    get breadcrumb() {
        return new TerminalIncludes(this.toString(), 'breadcrumb');
    }
    get calculatedprice() {
        return new TerminalIncludes(this.toString(), 'calculatedprice');
    }
    get cartcontents() {
        return new TerminalIncludes(this.toString(), 'cartcontents');
    }
    get productnotices() {
        return new TerminalIncludes(this.toString(), 'productnotices');
    }
    get emailaddress() {
        return new TerminalIncludes(this.toString(), 'emailaddress');
    }
    get signedref128() {
        return new TerminalIncludes(this.toString(), 'signedref128');
    }
    get signedref256() {
        return new TerminalIncludes(this.toString(), 'signedref256');
    }
    get signedreforiginal() {
        return new TerminalIncludes(this.toString(), 'signedreforiginal');
    }
    get rolepermits() {
        return new RoleIncludes(this.toString(), 'rolepermits');
    }
    get roleexclusions() {
        return new RoleIncludes(this.toString(), 'roleexclusions');
    }
    get composition() {
        return new RoleIncludes(this.toString(), 'composition');
    }
    get tags() {
        return new TagIncludes(this.toString(), 'tags');
    }
    get deliveries() {
        return new DeliveryIncludes(this.toString(), 'deliveries');
    }
    get requiredattributes() {
        return new ProductAttributeIncludes(this.toString(), 'requiredattributes');
    }
    get attributes() {
        return new ProductAttributeValueIncludes(this.toString(), 'attributes');
    }
    get additionalattributes() {
        return new ProductAttributeValueIncludes(this.toString(), 'additionalattributes');
    }
    get productcategories() {
        return new ProductCategoryIncludes(this.toString(), 'productcategories');
    }
    get productquantities() {
        return new ProductQuantityIncludes(this.toString(), 'productquantities');
    }
    get requestedproductquantities() {
        return new ProductQuantityIncludes(this.toString(), 'requestedproductquantities');
    }
    get optionalextras() {
        return new ProductIncludes(this.toString(), 'optionalextras');
    }
    get accessories() {
        return new ProductIncludes(this.toString(), 'accessories');
    }
    get suggestions() {
        return new ProductIncludes(this.toString(), 'suggestions');
    }
    get variants() {
        return new ProductIncludes(this.toString(), 'variants');
    }
    get subscriptions() {
        return new SubscriptionIncludes(this.toString(), 'subscriptions');
    }
    get userpermits() {
        return new UserIncludes(this.toString(), 'userpermits');
    }
    get customcontenttypefields() {
        return new CustomContentTypeFieldIncludes(this.toString(), 'customcontenttypefields');
    }
    get categories() {
        return new CategoryIncludes(this.toString(), 'categories');
    }
    get productimages() {
        return new UploadIncludes(this.toString(), 'productimages');
    }
    get productdownloads() {
        return new UploadIncludes(this.toString(), 'productdownloads');
    }
    get uploads() {
        return new UploadIncludes(this.toString(), 'uploads');
    }
}

export class TemplateIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class TagIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class PublishGroupIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class LocaleIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class TranslationIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class PasswordResetRequestIncludes extends ApiGlobalIncludes {
}

export class PageIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class PermalinkIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class AdminNavMenuItemIncludes extends ApiGlobalIncludes {
}

export class NavMenuIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class NavMenuItemIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class GuestUserIncludes extends ApiGlobalIncludes {
    get deliveryAddress() {
        return new AddressIncludes(this.toString(), 'deliveryaddress');
    }
    get billingAddress() {
        return new AddressIncludes(this.toString(), 'billingaddress');
    }
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class CouponIncludes extends ApiGlobalIncludes {
    get discountAmount() {
        return new PriceIncludes(this.toString(), 'discountamount');
    }
    get minSpendPrice() {
        return new PriceIncludes(this.toString(), 'minspendprice');
    }
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class DeliveryIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class DeliveryOptionIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class PaymentMethodIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class PriceIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class ProductAttributeIncludes extends ApiGlobalIncludes {
    get attributeGroup() {
        return new ProductAttributeGroupIncludes(this.toString(), 'attributegroup');
    }
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class ProductAttributeGroupIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class ProductAttributeValueIncludes extends ApiGlobalIncludes {
    get attribute() {
        return new ProductAttributeIncludes(this.toString(), 'attribute');
    }
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class ProductCategoryIncludes extends ApiGlobalIncludes {
    get productCategory() {
        return new ProductCategoryIncludes(this.toString(), 'productcategory');
    }
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class ProductIncludes extends ApiGlobalIncludes {
    get primaryCategory() {
        return new ProductCategoryIncludes(this.toString(), 'primarycategory');
    }
    get productTemplate() {
        return new ProductTemplateIncludes(this.toString(), 'producttemplate');
    }
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
    get attributeValueFacets() {
          return new AttributeValueFacetIncludes(this.toString(), 'attributevaluefacets');
    }
    get productCategoryFacets() {
          return new ProductCategoryFacetIncludes(this.toString(), 'productcategoryfacets');
    }
}

export class ProductQuantityIncludes extends ApiGlobalIncludes {
    get product() {
        return new ProductIncludes(this.toString(), 'product');
    }
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class ProductTemplateIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class PromotionIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class PurchaseIncludes extends ApiGlobalIncludes {
    get deliveryAddress() {
        return new AddressIncludes(this.toString(), 'deliveryaddress');
    }
    get billingAddress() {
        return new AddressIncludes(this.toString(), 'billingaddress');
    }
    get deliveryOption() {
        return new DeliveryOptionIncludes(this.toString(), 'deliveryoption');
    }
    get guestUser() {
        return new GuestUserIncludes(this.toString(), 'guestuser');
    }
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class ShoppingCartIncludes extends ApiGlobalIncludes {
    get coupon() {
        return new CouponIncludes(this.toString(), 'coupon');
    }
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class SubscriptionIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class SubscriptionUsageIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class PurchaseTokenIncludes extends ApiGlobalIncludes {
    get purchase() {
        return new PurchaseIncludes(this.toString(), 'purchase');
    }
}

export class EmailTemplateIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class UserIncludes extends ApiGlobalIncludes {
    get userRole() {
        return new RoleIncludes(this.toString(), 'userrole');
    }
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class CustomContentTypeIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class CustomContentTypeFieldIncludes extends ApiGlobalIncludes {
    get customContentType() {
        return new CustomContentTypeIncludes(this.toString(), 'customcontenttype');
    }
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class CustomContentTypeSelectOptionIncludes extends ApiGlobalIncludes {
    get customContentTypeField() {
        return new CustomContentTypeFieldIncludes(this.toString(), 'customcontenttypefield');
    }
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class CounterIncludes extends ApiGlobalIncludes {
}

export class ConfigurationIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class ComponentGroupIncludes extends ApiGlobalIncludes {
    get role() {
        return new RoleIncludes(this.toString(), 'role');
    }
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class DomainCertificateIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class CategoryIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class UploadIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class BlogIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class BlogPostIncludes extends ApiGlobalIncludes {
    get blog() {
        return new BlogIncludes(this.toString(), 'blog');
    }
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class ContentFieldAccessRuleIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class RoleIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class AuditEventIncludes extends ApiGlobalIncludes {
    get realUser() {
        return new UserIncludes(this.toString(), 'realuser');
    }
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}

export class AddressIncludes extends ApiGlobalIncludes {
    get creatorUser() {
        return new UserIncludes(this.toString(), 'creatoruser');
    }
}
export class SecondaryIncludes extends ApiGlobalIncludes {
    private secondaryIncludeString: string;
    constructor(existing: string = '', addition: string = ''){
        super('','');
        let src = existing.startsWith('secondary') ? existing : 'secondary';
        this.secondaryIncludeString = src + '.' + (addition.length != 0 ? addition : '');
    }
    toString(){ return this.secondaryIncludeString }
}
export class AttributeValueFacetIncludes extends SecondaryIncludes {
    get value() {
        return new ProductAttributeValueIncludes(this.toString(), 'value');
    }
}
export class ProductCategoryFacetIncludes extends SecondaryIncludes {
    get category() {
        return new ProductCategoryIncludes(this.toString(), 'category');
    }
}
