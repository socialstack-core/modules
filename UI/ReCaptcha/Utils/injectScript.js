export default (scriptSrc: string): void => {

    if (!window.SERVER) {
        const script = document.createElement('script');

        script.async = true;
        script.defer = true;
        script.src = scriptSrc;

        if (document.head) {
            document.head.appendChild(script);
        }
    }
};