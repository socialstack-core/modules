
interface SidebarTemplateProps {
    header?: React.ReactNode,
    body?: React.ReactNode,
    footer?: React.ReactNode,
}

const BaseWebTemplate: React.FC<SidebarTemplateProps> = (props : SidebarTemplateProps) => {

     return (
          <div id="wrapper">
               {props.header && <header>{props.header}</header>}
               <div id="content">
                    {props.body && <main>{props.body}</main>}
               </div>
               {props.footer && <footer>{props.footer}</footer>}
          </div>
     )
};

// BaseWebTemplate.propTypes = {}

export default BaseWebTemplate;