import { RouterProvider } from '@tanstack/react-router'
import { router } from './router'
import { AuthProvider } from './contexts/AuthContext'
import { initializeApiClient } from '@memberorg/api-client'
import authService from './services/auth'
import './App.css'

// Initialize the API client with proper configuration
initializeApiClient({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5001/api',
  getAuthToken: () => authService.getToken(),
  onAuthError: () => authService.logout()
})

function App() {
  return (
    <AuthProvider>
      <RouterProvider router={router} />
    </AuthProvider>
  )
}

export default App