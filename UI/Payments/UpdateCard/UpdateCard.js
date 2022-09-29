import Input from 'UI/Input';
import Form from 'UI/Form';
import { useRouter } from 'UI/Session';

export default function UpdateCard(props) {
    const { pageState, setPage } = useRouter();
    return <div className="update-card">
        <h2 className="payment-checkout__title">
            {`Update Card`}
        </h2>
        <Form
            action={'subscription/' + pageState.tokens[0] + '/update-card'}
            failedMessage={`Unable to update card`}
            loadingMessage={`Updating card details...`}
            onSuccess={info => {

                    if(info.status == 200){
                        setPage('/complete?status=card-update.success');
                    }else{
                        setPage('/complete?status=card-update.failed');
                    }
            }}
        >
            <div className="mb-3">
                <Input type='payment' name='paymentMethod' updateMode="true" label='Payment method' validate={['Required']} />
            </div>
            <div className="payment-checkout__footer">
                <button type="submit" className="btn btn-primary">
                    <i className="fal fa-fw fa-credit-card" />
                    {`Confirm Update`}
                </button>
            </div>
        </Form>
    </div>;
}

UpdateCard.propTypes = {
};

UpdateCard.defaultProps = {
}

UpdateCard.icon='register';