import { useState, useEffect } from 'react'
import { ApiClient } from '@memberorg/api-client'
import { APP_NAME } from '@memberorg/shared'
import './App.css'

const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5001'
const apiClient = new ApiClient(apiUrl)

function App() {
  const [message, setMessage] = useState<string>('Loading...')

  useEffect(() => {
    console.log('API URL:', apiUrl)
    apiClient.getHello()
      .then(data => setMessage(data.message))
      .catch(err => {
        console.error('API Error:', err)
        setMessage(`Failed to connect to API at ${apiUrl}`)
      })
  }, [])

  return (
    <div className="App">
      <h1>{APP_NAME} - Web</h1>
      <p>{message}</p>
    </div>
  )
}

export default App