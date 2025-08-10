import { useState } from 'react'
import { useNavigate } from '@tanstack/react-router'
import './MembershipPage.css'
import Navigation from './Navigation'
import { useAuth } from '../contexts/AuthContext'

const MembershipPage = () => {
  const navigate = useNavigate()
  const { register } = useAuth()
  const [selectedPlan, setSelectedPlan] = useState<'individual' | 'family' | 'student'>('individual')
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    username: '',
    password: '',
    confirmPassword: '',
    dateOfBirth: ''
  })
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(false)

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target
    setFormData(prev => ({
      ...prev,
      [name]: value
    }))
    setError('')
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')

    // Validate passwords match
    if (formData.password !== formData.confirmPassword) {
      setError('Passwords do not match')
      return
    }

    setIsLoading(true)

    try {
      // Register the user
      await register({
        username: formData.username,
        email: formData.email,
        password: formData.password,
        firstName: formData.firstName,
        lastName: formData.lastName,
        dateOfBirth: formData.dateOfBirth || undefined
      })
      
      // TODO: After successful registration, save membership plan preference
      // This could be stored in user profile or sent to a separate API endpoint
      console.log('Membership plan selected:', selectedPlan)
      
      // Navigate to home after successful registration
      navigate({ to: '/' })
    } catch (err: any) {
      setError(err.response?.data?.message || 'Registration failed. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="membership-page">
      <Navigation />

      <div className="membership-container">
        {/* Pricing Section */}
        <section className="pricing-section">
          <h1 className="membership-title">Choose Your Membership</h1>
          <p className="membership-subtitle">Join Birmingham's premier forum for international affairs</p>

          <div className="pricing-grid">
            <div 
              className={`pricing-card ${selectedPlan === 'individual' ? 'selected' : ''}`}
              onClick={() => setSelectedPlan('individual')}
            >
              <h3 className="pricing-tier">Individual</h3>
              <div className="pricing-amount">
                <span className="price">$125</span>
                <span className="period">/year</span>
              </div>
              <ul className="pricing-features">
                <li>✓ Access to all speaker events</li>
                <li>✓ Monthly newsletter</li>
                <li>✓ Member directory access</li>
                <li>✓ Voting rights</li>
              </ul>
            </div>

            <div 
              className={`pricing-card featured ${selectedPlan === 'family' ? 'selected' : ''}`}
              onClick={() => setSelectedPlan('family')}
            >
              <div className="featured-badge">Most Popular</div>
              <h3 className="pricing-tier">Family</h3>
              <div className="pricing-amount">
                <span className="price">$200</span>
                <span className="period">/year</span>
              </div>
              <ul className="pricing-features">
                <li>✓ All Individual benefits</li>
                <li>✓ Plus one guest at all events</li>
                <li>✓ Priority event registration</li>
                <li>✓ Special family programs</li>
                <li>✓ Youth engagement opportunities</li>
              </ul>
            </div>

            <div 
              className={`pricing-card ${selectedPlan === 'student' ? 'selected' : ''}`}
              onClick={() => setSelectedPlan('student')}
            >
              <h3 className="pricing-tier">Student</h3>
              <div className="pricing-amount">
                <span className="price">$25</span>
                <span className="period">/year</span>
              </div>
              <ul className="pricing-features">
                <li>✓ Access to all speaker events</li>
                <li>✓ Monthly newsletter</li>
                <li>✓ Student networking events</li>
                <li>✓ Career development resources</li>
              </ul>
            </div>
          </div>
        </section>

        {/* Application Form Section */}
        <section className="application-section">
          <div className="form-container">
            <div className="form-header">
              <h2>Create Your Account</h2>
              <p className="form-subtitle">
                Register to become a member of Birmingham Council on Foreign Relations
              </p>
            </div>

            <form onSubmit={handleSubmit} className="membership-form">
              {error && (
                <div className="error-message" style={{ color: 'red', marginBottom: '1rem' }}>
                  {error}
                </div>
              )}

              {/* Selected Plan Display */}
              <div className="selected-plan-display">
                <label>Selected Membership Plan</label>
                <div className="selected-plan-info">
                  <span className="plan-name">
                    {selectedPlan === 'individual' && 'Individual Membership'}
                    {selectedPlan === 'family' && 'Family Membership'}
                    {selectedPlan === 'student' && 'Student Membership'}
                  </span>
                  <span className="plan-price">
                    {selectedPlan === 'individual' && '$125/year'}
                    {selectedPlan === 'family' && '$200/year'}
                    {selectedPlan === 'student' && '$25/year'}
                  </span>
                </div>
              </div>

              {/* Account Information */}
              <div className="form-section">
                <h3 style={{ marginBottom: '1rem', fontSize: '1.1rem', color: '#333' }}>Account Information</h3>
                
                <div className="form-group">
                  <label htmlFor="username">Username</label>
                  <input
                    type="text"
                    id="username"
                    name="username"
                    value={formData.username}
                    onChange={handleInputChange}
                    placeholder="Choose a username"
                    required
                    disabled={isLoading}
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="email">Email</label>
                  <input
                    type="email"
                    id="email"
                    name="email"
                    value={formData.email}
                    onChange={handleInputChange}
                    placeholder="your.email@example.com"
                    required
                    disabled={isLoading}
                  />
                </div>

                <div className="form-row">
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
                  <div className="form-group">
                    <label htmlFor="confirmPassword">Confirm Password</label>
                    <input
                      type="password"
                      id="confirmPassword"
                      name="confirmPassword"
                      value={formData.confirmPassword}
                      onChange={handleInputChange}
                      placeholder="••••••••••••"
                      required
                      disabled={isLoading}
                    />
                  </div>
                </div>
              </div>

              {/* Personal Information */}
              <div className="form-section">
                <h3 style={{ marginBottom: '1rem', fontSize: '1.1rem', color: '#333' }}>Personal Information</h3>
                
                <div className="form-row">
                  <div className="form-group">
                    <label htmlFor="firstName">First Name</label>
                    <input
                      type="text"
                      id="firstName"
                      name="firstName"
                      value={formData.firstName}
                      onChange={handleInputChange}
                      placeholder="Your first name"
                      required
                      disabled={isLoading}
                    />
                  </div>
                  <div className="form-group">
                    <label htmlFor="lastName">Last Name</label>
                    <input
                      type="text"
                      id="lastName"
                      name="lastName"
                      value={formData.lastName}
                      onChange={handleInputChange}
                      placeholder="Your last name"
                      required
                      disabled={isLoading}
                    />
                  </div>
                </div>

                <div className="form-row">
                  <div className="form-group">
                    <label htmlFor="dateOfBirth">Date of Birth</label>
                    <input
                      type="date"
                      id="dateOfBirth"
                      name="dateOfBirth"
                      value={formData.dateOfBirth}
                      onChange={handleInputChange}
                      max={new Date().toISOString().split('T')[0]}
                      disabled={isLoading}
                    />
                  </div>
                  <div className="form-group">
                    <label htmlFor="phone">Phone Number</label>
                    <input
                      type="tel"
                      id="phone"
                      name="phone"
                      value={formData.phone}
                      onChange={handleInputChange}
                      placeholder="(555) 123-4567"
                      disabled={isLoading}
                    />
                  </div>
                </div>
              </div>

              <button type="submit" className="submit-btn" disabled={isLoading}>
                {isLoading ? 'Creating Account...' : 'Create Account & Continue'}
              </button>
            </form>

            <p className="form-footer">
              Already have an account? <a href="/login">Sign in</a>
            </p>
          </div>

          {/* Side illustration */}
          <div className="illustration-container">
            <div className="illustration">
              <div className="face face-1">
                <div className="glasses"></div>
              </div>
              <div className="face face-2">
                <div className="hair"></div>
              </div>
              <div className="face face-3">
                <div className="hair-bun"></div>
              </div>
              <div className="sparkle">✨</div>
            </div>
          </div>
        </section>
      </div>
    </div>
  )
}

export default MembershipPage