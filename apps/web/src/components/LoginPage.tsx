import { useState } from 'react'
import './LoginPage.css'
import Navigation from './Navigation'

const LoginPage = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: ''
  })

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target
    setFormData(prev => ({
      ...prev,
      [name]: value
    }))
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    // Handle login here
    console.log('Login submitted:', formData)
  }

  return (
    <div className="login-page">
      <Navigation />

      <div className="login-container">
        <div className="login-form-section">
          <h1 className="login-title">Sign in</h1>

          <form onSubmit={handleSubmit} className="login-form">
            <div className="form-group">
              <label htmlFor="email">E-mail</label>
              <input
                type="email"
                id="email"
                name="email"
                value={formData.email}
                onChange={handleInputChange}
                placeholder="your.email@example.com"
                required
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
              />
            </div>

            <button type="submit" className="signin-btn">
              Sign in
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