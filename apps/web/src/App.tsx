import { RouterProvider } from '@tanstack/react-router'
import { router } from './router'
import { AuthProvider } from './contexts/AuthContext'
import { initializeApiClient } from '@memberorg/api-client'
import './App.css'

// Initialize the API client with proper configuration
// The auth token will be provided by the AuthContext
initializeApiClient({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5001/api',
  getAuthToken: () => {
    // Get token directly from localStorage for now
    // This will be replaced when auth context is initialized
    return localStorage.getItem('authToken')
  },
  onAuthError: () => {
    // Clear auth data on 401
    localStorage.removeItem('authToken')
    localStorage.removeItem('authExpiresAt')
    localStorage.removeItem('authUser')
    window.location.href = '/login'
  }
})

function App() {
  return (
    <AuthProvider>
      <RouterProvider router={router} />
    </AuthProvider>
  )
}

export default App