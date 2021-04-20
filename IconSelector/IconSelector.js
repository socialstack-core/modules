import Modal from 'UI/Modal';
import Loop from 'UI/Loop';
import faIcons from 'Admin/IconSelector/faIcons';
import Input from 'UI/Input';
import Col from 'UI/Column';
import Row from 'UI/Row';
import Spacer from 'UI/Spacer';
import Debounce from 'UI/Functions/Debounce';


export default class IconSelector extends React.Component {
    constructor(props){
		super(props);
        this.search = this.search.bind(this);
		this.state = {
			selectIcon: false,
            selectedIcon: null,
            styleFilter: "all",
            debounce: new Debounce(this.search)
		};
		
	}

    search(query) {
        console.log(query);
        this.setState({searchFilter: query})
    }

    closeModal() {
        this.setState({selectIcon: false});
    }

    render(){
        var {selectIcon, value, styleFilter, searchFilter} = this.state;

        var currentRef = this.props.value || this.props.defaultValue;
		
		if(this.state.value !== undefined){
			currentRef = this.state.value;
            console.log("ref updated")
            console.log(currentRef);
		}

        return <div className = "icon-selector">

            <label>Icon</label>
            <div>
                {value ? <i className = {"icon " + value.replace(/:/g, " ")}/> : "None Selected"}
                <div className = "btn btn-secondary" onClick = {()=> {
                    this.setState({selectIcon: true});
                }}>
                    Change
                </div>
                <div className = "btn btn-danger" onClick = {() => {
                    this.setState({value: null})
                }}>
                    Remove
                </div>
            </div>

            <Modal
                visible = {selectIcon}
                onClose = {() => this.closeModal()}
                isLarge 
                title = {"Select an icon"}
                className={"icon-select-modal"}
            >
                <Spacer/>
                <label>
                    Style
                </label>
                <Input type ="select"
                    name = "style"
                    onChange = {(e) => {
                        this.setState({styleFilter: e.target.value})
                    }}
                >
                    <option value = {"all"}>All</option>
                    <option value = {"regular"}>Regular</option>
                    <option value = {"solid"}>Solid</option>
                    <option value = {"brands"}>Brands</option>
                </Input>
                <label>
                    Search
                </label>
                <Input type = "text" name = "search" onKeyUp = {(e) => {
                    this.state.debounce.handle(e.target.value);
                }}/>
                <Row>
                    <Loop
                        raw
                        over = {faIcons}
                    >
                        {icon => {
                            if(icon.name.includes(searchFilter) ||icon.name.replace(/-/g, " ").includes(searchFilter) || !searchFilter) {
                                return icon.styles.map(style => {
                                    if(styleFilter == "all" || styleFilter == style) {
                                        return <Col className="icon-tile" size = {3} onClick= {() => {
                                            this.setState({value: "fa"+style[0]+":fa-" + icon.name})
                                            this.closeModal && this.closeModal();
                                        }}>
                                            <i className={"fa"+style[0]+" fa-" + icon.name} />
                                            <p>{icon.name.replace(/-/g, " ")} ({style})</p>
                                        </Col>
                                    }
                                }) 
                            }
                        }}
                    </Loop>
                </Row>
            </Modal>
            {this.props.name && (
				/* Also contains a hidden input field containing the value */
				<input type="hidden" value={currentRef} name={this.props.name} id={this.props.id} />
			)}
        </div>
    }
}