import { useState, useEffect } from 'react'
import { ApiClient } from '@memberorg/api-client'
import { APP_NAME } from '@memberorg/shared'
import './App.css'

const apiClient = new ApiClient('http://localhost:5001')

function App() {
  const [message, setMessage] = useState<string>('Loading...')

  useEffect(() => {
    apiClient.getHello()
      .then(data => setMessage(data.message))
      .catch(() => setMessage('Failed to connect to API'))
  }, [])

  return (
    <div className="App">
      <h1>{APP_NAME} - Web</h1>
      <p>{message}</p>
    </div>
  )
}

export default App