import Modal from 'UI/Modal';
import Loop from 'UI/Loop';
import faIconsRef from './faIcons.json';
import Input from 'UI/Input';
import Col from 'UI/Column';
import Row from 'UI/Row';
import Loading from 'UI/Loading';
import Spacer from 'UI/Spacer';
import Debounce from 'UI/Functions/Debounce';
import webRequest from 'UI/Functions/WebRequest';
import getRef from 'UI/Functions/GetRef';

let icons = [];

export default class IconSelector extends React.Component {
    constructor(props){
		super(props);
        this.search = this.search.bind(this);
		this.state = {
			icons,
			selectIcon: false,
            selectedIcon: null,
            styleFilter: "all",
            debounce: new Debounce(this.search)
		};
		
	}
	
	componentDidMount(){
		if(!this.state.icons.length){
			webRequest(getRef(faIconsRef, {url: true})).then(response => {
				icons = response.json;
				this.setState({icons});
			});
		}
	}
	
    search(query) {
        console.log(query);
        this.setState({searchFilter: query})
    }

    closeModal() {
        this.props.onClose && this.props.onClose();
    }

    render(){
        var {selectIcon, value, styleFilter, searchFilter} = this.state;

        return <div className = "icon-selector">
            <Modal
                visible = {this.props.visible}
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
                        over = {icons}
						orNone = {() => <Loading />}
                    >
                        {icon => {
                            if(icon.name.includes(searchFilter) ||icon.name.replace(/-/g, " ").includes(searchFilter) || !searchFilter) {
                                return icon.styles.map(style => {
                                    if(styleFilter == "all" || styleFilter == style) {
                                        return <Col className="icon-tile" size = {3} onClick= {() => {
                                            var newIcon = "fa"+style[0]+":fa-" + icon.name;
                                            
                                            this.setState({value: newIcon});
                                            this.props.onSelected && this.props.onSelected(newIcon)
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
        </div>
    }
}