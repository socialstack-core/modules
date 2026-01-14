import { useEffect, useState } from "react";
import Loop from "UI/Loop";
import ProductAttributeApi, { ProductAttribute } from "Api/ProductAttribute";
import ProductAttributeValueApi from "Api/ProductAttributeValue";
import Input from "UI/Input";
import Button from "UI/Button";
import Image from "UI/Image";
import Video from "UI/Video";
import Container from "UI/Container";
import SubHeader from "Admin/SubHeader";

type FileChangeEvent = {
    target: {
        value: string;
    };
};

const AttributeValueEditor: React.FC = () => {
    const [attribute, setAttribute] = useState<ProductAttribute>();
    const [inputValue, setInputValue] = useState<string>("");
    const [error, setError] = useState<string | null>(null);
    const [updateNo, setUpdateNo] = useState<number>(0);
    const [existingValues, setExistingValues] = useState<string[]>([]);

    useEffect(() => {
        if (!attribute) {
            // this was added as the useTokens didn't return any valid attribute ID
            // and was erroring about useContext, happy to change this back when it's
            // fixed, but for the meantime.
            // TODO: Await useTokens fix for use in here.

            const segments = location.pathname.split("/").filter(Boolean);
            const id = (() => {
                const i = segments.indexOf("productattribute");
                return i !== -1 && /^\d+$/.test(segments[i + 1]) ? parseInt(segments[i + 1]) : null;
            })();

            if (id) {
                ProductAttributeApi.load(id as uint).then(setAttribute);
            }
        }
    }, [attribute]);

    useEffect(() => {
        if (attribute) {
            ProductAttributeValueApi.list({
                query: "productAttributeId = ?",
                args: [attribute.id],
                sort: {
                    field: "productAttributeId",
                    direction: "DESC",
                },
            }).then((values) => {
                if (values && values.results) {
                    setExistingValues(values.results.map((v) => v.value!));
                }
            });
        }
    }, [updateNo, attribute]);

    const isValidInput = (value: string): boolean => {
        if (!value.trim()) {
            setError("Cannot add empty value");
            return false;
        }
        if (existingValues.includes(value.trim())) {
            setError("Value already exists");
            return false;
        }
        setError(null);
        return true;
    };

    const addValue = async (value: string) => {
        if (!isValidInput(value)) return;
        await ProductAttributeValueApi.create({
            value: value.trim(),
            productAttributeId: attribute!.id,
        });
        setInputValue("");
        setUpdateNo(updateNo + 1);
    };

    const getFileInput = (accept: string) => (
        <Input
            type="file"
            accept={accept}
            key={updateNo}
            onChange={(fileRef: FileChangeEvent) => {
                const filePath = fileRef.target?.value;
                if (!filePath?.trim()) return;
                ProductAttributeValueApi.create({
                    value: filePath,
                    productAttributeId: attribute!.id,
                }).then(() => {
                    setUpdateNo(updateNo + 1);
                });
            }}
        />
    );

    const getInputField = (attrType: number) => {
        const commonProps = {
            value: inputValue,
            onInput: (e: React.FormEvent<HTMLInputElement>) =>
                setInputValue(e.currentTarget.value),
            onKeyDown: (e: React.KeyboardEvent<HTMLInputElement>) => {
                if (e.key === "Enter") {
                    e.preventDefault();
                    addValue(inputValue);
                }
            },
        };

        switch (attrType) {
            case 1:
                return <Input type="number" {...commonProps} />;
            case 2:
                return <Input type="number" step={0.1} {...commonProps} />;
            case 3:
                return <Input type="text" {...commonProps} />;
            case 4:
                return getFileInput("image/*");
            case 5:
                return getFileInput("video/*");
            case 6:
                return getFileInput("*/*");
            case 7:
                return null; // Boolean types are read-only
            default:
                return <Input type="text" {...commonProps} />;
        }
    };

    if (!attribute) {
        return (
            <p>Loading attribute...</p>
        );
    }

    const isFile = [4, 5, 6].includes(attribute.productAttributeType!);

    return (
        <>
            <SubHeader
                title={`Manage values for '${attribute.name}'`}
                breadcrumbs={[
                    { url: "/en-admin/productattribute/", title: "Product Attributes" },
                    { url: `/en-admin/productattribute/${attribute.id}`, title: attribute.name! },
                    { title: "Values" },
                ]}
            />
            <Container>
                <table className="table">
                    <thead>
                    <tr>
                        <th colSpan={2}>Value</th>
                    </tr>
                    </thead>
                    <tbody>
                    {isFile ? (
                        <tr>
                            <td colSpan={2}>{getInputField(attribute.productAttributeType!)}</td>
                        </tr>
                    ) : (
                        <tr>
                            <td>Add value:</td>
                            <td>
                                {getInputField(attribute.productAttributeType!)}
                                <Button
                                    type="button"
                                    onClick={() => addValue(inputValue)}
                                    disabled={!inputValue.trim()}
                                >
                                    Add value
                                </Button>
                            </td>
                        </tr>
                    )}
                    {error && (
                        <tr>
                            <td colSpan={2} style={{ color: "red" }}>
                                {error}
                            </td>
                        </tr>
                    )}
                    <Loop
                        key={updateNo}
                        over={ProductAttributeValueApi}
                        filter={{
                            query: "productAttributeId = ?",
                            args: [attribute.id],
                        }}
                        orNone={() => (
                            <tr>
                                <td colSpan={2}>No values for this attribute</td>
                            </tr>
                        )}
                    >
                        {(value) => (
                            <tr className="attribute-value-value">
                                {isFile ? (
                                    <td>
                                        {attribute.productAttributeType === 4 && (
                                            <Image fileRef={value.value!} />
                                        )}
                                        {attribute.productAttributeType === 5 && (
                                            <Video fileRef={value.value!} />
                                        )}
                                        {attribute.productAttributeType === 6 && (
                                            <>
                                                <i className="fas fa-file" /> {value.value!}
                                            </>
                                        )}
                                        <Button
                                            onClick={() => {
                                                ProductAttributeValueApi.delete(value.id).then(() =>
                                                    setUpdateNo(updateNo + 1)
                                                );
                                            }}
                                        >
                                            <i className="fas fa-trash" /> Remove
                                        </Button>
                                    </td>
                                ) : (
                                    <>
                                        <td>{value.value}</td>
                                        <td>
                                            <Button
                                                onClick={() => {
                                                    ProductAttributeValueApi.delete(value.id).then(() =>
                                                        setUpdateNo(updateNo + 1)
                                                    );
                                                }}
                                            >
                                                <i className="fas fa-trash" /> Remove
                                            </Button>
                                        </td>
                                    </>
                                )}
                            </tr>
                        )}
                    </Loop>
                    </tbody>
                </table>
            </Container>
        </>
    );
};

export default AttributeValueEditor;
