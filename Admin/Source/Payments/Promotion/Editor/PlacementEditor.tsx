import { useState, useEffect, useRef } from "react";
import MultiSelect from 'Admin/MultiSelect';
import PageSelector from 'Admin/Page/Selector';
import Button from 'UI/Button';
import { Content } from 'Api/Database';
import Icon from 'UI/Icon';
import { Placement, PlacementType, PlacementTypeLabels } from './types';

function isObject(value: number | object): value is object {
    return typeof value === 'object';
}

function useLoadedValues(contentType: string, ids: (number | Content<uint>)[], includes?: string): Content<uint>[] {
    const [loaded, setLoaded] = useState<Content<uint>[]>(() => ids.filter(isObject) as Content<uint>[]);
    const idsJsonRef = useRef<string>('');

    useEffect(() => {
        const newIdsJson = JSON.stringify(ids);
        
        if (newIdsJson === idsJsonRef.current) {
            return;
        }
        idsJsonRef.current = newIdsJson;

        const numericIds = ids.filter((id): id is number => typeof id === 'number');
        if (numericIds.length === 0) {
            setLoaded(ids.filter(isObject) as Content<uint>[]);
            return;
        }

        const existingObjects = ids.filter(isObject) as Content<uint>[];
        const existingIds = existingObjects.map(o => (o as any).id);
        const missingIds = numericIds.filter(id => !existingIds.includes(id));

        if (missingIds.length === 0) {
            setLoaded(existingObjects);
            return;
        }

        const api = require('Api/' + contentType).default;
        api.list({
            query: "Id=[?]",
            args: [missingIds],
            includes: includes
        }).then((response: any) => {
            const loadedObjects = response.results || [];
            setLoaded([...existingObjects, ...loadedObjects]);
        }).catch(() => {
            setLoaded(existingObjects);
        });
    }, [ids, contentType, includes]);

    return loaded;
}

interface PlacementEditorProps {
    placement: Placement;
    onUpdate: (updates: Partial<Placement>) => void;
    onRemove: () => void;
}

const PlacementEditor: React.FC<PlacementEditorProps> = ({ placement, onUpdate, onRemove }) => {
    const updateConditions = (updates: Partial<Placement['conditions']>) => {
        onUpdate({
            conditions: {
                ...placement.conditions,
                ...updates
            }
        });
    };

    const getTypeIcon = (type: PlacementType): string => {
        switch (type) {
            case 'header':
                return 'fa-bars';
            case 'search':
                return 'fa-search';
            case 'category':
                return 'fa-folder';
            case 'product':
                return 'fa-shopping-cart';
            case 'dashboard':
                return 'fa-tachometer';
            default:
                return 'fa-map-marker';
        }
    };

    return (
        <div className="placement-item">
            <div className="placement-header">
                <div className="placement-type">
                    <i className={`fa ${getTypeIcon(placement.type)} me-2`} />
                    <span className="fw-bold">{PlacementTypeLabels[placement.type]}</span>
                </div>
				<Button sm outlined variant="danger"
					onClick={(e: any) => {
                        e.preventDefault();
                        onRemove();
                    }}
                    title={`Remove placement`}
                >
                    <Icon type="fa-trash" />
                </Button>
            </div>

            <div className="placement-config">
                {placement.type === 'header' && (
                    <HeaderConfig placement={placement} onUpdate={updateConditions} />
                )}

                {placement.type === 'search' && (
                    <SearchConfig placement={placement} onUpdate={updateConditions} />
                )}

                {placement.type === 'category' && (
                    <CategoryConfig placement={placement} onUpdate={updateConditions} />
                )}

                {placement.type === 'product' && (
                    <ProductConfig placement={placement} onUpdate={updateConditions} />
                )}

                {placement.type === 'dashboard' && (
                    <DashboardConfig />
                )}
            </div>
        </div>
    );
};

interface ConfigProps {
    placement: Placement;
    onUpdate: (updates: Partial<Placement['conditions']>) => void;
}

const HeaderConfig: React.FC<ConfigProps> = ({ placement, onUpdate }) => {
    const loadedPages = useLoadedValues('Page', placement.conditions.pages || [], 'title,url');
    
    const pageSelectorValue = loadedPages.map((p: any) => ({
        id: p.id,
        title: p.title || `Page ${p.id}`,
        url: p.url || ''
    }));
    
    const handleChange = (selected: any[]) => {
        onUpdate({ pages: selected });
    };
    
    return (
        <div className="config-section">
            <div className="config-row">
                <label>{`Specific pages (optional)`}</label>
                <p className="text-muted small mb-1">{`Leave empty to show on all pages.`}</p>
                <p className="text-muted small">{`Note: If you select a page with dynamic content (such as a product or category), the promotion will appear on all pages of that type.`}</p>
                <PageSelector
                    value={pageSelectorValue}
                    onChange={handleChange}
                />
            </div>
        </div>
    );
};

const SearchConfig: React.FC<ConfigProps> = ({ placement, onUpdate }) => {
    const loadedCategories = useLoadedValues('ProductCategory', placement.conditions.searchCategories || []);
    const handleChange = (e: any) => {
        try {
            onUpdate({ searchCategories: e.fullValue });
        } catch (err) {
            console.error('SearchConfig onChange error:', err);
        }
    };
    return (
        <div className="config-section">
            <div className="config-row">
                <label>{`Filter by search category (optional)`}</label>
                <p className="text-muted small">{`Show only when searching within these categories`}</p>
                <MultiSelect
                    contentType="ProductCategory"
                    field="name"
                    label="Categories"
                    value={loadedCategories}
                    onChange={handleChange}
                />
            </div>
        </div>
    );
};

const CategoryConfig: React.FC<ConfigProps> = ({ placement, onUpdate }) => {
    const loadedCategories = useLoadedValues('ProductCategory', placement.conditions.categories || []);
    const handleChange = (e: any) => {
        try {
            onUpdate({ categories: e.fullValue });
        } catch (err) {
            console.error('CategoryConfig onChange error:', err);
        }
    };
    return (
        <div className="config-section">
            <div className="config-row">
                <label>{`Specific categories`}</label>
                <p className="text-muted small">{`Leave empty to show on all category pages`}</p>
                <MultiSelect
                    contentType="ProductCategory"
                    field="name"
                    label="Categories"
                    value={loadedCategories}
                    onChange={handleChange}
                />
            </div>
            <div className="config-row">
                <label className="d-flex align-items-center gap-2">
                    <input
                        type="checkbox"
                        checked={placement.conditions.includeChildren ?? true}
                        onChange={(e) => onUpdate({ includeChildren: e.target.checked })}
                    />
                    {`Include child categories`}
                </label>
                <p className="text-muted small">{`When enabled, also shows on subcategory pages`}</p>
            </div>
        </div>
    );
};

const ProductConfig: React.FC<ConfigProps> = ({ placement, onUpdate }) => {
    const loadedProducts = useLoadedValues('Product', placement.conditions.products || []);
    const loadedCategories = useLoadedValues('ProductCategory', placement.conditions.categories || []);
    const handleProductsChange = (e: any) => {
        try {
            onUpdate({ products: e.fullValue });
        } catch (err) {
            console.error('ProductConfig products onChange error:', err);
        }
    };
    const handleCategoriesChange = (e: any) => {
        try {
            onUpdate({ categories: e.fullValue });
        } catch (err) {
            console.error('ProductConfig categories onChange error:', err);
        }
    };
    return (
        <div className="config-section">
            <div className="config-row">
                <label>{`Specific products`}</label>
                <p className="text-muted small">{`Leave empty to show on all products`}</p>
                <MultiSelect
                    contentType="Product"
                    field="name"
                    label="Products"
                    value={loadedProducts}
                    onChange={handleProductsChange}
                />
            </div>
            <div className="config-row">
                <label>{`Filter by category (optional)`}</label>
                <MultiSelect
                    contentType="ProductCategory"
                    field="name"
                    label="Categories"
                    value={loadedCategories}
                    onChange={handleCategoriesChange}
                />
            </div>
            <div className="config-row price-range">
                <label>{`Price range (optional, inc. VAT)`}</label>
                <div className="d-flex align-items-center gap-3">
                    <div className="d-flex align-items-center gap-2">
                        <div className="price-editor">
                            <div className="currency-symbol">£</div>
                            <div className="price-field">
                                <input
                                    type="number"
                                    className="form-control"
                                    placeholder="Min"
                                    min="0"
                                    step="0.01"
                                    value={placement.conditions.minPrice != null ? placement.conditions.minPrice / 100 : ''}
                                    onChange={(e) => onUpdate({ minPrice: e.target.value ? Math.round(Number(e.target.value) * 100) : null })}
                                />
                            </div>
                        </div>
                    </div>
                    <span className="text-muted fw-bold">-</span>
                    <div className="d-flex align-items-center gap-2">
                        <div className="price-editor">
                            <div className="currency-symbol">£</div>
                            <div className="price-field">
                                <input
                                    type="number"
                                    className="form-control"
                                    placeholder="Max"
                                    min="0"
                                    step="0.01"
                                    value={placement.conditions.maxPrice != null ? placement.conditions.maxPrice / 100 : ''}
                                    onChange={(e) => onUpdate({ maxPrice: e.target.value ? Math.round(Number(e.target.value) * 100) : null })}
                                />
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

const DashboardConfig: React.FC = () => {
    return (
        <div className="config-section">
            <p className="text-muted">{`No additional options.`}</p>
        </div>
    );
};

export default PlacementEditor;
