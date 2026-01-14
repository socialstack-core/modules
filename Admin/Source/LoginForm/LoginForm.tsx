import LoginForm, { LoginFormProps } from 'UI/LoginForm';

/**
 * Admin login form.
 */
const AdminLoginForm: React.FC<LoginFormProps> = (props) => <LoginForm registerUrl='/en-admin/register' redirectTo='/en-admin/' {...props} />;

export default AdminLoginForm;