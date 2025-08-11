import { useState, useEffect } from 'react'
import './MembershipPage.css'
import Navigation from './Navigation'
import { useAuth } from '../contexts/AuthContext'
import CheckoutForm from './CheckoutForm'

const MembershipPage = () => {
  const { register } = useAuth()
  const [selectedPlan, setSelectedPlan] = useState<'over40' | 'under40' | 'student' | null>(null)
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    username: '',
    password: '',
    confirmPassword: '',
    dateOfBirth: '',
    address: '',
    city: '',
    state: '',
    zipCode: '',
    country: 'United States'
  })
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [age, setAge] = useState<number | null>(null)
  const [isRegistered, setIsRegistered] = useState(false)

  // Calculate age from date of birth
  const calculateAge = (dob: string): number => {
    const birthDate = new Date(dob)
    const today = new Date()
    let age = today.getFullYear() - birthDate.getFullYear()
    const monthDiff = today.getMonth() - birthDate.getMonth()
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
      age--
    }
    return age
  }

  // Auto-select membership tier based on DOB and email
  useEffect(() => {
    // Check if email has .edu domain
    const isStudent = formData.email.toLowerCase().includes('.edu')
    
    if (isStudent) {
      setSelectedPlan('student')
    } else if (formData.dateOfBirth) {
      const userAge = calculateAge(formData.dateOfBirth)
      setAge(userAge)
      if (userAge >= 40) {
        setSelectedPlan('over40')
      } else {
        setSelectedPlan('under40')
      }
    } else {
      setSelectedPlan(null)
      setAge(null)
    }
  }, [formData.dateOfBirth, formData.email])

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

    // Validate membership tier is selected
    if (!selectedPlan) {
      setError('Please enter your date of birth and email to determine your membership tier')
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
        dateOfBirth: formData.dateOfBirth || undefined,
        phone: formData.phone || undefined,
        address: formData.address || undefined,
        city: formData.city || undefined,
        state: formData.state || undefined,
        zipCode: formData.zipCode || undefined,
        country: formData.country || undefined
      })
      
      // After successful registration, show the checkout form
      setIsRegistered(true)
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
          <h1 className="membership-title">Birmingham Council on Foreign Relations Membership</h1>
          <p className="membership-subtitle">Join Birmingham's premier forum for international affairs</p>

          {/* Eligibility Rules Notice */}
          <div style={{
            backgroundColor: '#f0f7ff',
            border: '1px solid #4263EB',
            borderRadius: '8px',
            padding: '1rem',
            marginBottom: '2rem',
            maxWidth: '800px',
            margin: '0 auto 2rem'
          }}>
            <h3 style={{ color: '#4263EB', marginBottom: '0.5rem', fontSize: '1rem' }}>
              📋 Membership Tier Eligibility
            </h3>
            <ul style={{ margin: 0, paddingLeft: '1.5rem', color: '#333', fontSize: '0.9rem' }}>
              <li><strong>Over 40 Membership ($300/year):</strong> Members aged 40 and above</li>
              <li><strong>Under 40 Membership ($200/year):</strong> Members under 40 years of age</li>
              <li><strong>Student Membership ($75/year):</strong> Current students with a valid .edu email address</li>
            </ul>
            <p style={{ marginTop: '0.5rem', color: '#666', fontSize: '0.85rem', fontStyle: 'italic' }}>
              Your membership tier will be automatically determined based on your date of birth and email address.
            </p>
          </div>

          <div className="pricing-grid">
            <div 
              className={`pricing-card ${selectedPlan === 'over40' ? 'selected' : ''}`}
              style={{ cursor: 'default', opacity: selectedPlan === null ? 0.6 : 1 }}
            >
              <h3 className="pricing-tier">Over 40</h3>
              <div className="pricing-amount">
                <span className="price">$300</span>
                <span className="period">/year</span>
                <span style={{ fontSize: '0.75rem', color: '#666', display: 'block', marginTop: '0.25rem' }}>
                  + processing fee
                </span>
              </div>
              <ul className="pricing-features">
                <li>✓ Access to all speaker events</li>
                <li>✓ Monthly newsletter</li>
                <li>✓ Member directory access</li>
                <li>✓ Voting rights</li>
                <li>✓ Special interest groups</li>
                <li>✓ Leadership opportunities</li>
              </ul>
              {selectedPlan === 'over40' && (
                <div style={{ 
                  marginTop: '1rem', 
                  padding: '0.5rem', 
                  backgroundColor: '#4263EB', 
                  color: 'white', 
                  borderRadius: '4px',
                  fontSize: '0.9rem'
                }}>
                  ✓ Your Membership Tier
                </div>
              )}
            </div>

            <div 
              className={`pricing-card ${selectedPlan === 'under40' ? 'selected' : ''}`}
              style={{ cursor: 'default', opacity: selectedPlan === null ? 0.6 : 1 }}
            >
              <h3 className="pricing-tier">Under 40</h3>
              <div className="pricing-amount">
                <span className="price">$200</span>
                <span className="period">/year</span>
                <span style={{ fontSize: '0.75rem', color: '#666', display: 'block', marginTop: '0.25rem' }}>
                  + processing fee
                </span>
              </div>
              <ul className="pricing-features">
                <li>✓ Access to all speaker events</li>
                <li>✓ Monthly newsletter</li>
                <li>✓ Member directory access</li>
                <li>✓ Voting rights</li>
                <li>✓ Young professionals networking</li>
                <li>✓ Career development programs</li>
              </ul>
              {selectedPlan === 'under40' && (
                <div style={{ 
                  marginTop: '1rem', 
                  padding: '0.5rem', 
                  backgroundColor: '#4263EB', 
                  color: 'white', 
                  borderRadius: '4px',
                  fontSize: '0.9rem'
                }}>
                  ✓ Your Membership Tier
                </div>
              )}
            </div>

            <div 
              className={`pricing-card ${selectedPlan === 'student' ? 'selected' : ''}`}
              style={{ cursor: 'default', opacity: selectedPlan === null ? 0.6 : 1 }}
            >
              <h3 className="pricing-tier">Student</h3>
              <div className="pricing-amount">
                <span className="price">$75</span>
                <span className="period">/year</span>
                <span style={{ fontSize: '0.75rem', color: '#666', display: 'block', marginTop: '0.25rem' }}>
                  + processing fee
                </span>
              </div>
              <ul className="pricing-features">
                <li>✓ Access to all speaker events</li>
                <li>✓ Monthly newsletter</li>
                <li>✓ Student networking events</li>
                <li>✓ Career development resources</li>
                <li>✓ Mentorship opportunities</li>
                <li>✓ Academic resources</li>
              </ul>
              {selectedPlan === 'student' && (
                <div style={{ 
                  marginTop: '1rem', 
                  padding: '0.5rem', 
                  backgroundColor: '#4263EB', 
                  color: 'white', 
                  borderRadius: '4px',
                  fontSize: '0.9rem'
                }}>
                  ✓ Your Membership Tier
                </div>
              )}
            </div>
          </div>

          {/* Show selected tier info */}
          {selectedPlan && (
            <div style={{
              textAlign: 'center',
              marginTop: '1rem',
              padding: '1rem',
              backgroundColor: '#f8f9fa',
              borderRadius: '8px',
              maxWidth: '600px',
              margin: '1rem auto'
            }}>
              <p style={{ color: '#4263EB', fontWeight: 'bold', marginBottom: '0.5rem' }}>
                Based on your information:
              </p>
              <p style={{ color: '#666' }}>
                {selectedPlan === 'student' && 'You qualify for Student Membership (.edu email detected)'}
                {selectedPlan === 'over40' && `You qualify for Over 40 Membership (Age: ${age})`}
                {selectedPlan === 'under40' && `You qualify for Under 40 Membership (Age: ${age})`}
              </p>
            </div>
          )}
        </section>

        {/* Application Form Section */}
        <section className="application-section">
          <div className="form-container">
            {!isRegistered ? (
              <>
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

              {/* Personal Information - Move this first to determine tier */}
              <div className="form-section">
                <h3 style={{ marginBottom: '1rem', fontSize: '1.1rem', color: '#333' }}>Personal Information</h3>
                
                <div className="form-row">
                  <div className="form-group">
                    <label htmlFor="firstName">First Name *</label>
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
                    <label htmlFor="lastName">Last Name *</label>
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
                    <label htmlFor="dateOfBirth">Date of Birth *</label>
                    <input
                      type="date"
                      id="dateOfBirth"
                      name="dateOfBirth"
                      value={formData.dateOfBirth}
                      onChange={handleInputChange}
                      max={new Date().toISOString().split('T')[0]}
                      required
                      disabled={isLoading}
                    />
                    <small style={{ color: '#666', fontSize: '0.8rem' }}>
                      Used to determine membership tier
                    </small>
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

                {/* Mailing Address */}
                <div className="form-group">
                  <label htmlFor="address">Street Address</label>
                  <input
                    type="text"
                    id="address"
                    name="address"
                    value={formData.address}
                    onChange={handleInputChange}
                    placeholder="123 Main Street"
                    disabled={isLoading}
                  />
                </div>

                <div className="form-row">
                  <div className="form-group">
                    <label htmlFor="city">City</label>
                    <input
                      type="text"
                      id="city"
                      name="city"
                      value={formData.city}
                      onChange={handleInputChange}
                      placeholder="Birmingham"
                      disabled={isLoading}
                    />
                  </div>
                  <div className="form-group">
                    <label htmlFor="state">State</label>
                    <input
                      type="text"
                      id="state"
                      name="state"
                      value={formData.state}
                      onChange={handleInputChange}
                      placeholder="AL"
                      disabled={isLoading}
                    />
                  </div>
                </div>

                <div className="form-row">
                  <div className="form-group">
                    <label htmlFor="zipCode">ZIP Code</label>
                    <input
                      type="text"
                      id="zipCode"
                      name="zipCode"
                      value={formData.zipCode}
                      onChange={handleInputChange}
                      placeholder="35203"
                      disabled={isLoading}
                    />
                  </div>
                  <div className="form-group">
                    <label htmlFor="country">Country</label>
                    <select
                      id="country"
                      name="country"
                      value={formData.country}
                      onChange={(e) => setFormData(prev => ({ ...prev, country: e.target.value }))}
                      disabled={isLoading}
                    >
                      <option value="United States">United States</option>
                      <option value="Canada">Canada</option>
                      <option value="Mexico">Mexico</option>
                      <option value="United Kingdom">United Kingdom</option>
                      <option value="Other">Other</option>
                    </select>
                  </div>
                </div>
              </div>

              {/* Account Information */}
              <div className="form-section">
                <h3 style={{ marginBottom: '1rem', fontSize: '1.1rem', color: '#333' }}>Account Information</h3>
                
                <div className="form-group">
                  <label htmlFor="username">Username *</label>
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
                  <label htmlFor="email">Email *</label>
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
                  <small style={{ color: '#666', fontSize: '0.8rem' }}>
                    Students: Use your .edu email to qualify for student membership
                  </small>
                </div>

                <div className="form-row">
                  <div className="form-group">
                    <label htmlFor="password">Password *</label>
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
                    <label htmlFor="confirmPassword">Confirm Password *</label>
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

              {/* Selected Plan Display */}
              {selectedPlan && (
                <div className="selected-plan-display">
                  <label>Your Membership Tier</label>
                  <div className="selected-plan-info">
                    <span className="plan-name">
                      {selectedPlan === 'over40' && 'Over 40 Membership'}
                      {selectedPlan === 'under40' && 'Under 40 Membership'}
                      {selectedPlan === 'student' && 'Student Membership'}
                    </span>
                    <span className="plan-price">
                      {selectedPlan === 'over40' && '$300/year'}
                      {selectedPlan === 'under40' && '$200/year'}
                      {selectedPlan === 'student' && '$75/year'}
                    </span>
                  </div>
                </div>
              )}

              <button type="submit" className="submit-btn" disabled={isLoading || !selectedPlan}>
                {isLoading ? 'Creating Account...' : 'Create Account & Continue to Payment'}
              </button>

              {!selectedPlan && formData.email && formData.dateOfBirth && (
                <p style={{ color: '#666', fontSize: '0.9rem', textAlign: 'center', marginTop: '0.5rem' }}>
                  Please complete all required fields to determine your membership tier
                </p>
              )}
            </form>

                <p className="form-footer">
                  Already have an account? <a href="/login">Sign in</a>
                </p>
              </>
            ) : (
              <>
                <div className="form-header">
                  <h2>Complete Your Membership</h2>
                  <p className="form-subtitle">
                    Your account has been created successfully! Now complete your membership payment.
                  </p>
                </div>
                {selectedPlan && (
                  <CheckoutForm 
                    membershipTier={selectedPlan}
                    onError={(error) => setError(error)}
                  />
                )}
              </>
            )}
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