declare global {
    /**
     * Represents a socialstack file ref as a string.
     * Use this type whenever you need to indicate that a string is specifically a ref.
     */
    type FileRef = string;
    /**
     * Represents a bit of HTML as a string
     */
    type HtmlString = string;
    /**
     * Represents a hex color as a string.
     */
    type HexColor = string;

    /**
     * Unsigned byte.
     */
    type byte = int;

    /**
     * Signed byte
     */
    type sbyte = int;

    /**
     * Unsigned integer (4 bytes).
     */
    type uint = int;

    /**
     * Signed integer (4 bytes).
     */
    type int = number & { __type: "int" };

    /**
     * Unsigned integer (8 bytes).
     */
    type ulong = int;

    /**
     * Signed integer (8 bytes).
     */
    type long = int;

    /**
     * A floating point number
     */
    type double = number;

    /**
     * A floating point number (double)
     */
    type float = number;

    /**
     * Convertible to a date with isoConvert from DateTools.
     */
    type Dateish = number | Date | string;

    interface PublicError {
        /**
         * Textual message to display.
         */
        message: string,

        /**
         * Error type.
         */
        type: string,

        /**
         * Any other detail if available.
         */
        detail?: any
    }

    /**
     * Public configuration objects from the API.
     */
    interface Config {}

    /**
     * Internal site configuration.
     */
    var __cfg: Record<string, Config>;

    /**
     * URL where static content originates from.
     */
    var staticContentSource: string;

    /**
     * URL where upload content originates from.
     */
    var contentSource: string;

    /**
     * Optionally prefix all links with this.
     */
    var urlPrefix: string | undefined;

    /**
     * Global Session Init data. Originates from the server serialising the user's context.
     */
    var gsInit: SessionResponse | undefined;

    /**
     * True if this is executing on the serverside. Only use if absolutely necessary
     * as it will cause odd behaviour (mainly React not hydrating correctly) with the SSR if this is used too much.
     * Most of the time you should do client specific things in useEffect instead
     * as a useEffect does not run on the server and is designed by react to be able to manipulate the DOM after rendering.
     */
    var SERVER: boolean;

    type CustomInputTypePropsBase = {
        validationFailure: PublicError | null;
        helpFieldId: string;
        label?: React.ReactNode;
        help?: React.ReactNode;
        icon?: React.ReactNode;
        inputRef: HTMLElement | null;
        onInputRef?: (el: HTMLElement) => void;
        onBlur?: (e: React.FocusEvent) => void;
        onChange?: (e: React.ChangeEvent) => void;
        onCanvasChange?: (source: string) => void;
    };

    /**
     * Used when an input type is rendering.
     */
    type CustomInputTypeProps<T extends keyof InputPropsRegistry> = CustomInputTypePropsBase & {
        field: InputPropsRegistry[T];
    };

    /**
     * A partial global interface which you can extend with your custom input type and the props it supports.
     * Those types must all extend BaseInputProps.
     */
    interface InputPropsRegistry {}

    /**
     * The actual set of input props available.
     */
    type InputProps = {
        [K in keyof InputPropsRegistry]: { type: K } & InputPropsRegistry[K]
    }[keyof InputPropsRegistry]

    var inputTypes: {
        [K in keyof InputPropsRegistry]: React.FC<CustomInputTypeProps<K>>;
    };

    /**
     * A base URL for the API.
     */
    var apiHost: string;

    /**
     * Optional token which will be used as a Token header by webRequest.
     */
    var storedToken: string | null;

    /**
     * Require modules by their alias, available at runtime.
     * Returns the raw UMD module meaning e.g. a default export is available as .default
     */
    var require: (moduleName: string) => any;

    /**
     * Called before pages are loaded.
     */
    var beforePageLoad: ((url: string) => Promise<void>) | null;

    /**
     * The module manager, which holds a set of the UMD modules.
     */
    var __mm: Record<string, any>;
}

export {};