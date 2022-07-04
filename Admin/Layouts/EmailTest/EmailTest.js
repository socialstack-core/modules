import Tile from 'Admin/Tile';
import Form from 'UI/Form';
import Input from 'UI/Input';
import Default from 'Admin/Layouts/Default';


export default function EmailTest(props){
	return  <Default>
		<Tile className="email-test" title={'Email Test'}>
			<Form 
				action='emailtemplate/test'
				submitLabel='Send test to yourself'
				onValues={(values) => {
					if(values.customData){
						try{
							var cd = JSON.parse(values.customData);
							values.customData = cd;
						}catch(e){
							// Ignore (it was probably blank).
							console.warn("Invalid custom email data. It was dropped: ", e);
							values.customData = null;
						}
					}else{
						values.customData = null;
					}
					return values;
				}}
			>
				<Input type='text' name='templateKey' label='Template Key' />
				<Input type='text' contentType='application/json' name='customData' label='Custom Data JSON' />
			</Form>
		</Tile>
	</Default>;
}