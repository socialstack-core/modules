/*
* ToastInfo will be passed directly to your ToastList render function.
* toastInfo.duration defines how long it's visible for (don't set it at all if you want it to be explicitly closed by the user).
*/

const SessionChat = React.createContext();

export const Provider = (props) => {
	const [chat, setChat] = React.useState({});
    const [chatIdentity, setChatIdentity] = React.useState({});


    let startChat = mode => {
        if (mode == chat.mode) {
            return;
        }

        setChat({mode});
    }

	return (
		<SessionChat.Provider
			value={{
				chat,
                chatIdentity,
                setChat,
                setChatIdentity,
                startChat
			}}
		>
			{props.children}
		</SessionChat.Provider>
	);
};

export { SessionChat }; 

export function useChat() {
	return React.useContext(SessionChat);
}
