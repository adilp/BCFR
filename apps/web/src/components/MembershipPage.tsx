import { useState, useEffect } from 'react'
import './MembershipPage.css'
import Navigation from './Navigation'
import { useAuth } from '../contexts/AuthContext'
import CheckoutForm from './CheckoutForm'
import { calculateAge, formatForDateInput, validateEmail, validatePhone, validateZipCode, isRequired } from '@memberorg/shared'

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
    country: 'United States',
    dietaryRestrictions: [] as string[]
  })
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [age, setAge] = useState<number | null>(null)
  const [isRegistered, setIsRegistered] = useState(false)
  const [newRestriction, setNewRestriction] = useState('')
  const [payByCheck, setPayByCheck] = useState(false)
  const [showCheckInstructions, setShowCheckInstructions] = useState(false)
  
  // Field-specific error states
  const [fieldErrors, setFieldErrors] = useState<{[key: string]: string}>({})
  const [touchedFields, setTouchedFields] = useState<{[key: string]: boolean}>({})

  // Calculate age from date of birth using shared utility
  const getAge = (dob: string): number => {
    return calculateAge(dob) || 0
  }

  // Validate individual fields
  const validateField = (name: string, value: string): string => {
    switch (name) {
      case 'firstName':
        return !isRequired(value) ? 'First name is required' : ''
      case 'lastName':
        return !isRequired(value) ? 'Last name is required' : ''
      case 'email':
        if (!isRequired(value)) return 'Email is required'
        if (!validateEmail(value)) return 'Please enter a valid email address'
        return ''
      case 'username':
        if (!isRequired(value)) return 'Username is required'
        if (value.length < 3) return 'Username must be at least 3 characters'
        return ''
      case 'password':
        if (!isRequired(value)) return 'Password is required'
        if (value.length < 6) return 'Password must be at least 6 characters'
        return ''
      case 'confirmPassword':
        if (!isRequired(value)) return 'Please confirm your password'
        if (value !== formData.password) return 'Passwords do not match'
        return ''
      case 'phone':
        if (value && !validatePhone(value)) return 'Please enter a valid phone number'
        return ''
      case 'zipCode':
        if (value && !validateZipCode(value)) return 'Please enter a valid ZIP code'
        return ''
      case 'dateOfBirth':
        if (!isRequired(value)) return 'Date of birth is required'
        return ''
      default:
        return ''
    }
  }

  // Auto-select membership tier based on DOB and email
  useEffect(() => {
    // Check if email has .edu domain
    const isStudent = formData.email.toLowerCase().includes('.edu')
    
    if (isStudent) {
      setSelectedPlan('student')
    } else if (formData.dateOfBirth) {
      const userAge = getAge(formData.dateOfBirth)
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
    
    // Real-time validation only if field has been touched
    if (touchedFields[name]) {
      const error = validateField(name, value)
      setFieldErrors(prev => ({
        ...prev,
        [name]: error
      }))
      
      // Also validate confirmPassword when password changes
      if (name === 'password' && touchedFields['confirmPassword']) {
        const confirmError = validateField('confirmPassword', formData.confirmPassword)
        setFieldErrors(prev => ({
          ...prev,
          confirmPassword: confirmError
        }))
      }
    }
  }

  const handleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
    const { name, value } = e.target
    setTouchedFields(prev => ({
      ...prev,
      [name]: true
    }))
    
    const error = validateField(name, value)
    setFieldErrors(prev => ({
      ...prev,
      [name]: error
    }))
  }

  const handleAddRestriction = () => {
    if (newRestriction.trim() && !formData.dietaryRestrictions.includes(newRestriction.trim())) {
      setFormData(prev => ({
        ...prev,
        dietaryRestrictions: [...prev.dietaryRestrictions, newRestriction.trim()]
      }))
      setNewRestriction('')
    }
  }

  const handleRemoveRestriction = (index: number) => {
    setFormData(prev => ({
      ...prev,
      dietaryRestrictions: prev.dietaryRestrictions.filter((_, i) => i !== index)
    }))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')

    // Validate all fields
    const newErrors: {[key: string]: string} = {}
    const fieldsToValidate = ['firstName', 'lastName', 'email', 'username', 'password', 'confirmPassword', 'dateOfBirth']
    
    fieldsToValidate.forEach(field => {
      const error = validateField(field, formData[field as keyof typeof formData] as string)
      if (error) {
        newErrors[field] = error
      }
    })
    
    // Validate optional fields if they have values
    if (formData.phone) {
      const phoneError = validateField('phone', formData.phone)
      if (phoneError) newErrors.phone = phoneError
    }
    
    if (formData.zipCode) {
      const zipError = validateField('zipCode', formData.zipCode)
      if (zipError) newErrors.zipCode = zipError
    }

    // If there are errors, set them and mark all fields as touched
    if (Object.keys(newErrors).length > 0) {
      setFieldErrors(newErrors)
      const newTouchedFields: {[key: string]: boolean} = {}
      fieldsToValidate.forEach(field => {
        newTouchedFields[field] = true
      })
      setTouchedFields(newTouchedFields)
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
        country: formData.country || undefined,
        dietaryRestrictions: formData.dietaryRestrictions.length > 0 ? formData.dietaryRestrictions : undefined
      })
      
      // After successful registration
      setIsRegistered(true)
      
      // If paying by check, show instructions instead of Stripe checkout
      if (payByCheck) {
        setShowCheckInstructions(true)
      }
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
                      onBlur={handleBlur}
                      placeholder="Your first name"
                      required
                      disabled={isLoading}
                      style={{ borderColor: fieldErrors.firstName ? '#dc3545' : '' }}
                    />
                    {fieldErrors.firstName && (
                      <span style={{ color: '#dc3545', fontSize: '0.875rem', marginTop: '0.25rem', display: 'block' }}>
                        {fieldErrors.firstName}
                      </span>
                    )}
                  </div>
                  <div className="form-group">
                    <label htmlFor="lastName">Last Name *</label>
                    <input
                      type="text"
                      id="lastName"
                      name="lastName"
                      value={formData.lastName}
                      onChange={handleInputChange}
                      onBlur={handleBlur}
                      placeholder="Your last name"
                      required
                      disabled={isLoading}
                      style={{ borderColor: fieldErrors.lastName ? '#dc3545' : '' }}
                    />
                    {fieldErrors.lastName && (
                      <span style={{ color: '#dc3545', fontSize: '0.875rem', marginTop: '0.25rem', display: 'block' }}>
                        {fieldErrors.lastName}
                      </span>
                    )}
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
                      onBlur={handleBlur}
                      max={formatForDateInput(new Date())}
                      required
                      disabled={isLoading}
                      style={{ borderColor: fieldErrors.dateOfBirth ? '#dc3545' : '' }}
                    />
                    {fieldErrors.dateOfBirth && (
                      <span style={{ color: '#dc3545', fontSize: '0.875rem', marginTop: '0.25rem', display: 'block' }}>
                        {fieldErrors.dateOfBirth}
                      </span>
                    )}
                    {!fieldErrors.dateOfBirth && (
                      <small style={{ color: '#666', fontSize: '0.8rem' }}>
                        Used to determine membership tier
                      </small>
                    )}
                  </div>
                  <div className="form-group">
                    <label htmlFor="phone">Phone Number</label>
                    <input
                      type="tel"
                      id="phone"
                      name="phone"
                      value={formData.phone}
                      onChange={handleInputChange}
                      onBlur={handleBlur}
                      placeholder="(555) 123-4567"
                      disabled={isLoading}
                      style={{ borderColor: fieldErrors.phone ? '#dc3545' : '' }}
                    />
                    {fieldErrors.phone && (
                      <span style={{ color: '#dc3545', fontSize: '0.875rem', marginTop: '0.25rem', display: 'block' }}>
                        {fieldErrors.phone}
                      </span>
                    )}
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
                      onBlur={handleBlur}
                      placeholder="35203"
                      disabled={isLoading}
                      style={{ borderColor: fieldErrors.zipCode ? '#dc3545' : '' }}
                    />
                    {fieldErrors.zipCode && (
                      <span style={{ color: '#dc3545', fontSize: '0.875rem', marginTop: '0.25rem', display: 'block' }}>
                        {fieldErrors.zipCode}
                      </span>
                    )}
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

                {/* Dietary Restrictions */}
                <div className="form-group full-width">
                  <label htmlFor="dietaryRestrictions">Dietary Restrictions</label>
                  <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '0.5rem' }}>
                    <input
                      type="text"
                      id="dietaryRestrictions"
                      value={newRestriction}
                      onChange={(e) => setNewRestriction(e.target.value)}
                      onKeyPress={(e) => {
                        if (e.key === 'Enter') {
                          e.preventDefault()
                          handleAddRestriction()
                        }
                      }}
                      placeholder="Enter a dietary restriction"
                      disabled={isLoading}
                      style={{ flex: 1 }}
                    />
                    <button
                      type="button"
                      onClick={handleAddRestriction}
                      disabled={isLoading || !newRestriction.trim()}
                      style={{
                        padding: '0.5rem 1rem',
                        backgroundColor: '#4263EB',
                        color: 'white',
                        border: 'none',
                        borderRadius: '4px',
                        cursor: 'pointer',
                        opacity: !newRestriction.trim() ? 0.5 : 1
                      }}
                    >
                      Add
                    </button>
                  </div>
                  {formData.dietaryRestrictions.length > 0 && (
                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem' }}>
                      {formData.dietaryRestrictions.map((restriction, index) => (
                        <div
                          key={index}
                          style={{
                            display: 'inline-flex',
                            alignItems: 'center',
                            gap: '0.5rem',
                            padding: '0.25rem 0.75rem',
                            backgroundColor: '#f0f7ff',
                            border: '1px solid #4263EB',
                            borderRadius: '16px',
                            fontSize: '0.9rem'
                          }}
                        >
                          <span>{restriction}</span>
                          <button
                            type="button"
                            onClick={() => handleRemoveRestriction(index)}
                            disabled={isLoading}
                            style={{
                              background: 'none',
                              border: 'none',
                              color: '#4263EB',
                              cursor: 'pointer',
                              fontSize: '1.2rem',
                              lineHeight: 1,
                              padding: 0
                            }}
                          >
                            ×
                          </button>
                        </div>
                      ))}
                    </div>
                  )}
                  <small style={{ color: '#666', fontSize: '0.8rem' }}>
                    Add any dietary restrictions or allergies (e.g., vegetarian, gluten-free, nut allergy)
                  </small>
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
                    onBlur={handleBlur}
                    placeholder="Choose a username"
                    required
                    disabled={isLoading}
                    style={{ borderColor: fieldErrors.username ? '#dc3545' : '' }}
                  />
                  {fieldErrors.username && (
                    <span style={{ color: '#dc3545', fontSize: '0.875rem', marginTop: '0.25rem', display: 'block' }}>
                      {fieldErrors.username}
                    </span>
                  )}
                </div>

                <div className="form-group">
                  <label htmlFor="email">Email *</label>
                  <input
                    type="email"
                    id="email"
                    name="email"
                    value={formData.email}
                    onChange={handleInputChange}
                    onBlur={handleBlur}
                    placeholder="your.email@example.com"
                    required
                    disabled={isLoading}
                    style={{ borderColor: fieldErrors.email ? '#dc3545' : '' }}
                  />
                  {fieldErrors.email && (
                    <span style={{ color: '#dc3545', fontSize: '0.875rem', marginTop: '0.25rem', display: 'block' }}>
                      {fieldErrors.email}
                    </span>
                  )}
                  {!fieldErrors.email && (
                    <small style={{ color: '#666', fontSize: '0.8rem' }}>
                      Students: Use your .edu email to qualify for student membership
                    </small>
                  )}
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
                      onBlur={handleBlur}
                      placeholder="••••••••••••"
                      required
                      disabled={isLoading}
                      style={{ borderColor: fieldErrors.password ? '#dc3545' : '' }}
                    />
                    {fieldErrors.password && (
                      <span style={{ color: '#dc3545', fontSize: '0.875rem', marginTop: '0.25rem', display: 'block' }}>
                        {fieldErrors.password}
                      </span>
                    )}
                  </div>
                  <div className="form-group">
                    <label htmlFor="confirmPassword">Confirm Password *</label>
                    <input
                      type="password"
                      id="confirmPassword"
                      name="confirmPassword"
                      value={formData.confirmPassword}
                      onChange={handleInputChange}
                      onBlur={handleBlur}
                      placeholder="••••••••••••"
                      required
                      disabled={isLoading}
                      style={{ borderColor: fieldErrors.confirmPassword ? '#dc3545' : '' }}
                    />
                    {fieldErrors.confirmPassword && (
                      <span style={{ color: '#dc3545', fontSize: '0.875rem', marginTop: '0.25rem', display: 'block' }}>
                        {fieldErrors.confirmPassword}
                      </span>
                    )}
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

              {/* Payment Method Selection */}
              <div className="form-section" style={{ marginTop: '1.5rem' }}>
                <h3 style={{ marginBottom: '1rem', fontSize: '1.1rem', color: '#333' }}>Payment Method</h3>
                
                <div style={{ 
                  display: 'flex', 
                  alignItems: 'flex-start', 
                  padding: '1rem',
                  backgroundColor: '#f9f9f9',
                  borderRadius: '8px',
                  marginBottom: '1rem'
                }}>
                  <input
                    type="checkbox"
                    id="payByCheck"
                    checked={payByCheck}
                    onChange={(e) => setPayByCheck(e.target.checked)}
                    style={{ marginRight: '0.75rem', marginTop: '0.25rem' }}
                    disabled={isLoading}
                  />
                  <label htmlFor="payByCheck" style={{ cursor: 'pointer', flex: 1 }}>
                    <strong>I prefer to pay by check</strong>
                    <p style={{ margin: '0.5rem 0 0 0', fontSize: '0.9rem', color: '#666' }}>
                      Select this option if you'd like to mail a check instead of paying online. 
                      Your account will be activated once we receive and process your payment.
                    </p>
                  </label>
                </div>
              </div>

              <button type="submit" className="submit-btn" disabled={isLoading || !selectedPlan}>
                {isLoading ? 'Creating Account...' : payByCheck ? 'Create Account & Get Check Instructions' : 'Create Account & Continue to Payment'}
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
                {showCheckInstructions ? (
                  <>
                    <div className="form-header">
                      <h2>Account Created Successfully!</h2>
                      <p className="form-subtitle">
                        Please follow the instructions below to complete your membership payment by check.
                      </p>
                    </div>
                    
                    <div style={{
                      backgroundColor: '#f0f7ff',
                      border: '2px solid #4263EB',
                      borderRadius: '12px',
                      padding: '2rem',
                      marginTop: '2rem'
                    }}>
                      <h3 style={{ color: '#4263EB', marginBottom: '1rem' }}>
                        📮 Check Payment Instructions
                      </h3>
                      
                      <div style={{ marginBottom: '1.5rem' }}>
                        <p style={{ fontWeight: '600', marginBottom: '0.5rem' }}>
                          Please make your check payable to:
                        </p>
                        <div style={{ 
                          backgroundColor: 'white', 
                          padding: '1rem', 
                          borderRadius: '8px',
                          fontFamily: 'monospace',
                          fontSize: '1.1rem'
                        }}>
                          Birmingham Committee on Foreign Relations
                        </div>
                      </div>
                      
                      <div style={{ marginBottom: '1.5rem' }}>
                        <p style={{ fontWeight: '600', marginBottom: '0.5rem' }}>
                          Mail your check to:
                        </p>
                        <div style={{ 
                          backgroundColor: 'white', 
                          padding: '1rem', 
                          borderRadius: '8px',
                          fontFamily: 'monospace',
                          fontSize: '1.1rem',
                          lineHeight: '1.6'
                        }}>
                          Birmingham Committee on Foreign Relations<br />
                          P.O. Box 130003<br />
                          Birmingham, AL 35213-0003
                        </div>
                      </div>
                      
                      <div style={{ marginBottom: '1.5rem' }}>
                        <p style={{ fontWeight: '600', marginBottom: '0.5rem' }}>
                          Amount Due:
                        </p>
                        <div style={{ 
                          backgroundColor: '#FFC833', 
                          padding: '1rem', 
                          borderRadius: '8px',
                          fontSize: '1.5rem',
                          fontWeight: 'bold',
                          textAlign: 'center'
                        }}>
                          {selectedPlan === 'over40' && '$300.00'}
                          {selectedPlan === 'under40' && '$200.00'}
                          {selectedPlan === 'student' && '$75.00'}
                        </div>
                      </div>
                      
                      <div style={{ 
                        backgroundColor: '#fff9e6', 
                        border: '1px solid #FFC833',
                        padding: '1rem', 
                        borderRadius: '8px',
                        marginTop: '1rem'
                      }}>
                        <p style={{ margin: 0, fontSize: '0.9rem' }}>
                          <strong>Important:</strong> Your membership will be activated once we receive and process your check. 
                          This typically takes 5-7 business days after mailing. You will receive an email confirmation 
                          when your membership is active.
                        </p>
                      </div>
                      
                      <div style={{ marginTop: '2rem', textAlign: 'center' }}>
                        <a 
                          href="/login" 
                          className="submit-btn" 
                          style={{ 
                            display: 'inline-block', 
                            textDecoration: 'none',
                            backgroundColor: '#6B3AA0'
                          }}
                        >
                          Go to Login
                        </a>
                        <p style={{ marginTop: '1rem', fontSize: '0.9rem', color: '#666' }}>
                          You can log in to your account now, but membership features will be available after payment processing.
                        </p>
                      </div>
                    </div>
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