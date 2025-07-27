import { useState } from 'react'
import './MembershipPage.css'
import Navigation from './Navigation'

const MembershipPage = () => {
  const [selectedPlan, setSelectedPlan] = useState<'individual' | 'family' | 'student'>('individual')
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: ''
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
    // Handle form submission here
    const submissionData = {
      ...formData,
      membershipPlan: selectedPlan
    }
    console.log('Form submitted:', submissionData)
    // TODO: Send to API
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
              <h2>Apply for Membership</h2>
              <p className="form-subtitle">
                Complete the form below and our membership team will review your application
              </p>
            </div>

            <form onSubmit={handleSubmit} className="membership-form">
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

              <div className="form-row">
                <div className="form-group">
                  <label htmlFor="firstName">First name</label>
                  <input
                    type="text"
                    id="firstName"
                    name="firstName"
                    value={formData.firstName}
                    onChange={handleInputChange}
                    placeholder="Enter your first name"
                    required
                  />
                </div>
                <div className="form-group">
                  <label htmlFor="lastName">Last name</label>
                  <input
                    type="text"
                    id="lastName"
                    name="lastName"
                    value={formData.lastName}
                    onChange={handleInputChange}
                    placeholder="Enter your last name"
                    required
                  />
                </div>
              </div>

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
                <label htmlFor="phone">Phone number</label>
                <input
                  type="tel"
                  id="phone"
                  name="phone"
                  value={formData.phone}
                  onChange={handleInputChange}
                  placeholder="(555) 123-4567"
                  required
                />
              </div>

              <button type="submit" className="submit-btn">
                Submit Application
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
