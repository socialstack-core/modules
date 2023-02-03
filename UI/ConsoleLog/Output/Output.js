import React, { useEffect, useRef, useState } from 'react'
import { serialize } from 'UI/ConsoleLog/serialize'
import { mark } from 'UI/ConsoleLog/util'

function Output (props) {
  const [command, setCommand] = useState('')
  const history = props.history || []

  const scrollToBottom = () => {
    messagesEndRef.current.scrollIntoView({ behavior: 'smooth' })
  }
  useEffect(scrollToBottom, [history])
  const messagesEndRef = useRef(null)

  return (
    <div className={'console ' + (props.theme || '')}>
      <ul className='output'>
        {history.map((line, i) => {
          return (
            <li key={i} className='line'>
              <div className={'mode ' + line.mode}>
                <div className='marker'>{mark(line.mode)}</div>
                <div className='timestamp'>{line.timeStamp}</div>
              </div>
              <div className='content' dangerouslySetInnerHTML={{ __html: serialize(line.content) }} />
            </li>
          )
        })}
      </ul>

      <div ref={messagesEndRef} />

    </div>
  )
}

export default Output
