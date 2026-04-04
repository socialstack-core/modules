const Landing: React.FC<React.PropsWithChildren<{}>> = (props: React.PropsWithChildren<{}>): React.ReactNode => {
	const { children } = props;

    return (
        <div className="admin-landing">
			{children}
        </div>
    );    
}

export default Landing;