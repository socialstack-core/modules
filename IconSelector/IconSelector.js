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
let iconStyles = [];
let iconSets = [];

export default class IconSelector extends React.Component {
    constructor(props){
		super(props);
        this.search = this.search.bind(this);
		this.state = {
			selectIcon: false,
            selectedIcon: null,
            debounce: new Debounce(this.search)
		};
		
	}
	
	componentDidMount(){
		if(!icons.length){
			
			var styles = [{name: 'All', key: 'all'},{name:'Regular', key: 'regular', prefix: 'far'}, {name:'Solid', key: 'solid', prefix: 'fas'}, {name: 'Brands', key: 'brands', prefix: 'fab'}];
			var sets = [{name: 'All', key: 'all'}, {name: 'Default (FontAwesome)', key: 'default'}];
			
			var proms = [webRequest(getRef(faIconsRef, {url: true}))];
			
			if(global.customIcons){
				global.customIcons.forEach(ci => {
					proms.push(webRequest(getRef(ci.listRef, {url: true})).then(response=>{
						response.json.forEach(icon => {
							if(ci.prefix){
								icon.prefix = ci.prefix;
							}
							
							icon.set = ci.customSetName || 'custom';
						});
						
						sets.push({name: ci.customSetName || 'custom', key: ci.customSetName || 'custom'});
						
						return response;
					}));
					
				});
				
			}
			
			Promise.all(proms).then(responses => {
				icons = [];
				responses.forEach(r => icons=icons.concat(r.json));
				iconStyles = styles;
				iconSets = sets;
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

    renderHeader(){
        return <div className = "row header-container">
            <h5>Select an icon</h5>
            <Col size = {6}>
                <label>
                    Style
                </label>
                <Input type ="select"
                    name = "style"
                    onChange = {(e) => {
                        this.setState({styleFilter: e.target.value})
                    }}
                >
					{iconStyles.map(s => <option value = {s.key}>{s.name}</option>)}
                </Input>
                <label>
                    Set
                </label>
                <Input type ="select"
                    name = "set"
                    onChange = {(e) => {
                        this.setState({setFilter: e.target.value})
                    }}
                >
					{iconSets.map(s => <option value = {s.key}>{s.name}</option>)}
                </Input>
            </Col>
            <Col size = {6}>
                <label>
                    Search
                </label>
                <Input type = "text" name = "search" onKeyUp = {(e) => {
                    this.state.debounce.handle(e.target.value);
                }}/>
            </Col>
        </div>;
    }

    render(){
        var {selectIcon, value, styleFilter, setFilter, searchFilter} = this.state;
		
		var prefixForStyle = {};
		
		iconStyles.forEach(s => {
			prefixForStyle[s.key] = s.prefix;
		});
		
        return <div className = "icon-selector">
            <Modal
                visible = {this.props.visible}
                onClose = {() => this.closeModal()}
                isLarge 
                className={"icon-select-modal"}
                title = {this.renderHeader()}
            >
                <div className = "row icon-container">
					<Loop
                        raw
                        over = {icons}
						orNone = {() => <Loading />}
                    >
                        {icon => {
                            if(icon.name.includes(searchFilter) ||icon.name.replace(/-/g, " ").includes(searchFilter) || !searchFilter) {
                                return icon.styles.map(style => {
									
									if(styleFilter && styleFilter != "all"){
										if(styleFilter != style){
											return null;
										}
									}
									
									if(setFilter && setFilter != "all"){
										if(setFilter == 'default'){
											if(icon.set){
												return null;
											}
										}else if(setFilter != icon.set){
											return null;
										}
									}
									
									var prefix = prefixForStyle[style];
									
									return <Col className="icon-tile" size = {3} onClick= {() => {
										var newIcon = prefix+":"+(icon.prefix||"fa")+"-" + icon.name;
										
										this.setState({value: newIcon});
										this.props.onSelected && this.props.onSelected(newIcon)
										this.closeModal && this.closeModal();
									}}>
										<i className={prefix+" "+(icon.prefix||"fa")+"-" + icon.name} />
										<p>{icon.name.replace(/-/g, " ")} ({style})</p>
									</Col>
                                }) 
                            }
                        }}
                    </Loop>
                </div>
            </Modal>
        </div>
    }
}