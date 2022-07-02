import activitiesRef from './Categories/activities.json';
import animalsRef from './Categories/animals.json';
import flagsRef from './Categories/flags.json';
import foodRef from './Categories/food.json';
import objectsRef from './Categories/objects.json';
import peopleRef from './Categories/people.json';
import smileysRef from './Categories/smileys.json';
import symbolsRef from './Categories/symbols.json';
import travelRef from './Categories/travel.json';
import Modal from 'UI/Modal';
import Loop from 'UI/Loop';
import Input from 'UI/Input';
import Col from 'UI/Column';
import Row from 'UI/Row';
import Loading from 'UI/Loading';
import Spacer from 'UI/Spacer';
import Debounce from 'UI/Functions/Debounce';
import webRequest from 'UI/Functions/WebRequest';
import getRef from 'UI/Functions/GetRef';



var skinTones = ["🏻","🏼","🏽","🏾","🏿"]

var categories = [
	{
		name: "Smileys",
		icon: "far fa-smile-beam",
		set: smileysRef
	},
	{
		name: "People",
		icon: "far fa-user",
		set: peopleRef
	},
	{
		name: "Animals & Nature",
		icon: "far fa-user",
		set: animalsRef
	},
	{
		name: "Food & Drink",
		icon: "fa fa-hamburger",
		set: foodRef
	},
	{
		name: "Travel",
		icon: "fa fa-plane-departure",
		set: travelRef
	},
	{
		name: "Activities",
		icon: "fa fa-volleyball-ball",
		set: activitiesRef
	},
	{
		name: "Objects",
		icon: "fa fa-volleyball-ball",
		set: objectsRef
	},
	{
		name: "Symbols",
		icon: "fa fa-peace",
		set: symbolsRef
	},
	{
		name: "Flags",
		icon: "far fa-flag",
		set: flagsRef
	},
];


export default class Emojis extends React.Component {
    constructor(props){
		super(props);
		this.state = {
			category: 0,
			skin: 0,
			set: []
		};
		
	}
	
	componentDidMount(){
		this.changeSet(0);
	}
	
	changeSet(index){
		if(categories[index].loaded){
			this.setState({set: categories[index].loaded});
		}else{
			webRequest(getRef(categories[index].set, {url: true})).then(response => {
				categories[index].loaded = response.json;
				this.setState({set: response.json});
			});
		}
	}
	
    closeModal() {
        this.props.onClose && this.props.onClose();
    }

    render(){
        var { set, skin, category } = this.state;

        return <div className = "emojis">
            <Modal
                visible = {this.props.visible}
                onClose = {() => this.closeModal()}
                isLarge 
                title = {"Select an emoji"}
                className={"emoji-select-modal"}
            >
                <Spacer/>
                <label>
                    Type
                </label>
                <Input type ="select"
                    name = "category"
                    onChange = {(e) => {
						var num = parseInt(e.target.value);
                        this.setState({category: num})
						this.changeSet(num);
                    }}
                >
					{
						categories.map((cat, i) => <option value={i}>{cat.name}</option>)
					}
                </Input>
                {category == 1 && <Input type ="select"
                    name = "skin-tone"
					defaultValue={skin}
                    onChange = {(e) => {
                        this.setState({skin: parseInt(e.target.value)})
                    }}
                >
					{
						skinTones.map((character, i) => <option value={i}>{character}</option>)
					}
                </Input>}
                <Row>
					<Loop
                        raw
                        over = {set}
						orNone = {() => <Loading />}
                    >
                        {emoji => {
							if(category == 1 && emoji.skin !== undefined && emoji.skin != skin){
								return null;
							}
							
							return <Col className="emoji-tile" size = {1} onClick= {() => {
								this.props.onSelected && this.props.onSelected(emoji)
								this.closeModal && this.closeModal();
							}}>
								{emoji.char}
							</Col>
                        }}
                    </Loop>
                </Row>
            </Modal>
        </div>
    }
}