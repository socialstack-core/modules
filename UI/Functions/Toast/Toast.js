/*
* ToastInfo will be passed directly to your ToastList render function.
* toastInfo.duration defines how long it's visible for (don't set it at all if you want it to be explicitly closed by the user).
*/

const SessionToasts = React.createContext();

export const Provider = (props) => {
	const [toastList, setToastList] = React.useState([]);

	let close =  toastInfo => {
		setToastList(toastList.filter(toast => toast != toastInfo));
	}

	React.useEffect(() => {
        toastList.forEach(toast => {
            var now = Date.now();
            toast.timeout = setTimeout(() => {
                close(toast);
            }, toast.closeTime - now);
        })

        return () => {
            toastList.forEach(toast => {
                if(toast.timeout){
                    clearTimeout(toast.timeout);
                }
            });
        };

    }, [toastList]);


	let pop = toastInfo => {
        if(toastInfo.duration){
            toastInfo.closeTime = Date.now() + (toastInfo.duration * 1000);
        }

        setToastList([...toastList, toastInfo]);
    }

	return (
		<SessionToasts.Provider
			value={{
				toastList,
				pop,
				close
			}}
		>
			{props.children}
		</SessionToasts.Provider>
	);
};

export { SessionToasts }; 

export function useToast() {
	return React.useContext(SessionToasts);
}
