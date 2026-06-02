import { useCallback, useEffect, useRef } from "react";

/**
 * Enum representing the horizontal side from which the Popout appears.
 */
enum PopoutSide {
    Left,
    Right
}

/**
 * Props for the `Popout` component.
 */
type PopoutProps = React.PropsWithChildren<{
    /**
     * Callback triggered when the popout requests to close,
     * either from clicking the backdrop or pressing Escape.
     */
    onClose: () => void;

    /**
     * Side from which the popout slides in.
     */
    side: PopoutSide;
}>;

/**
 * A controlled popout (modal-like) component that slides in from the left or right.
 * It supports dismissal via backdrop click and Escape key.
 *
 * Example usage:
 * ```tsx
 * <Popout side={PopoutSide.Right} onClose={handleClose}>
 *   <YourContentHere />
 * </Popout>
 * ```
 */
const Popout: React.FC<PopoutProps> = ({ children, onClose, side }: PopoutProps) => {
    const popoutRootRef = useRef<HTMLDivElement>(null);

    /**
     * Handles clicks on the backdrop, triggering close if clicked outside the content.
     */
    const handleBackdropClick = useCallback((ev: React.MouseEvent<HTMLDivElement>) => {
        if (ev.target === popoutRootRef.current) {
            onClose();
        }
    }, [onClose]);

    /**
     * Handles Escape key press to trigger close.
     */
    useEffect(() => {
        const handleKeyDown = (ev: KeyboardEvent) => {
            if (ev.code === "Escape") {
                onClose();
            }
        };

        window.addEventListener("keydown", handleKeyDown);
        return () => {
            window.removeEventListener("keydown", handleKeyDown);
        };
    }, [onClose]);

    const contentSideClass = side === PopoutSide.Left ? "left" : "right";

    return (
        <div
            className="popout-background"
            ref={popoutRootRef}
            onClick={handleBackdropClick}
        >
            <div className={`popout-content ${contentSideClass}`}>
                {children}
            </div>
        </div>
    );
};

export default Popout;

export {
    PopoutSide, 
    PopoutProps
}
