import { useState } from 'react'
import { useNavigate } from '@tanstack/react-router'
import './LoginPage.css'
import Navigation from './Navigation'
import { useAuth } from '../contexts/AuthContext'

const LoginPage = () => {
  const navigate = useNavigate()
  const { login } = useAuth()
  const [formData, setFormData] = useState({
    username: '',
    password: ''
  })
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(false)

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target
    setFormData(prev => ({
      ...prev,
      [name]: value
    }))
    setError('') // Clear error when user types
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    console.log('Form submitted with:', formData) // Debug log
    
    // Debug: Check if form data is empty
    if (!formData.username || !formData.password) {
      console.error('Form data is empty!', formData)
      setError('Please enter username and password')
      return
    }
    
    setError('')
    setIsLoading(true)

    try {
      console.log('Calling login with:', formData.username, formData.password)
      await login({
        username: formData.username,
        password: formData.password
      })
      console.log('Login successful, navigating to home')
      navigate({ to: '/' })
    } catch (err: any) {
      console.error('Login error:', err) // Debug log
      setError(err.response?.data?.message || 'Login failed. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="login-page">
      <Navigation />

      <div className="login-container">
        <div className="login-form-section">
          <h1 className="login-title">Sign in</h1>

          <form onSubmit={handleSubmit} className="login-form">
            {error && (
              <div className="error-message" style={{ color: 'red', marginBottom: '1rem' }}>
                {error}
              </div>
            )}
            
            <div className="form-group">
              <label htmlFor="username">Username</label>
              <input
                type="text"
                id="username"
                name="username"
                value={formData.username}
                onChange={handleInputChange}
                placeholder="Enter your username"
                required
                disabled={isLoading}
              />
            </div>

            <div className="form-group">
              <label htmlFor="password">Password</label>
              <input
                type="password"
                id="password"
                name="password"
                value={formData.password}
                onChange={handleInputChange}
                placeholder="••••••••••••"
                required
                disabled={isLoading}
              />
            </div>

            <button type="submit" className="signin-btn" disabled={isLoading}>
              {isLoading ? 'Signing in...' : 'Sign in'}
            </button>
          </form>

          <a href="#" className="forgot-password">Forgot password?</a>

          <p className="signup-prompt">
            Not a member yet? <a href="/membership">Sign up</a>
          </p>
        </div>

        {/* Side illustration */}
        <div className="login-illustration">
          <div className="illustration-faces">
            <div className="face face-1">
              <div className="glasses"></div>
              <div className="mustache"></div>
            </div>
            <div className="face face-2">
              <div className="hair"></div>
              <div className="smile"></div>
            </div>
            <div className="face face-3">
              <div className="hair-bun"></div>
            </div>
            <div className="sparkle">✨</div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default LoginPage