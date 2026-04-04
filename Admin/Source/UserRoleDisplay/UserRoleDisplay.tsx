import {AutoListCellRenderer} from "Admin/AutoList";
import { User } from "Api/User";

const UserRoleDisplay: React.FC<AutoListCellRenderer> = props => {
    const { entity, value } = props;
    const roleName = (entity as User)?.userRole?.name;
    
    return <span>{roleName || value}</span>;
};

export default UserRoleDisplay;
