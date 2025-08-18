import { useState, useEffect } from 'react'
import { useAuth } from '../contexts/AuthContext'
import Navigation from './Navigation'
import './ProfilePage.css'
import { getApiClient } from '@memberorg/api-client'
import { formatDateForDisplay, type Subscription, type UpdateUserProfile } from '@memberorg/shared'



const ProfilePage = () => {
  const { user } = useAuth()
  const [isEditing, setIsEditing] = useState(false)
  const [activeTab, setActiveTab] = useState<'profile' | 'subscription'>('profile')
  const [subscription, setSubscription] = useState<Subscription | null>(null)
  // const [paymentHistory, setPaymentHistory] = useState<PaymentHistory[]>([])
  const [loading, setLoading] = useState(false)
  
  const [profileData, setProfileData] = useState({
    firstName: user?.firstName || '',
    lastName: user?.lastName || '',
    email: user?.email || '',
    username: user?.username || '',
    dateOfBirth: '',
    phone: '',
    address: '',
    city: '',
    state: '',
    zipCode: '',
    country: 'United States',
    dietaryRestrictions: [] as string[]
  })
  const [newRestriction, setNewRestriction] = useState('')

  // No need to check auth - handled by ProtectedRoute at router level
  useEffect(() => {
    if (user) {
      fetchProfileData()
      fetchSubscriptionData()
    }
  }, [user])

  const fetchProfileData = async () => {
    try {
      const apiClient = getApiClient()
      const data = await apiClient.getProfile()
      setProfileData({
        firstName: data.firstName || '',
        lastName: data.lastName || '',
        email: data.email || '',
        username: data.username || '',
        dateOfBirth: data.dateOfBirth || '',
        phone: data.phone || '',
        address: data.address || '',
        city: data.city || '',
        state: data.state || '',
        zipCode: data.zipCode || '',
        country: data.country || 'United States',
        dietaryRestrictions: data.dietaryRestrictions || []
      })
    } catch (error) {
      console.error('Failed to fetch profile:', error)
      // If profile endpoint fails, try to load basic data from auth context
      if (user) {
        setProfileData(prev => ({
          ...prev,
          firstName: user.firstName || '',
          lastName: user.lastName || '',
          email: user.email || '',
          username: user.username || ''
        }))
      }
    }
  }

  const fetchSubscriptionData = async () => {
    setLoading(true)
    try {
      const apiClient = getApiClient()
      const data = await apiClient.getSubscription()
      setSubscription(data)
    } catch (error) {
      console.error('Failed to fetch subscription:', error)
      setSubscription(null)
    } finally {
      setLoading(false)
    }
  }

  // const fetchPaymentHistory = async () => {
  //   try {
  //     const response = await apiClient.get('/payments/history')
  //     setPaymentHistory(response.data)
  //   } catch (error) {
  //     console.error('Failed to fetch payment history:', error)
  //     // Mock data for now
  //     setPaymentHistory([
  //       {
  //         id: 'pay_1',
  //         date: '2024-12-10',
  //         amount: 125,
  //         description: 'Individual Membership - Monthly',
  //         status: 'completed',
  //         invoiceUrl: '#'
  //       },
  //       {
  //         id: 'pay_2',
  //         date: '2024-11-10',
  //         amount: 125,
  //         description: 'Individual Membership - Monthly',
  //         status: 'completed',
  //         invoiceUrl: '#'
  //       },
  //       {
  //         id: 'pay_3',
  //         date: '2024-10-10',
  //         amount: 125,
  //         description: 'Individual Membership - Monthly',
  //         status: 'completed',
  //         invoiceUrl: '#'
  //       }
  //     ])
  //   }
  // }

  const handleProfileUpdate = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      const apiClient = getApiClient()
      const updateData: UpdateUserProfile = {
        firstName: profileData.firstName,
        lastName: profileData.lastName,
        email: profileData.email,
        username: profileData.username,
        dateOfBirth: profileData.dateOfBirth,
        phone: profileData.phone,
        address: profileData.address,
        city: profileData.city,
        state: profileData.state,
        zipCode: profileData.zipCode,
        country: profileData.country,
        dietaryRestrictions: profileData.dietaryRestrictions
      }
      await apiClient.updateProfile(updateData)
      setIsEditing(false)
      alert('Profile updated successfully!')
    } catch (error: any) {
      console.error('Failed to update profile:', error)
      if (error.message) {
        alert(error.message)
      } else {
        alert('Failed to update profile. Please try again.')
      }
    }
  }

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    setProfileData({
      ...profileData,
      [e.target.name]: e.target.value
    })
  }

  const handleAddRestriction = () => {
    if (newRestriction.trim() && !profileData.dietaryRestrictions.includes(newRestriction.trim())) {
      setProfileData(prev => ({
        ...prev,
        dietaryRestrictions: [...prev.dietaryRestrictions, newRestriction.trim()]
      }))
      setNewRestriction('')
    }
  }

  const handleRemoveRestriction = (index: number) => {
    setProfileData(prev => ({
      ...prev,
      dietaryRestrictions: prev.dietaryRestrictions.filter((_, i) => i !== index)
    }))
  }

  const formatDate = (dateString: string) => {
    return formatDateForDisplay(dateString, { format: 'long' })
  }

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount)
  }

  return (
    <div className="profile-page">
      <Navigation />
      
      <div className="profile-container">
        <div className="profile-header">
          <h1>My Profile</h1>
          <p className="profile-subtitle">Manage your account and subscription</p>
        </div>

        <div className="tabs">
          <button 
            className={`tab ${activeTab === 'profile' ? 'active' : ''}`}
            onClick={() => setActiveTab('profile')}
          >
            Profile Information
          </button>
          <button 
            className={`tab ${activeTab === 'subscription' ? 'active' : ''}`}
            onClick={() => setActiveTab('subscription')}
          >
            Subscription
          </button>
        </div>

        <div className="tab-content">
          {activeTab === 'profile' && (
            <div className="profile-section card">
              <div className="card-header">
                <h2 className="card-title">Personal Information</h2>
                {!isEditing && (
                  <button 
                    className="btn btn-secondary"
                    onClick={() => setIsEditing(true)}
                  >
                    Edit Profile
                  </button>
                )}
              </div>

              <form onSubmit={handleProfileUpdate}>
                <div className="form-grid">
                  <div className="form-group">
                    <label className="form-label">First Name</label>
                    <input
                      type="text"
                      name="firstName"
                      className="form-input"
                      value={profileData.firstName}
                      onChange={handleInputChange}
                      disabled={!isEditing}
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label">Last Name</label>
                    <input
                      type="text"
                      name="lastName"
                      className="form-input"
                      value={profileData.lastName}
                      onChange={handleInputChange}
                      disabled={!isEditing}
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label">Username</label>
                    <input
                      type="text"
                      name="username"
                      className="form-input"
                      value={profileData.username}
                      onChange={handleInputChange}
                      disabled={!isEditing}
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label">Email</label>
                    <input
                      type="email"
                      name="email"
                      className="form-input"
                      value={profileData.email}
                      onChange={handleInputChange}
                      disabled={!isEditing}
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label">Date of Birth</label>
                    <input
                      type="date"
                      name="dateOfBirth"
                      className="form-input"
                      value={profileData.dateOfBirth}
                      onChange={handleInputChange}
                      disabled={!isEditing}
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label">Phone Number</label>
                    <input
                      type="tel"
                      name="phone"
                      className="form-input"
                      value={profileData.phone}
                      onChange={handleInputChange}
                      disabled={!isEditing}
                      placeholder="(555) 123-4567"
                    />
                  </div>

                  <div className="form-group full-width">
                    <label className="form-label">Address</label>
                    <input
                      type="text"
                      name="address"
                      className="form-input"
                      value={profileData.address}
                      onChange={handleInputChange}
                      disabled={!isEditing}
                      placeholder="123 Main Street"
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label">City</label>
                    <input
                      type="text"
                      name="city"
                      className="form-input"
                      value={profileData.city}
                      onChange={handleInputChange}
                      disabled={!isEditing}
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label">State</label>
                    <input
                      type="text"
                      name="state"
                      className="form-input"
                      value={profileData.state}
                      onChange={handleInputChange}
                      disabled={!isEditing}
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label">ZIP Code</label>
                    <input
                      type="text"
                      name="zipCode"
                      className="form-input"
                      value={profileData.zipCode}
                      onChange={handleInputChange}
                      disabled={!isEditing}
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label">Country</label>
                    <select
                      name="country"
                      className="form-select"
                      value={profileData.country}
                      onChange={handleInputChange}
                      disabled={!isEditing}
                    >
                      <option value="United States">United States</option>
                      <option value="Canada">Canada</option>
                      <option value="Mexico">Mexico</option>
                    </select>
                  </div>

                  <div className="form-group full-width">
                    <label className="form-label">Dietary Restrictions</label>
                    {isEditing ? (
                      <>
                        <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '0.5rem' }}>
                          <input
                            type="text"
                            className="form-input"
                            value={newRestriction}
                            onChange={(e) => setNewRestriction(e.target.value)}
                            onKeyPress={(e) => {
                              if (e.key === 'Enter') {
                                e.preventDefault()
                                handleAddRestriction()
                              }
                            }}
                            placeholder="Enter a dietary restriction"
                            style={{ flex: 1 }}
                          />
                          <button
                            type="button"
                            onClick={handleAddRestriction}
                            className="btn btn-secondary"
                            disabled={!newRestriction.trim()}
                          >
                            Add
                          </button>
                        </div>
                        {profileData.dietaryRestrictions.length > 0 && (
                          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem' }}>
                            {profileData.dietaryRestrictions.map((restriction, index) => (
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
                      </>
                    ) : (
                      <div style={{ padding: '0.5rem 0' }}>
                        {profileData.dietaryRestrictions.length > 0 ? (
                          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem' }}>
                            {profileData.dietaryRestrictions.map((restriction, index) => (
                              <span
                                key={index}
                                style={{
                                  display: 'inline-block',
                                  padding: '0.25rem 0.75rem',
                                  backgroundColor: '#f0f7ff',
                                  border: '1px solid #4263EB',
                                  borderRadius: '16px',
                                  fontSize: '0.9rem'
                                }}
                              >
                                {restriction}
                              </span>
                            ))}
                          </div>
                        ) : (
                          <span style={{ color: '#999' }}>No dietary restrictions specified</span>
                        )}
                      </div>
                    )}
                  </div>
                </div>

                {isEditing && (
                  <div className="form-actions">
                    <button type="submit" className="btn btn-primary">
                      Save Changes
                    </button>
                    <button 
                      type="button" 
                      className="btn btn-ghost"
                      onClick={() => {
                        setIsEditing(false)
                        fetchProfileData() // Reset form to original data
                      }}
                    >
                      Cancel
                    </button>
                  </div>
                )}
              </form>
            </div>
          )}

          {activeTab === 'subscription' && (
            <div className="subscription-section">
              {loading ? (
                <div className="loading-state">Loading subscription details...</div>
              ) : subscription ? (
                <div className="card">
                  <div className="card-header">
                    <h2 className="card-title">Current Subscription</h2>
                    <span className={`tag tag-${subscription.status === 'active' ? 'green' : 'gray'}`}>
                      {subscription.status.charAt(0).toUpperCase() + subscription.status.slice(1)}
                    </span>
                  </div>
                  
                  <div className="subscription-details">
                    <div className="detail-row">
                      <span className="detail-label">Membership Tier</span>
                      <span className="detail-value">{subscription.membershipTier}</span>
                    </div>
                    <div className="detail-row">
                      <span className="detail-label">Amount</span>
                      <span className="detail-value">{formatCurrency(subscription.amount)}/year</span>
                    </div>
                    <div className="detail-row">
                      <span className="detail-label">Member Since</span>
                      <span className="detail-value">{formatDate(subscription.startDate)}</span>
                    </div>
                    <div className="detail-row">
                      <span className="detail-label">Next Billing Date</span>
                      <span className="detail-value">{formatDate(subscription.nextBillingDate)}</span>
                    </div>
                  </div>
                </div>
              ) : (
                <div className="card">
                  <div className="empty-state">
                    <h3>No Active Subscription</h3>
                    <p>You don't have an active subscription.</p>
                    <button 
                      className="btn btn-primary"
                      onClick={() => navigate({ to: '/membership' })}
                    >
                      View Membership Plans
                    </button>
                  </div>
                </div>
              )}
            </div>
          )}

        </div>
      </div>
    </div>
  )
}

export default ProfilePage