import Default from 'Admin/Layouts/Default'
import Container from 'UI/Container'
import Column from 'UI/Column'
import Row from 'UI/Row'
import Form from 'UI/Form'
import Input from 'UI/Input'
import webRequest from 'UI/Functions/WebRequest'

import { useState, useEffect } from 'react'

export default function CreateNewPage(props) {

    const [layouts, setLayouts] = useState(null)

    useEffect(() => {

        if (!layouts) {

            webRequest('layout/list').then((resp) => {
                setLayouts(resp.json.results)
            })
            .catch(console.error)
        }

    }, [layouts]);

    return (
        <Default>
            <div className='new-page-container'>
                <Container>
                    <Row>
                        <Column size='lg'>
                            <h3>{`Add a new page`}</h3>
                            <p>{`Please choose a page title, URL and layout. You can add content and images after the page is created.`}</p>
                        </Column>
                    </Row>
                    <Row>
                        <Column size='lg' className='content'>
                            <Form
                                action='page'
                                method='POST'
                                onSuccess={(resp) => {
                                    location.href = '/en-admin/page/' + resp.id;
                                }}
                                onError={(err) => {
                                    console.error(err)
                                }}
                                onValues={(vals) => {
                                    vals.layoutId = parseInt(vals.layoutId);
                                    return vals;
                                }}
                            >
                                <Input label='Page title' name='title' help={`Enter the page title:`}/>
                                <Input label='URL' name='url' help={`Example: /blog/creating-a-page:`} />
                                <Input label='Layout' name='layoutId' type='select' help={`Choose a layout for the page:`}>
                                    {layouts?.map(layout => <option value={layout.id}>{layout.name}</option>)}
                                </Input>
                                <button className='btn btn-primary'>{`Create page`}</button>
                            </Form>
                        </Column>
                    </Row>
                </Container>
            </div>
        </Default>
    )

}

CreateNewPage.propTypes = {};