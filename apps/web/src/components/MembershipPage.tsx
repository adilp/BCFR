import { useState } from 'react'
import './MembershipPage.css'

const MembershipPage = () => {
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
    console.log('Form submitted:', formData)
  }

  return (
    <div className="membership-page">
      {/* Navigation */}
      <nav className="navbar">
        <div className="nav-container">
          <a href="/" className="logo" style={{ textDecoration: 'none' }}>BCFR</a>
          <div className="nav-menu">
            <a href="/#about" className="nav-link">About</a>
            <a href="/#events" className="nav-link">Events</a>
            <a href="/membership" className="nav-link">Membership</a>
            <a href="/#contact" className="nav-link">Contact</a>
            <button className="login-btn">Member Login</button>
          </div>
        </div>
      </nav>

      <div className="membership-container">
        {/* Pricing Section */}
        <section className="pricing-section">
          <h1 className="membership-title">Choose Your Membership</h1>
          <p className="membership-subtitle">Join Birmingham's premier forum for international affairs</p>

          <div className="pricing-grid">
            <div className="pricing-card">
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

            <div className="pricing-card featured">
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

            <div className="pricing-card">
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
