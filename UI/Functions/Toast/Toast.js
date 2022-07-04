/*
* ToastInfo will be passed directly to your ToastList render function.
* toastInfo.duration defines how long it's visible for (don't set it at all if you want it to be explicitly closed by the user).
*/

const SessionToasts = React.createContext();

export const Provider = (props) => {
	const [toastList, setToastList] = React.useState([]);

	let close =  toastInfo => {
		toastList = toastList.filter(toast => toast != toastInfo);
		setToastList(toastList);
	}

	let pop = toastInfo => {
		
		toastList.push(toastInfo);
		
		setToastList(toastList);
		
		if(toastInfo.duration){
			setTimeout(() => {
				close(toastInfo);
			}, toastInfo.duration * 1000);
		}
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
