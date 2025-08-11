import { useState, useEffect } from 'react'
import { useNavigate } from '@tanstack/react-router'
import { useAuth } from '../contexts/AuthContext'
import Navigation from './Navigation'
import './ProfilePage.css'
import { apiClient } from '../services/api'

interface SubscriptionData {
  id: string
  status: string
  plan: string
  amount: number
  nextBillingDate: string
  startDate: string
}

interface PaymentHistory {
  id: string
  date: string
  amount: number
  description: string
  status: string
  invoiceUrl?: string
}

const ProfilePage = () => {
  const { user, isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const [isEditing, setIsEditing] = useState(false)
  const [activeTab, setActiveTab] = useState<'profile' | 'subscription' | 'payments'>('profile')
  const [subscription, setSubscription] = useState<SubscriptionData | null>(null)
  const [paymentHistory, setPaymentHistory] = useState<PaymentHistory[]>([])
  const [loading, setLoading] = useState(false)
  
  const [profileData, setProfileData] = useState({
    firstName: user?.firstName || '',
    lastName: user?.lastName || '',
    email: user?.email || '',
    phoneNumber: '',
    address: '',
    city: '',
    state: '',
    zipCode: '',
    country: 'United States'
  })

  useEffect(() => {
    if (!isAuthenticated) {
      navigate({ to: '/login' })
    }
  }, [isAuthenticated, navigate])

  useEffect(() => {
    if (user) {
      setProfileData(prev => ({
        ...prev,
        firstName: user.firstName || '',
        lastName: user.lastName || '',
        email: user.email || ''
      }))
      fetchSubscriptionData()
      fetchPaymentHistory()
    }
  }, [user])

  const fetchSubscriptionData = async () => {
    setLoading(true)
    try {
      const response = await apiClient.get('/subscription')
      setSubscription(response.data)
    } catch (error) {
      console.error('Failed to fetch subscription:', error)
      // Mock data for now
      setSubscription({
        id: 'sub_123',
        status: 'active',
        plan: 'Individual Membership',
        amount: 125,
        nextBillingDate: '2025-01-10',
        startDate: '2024-01-10'
      })
    } finally {
      setLoading(false)
    }
  }

  const fetchPaymentHistory = async () => {
    try {
      const response = await apiClient.get('/payments/history')
      setPaymentHistory(response.data)
    } catch (error) {
      console.error('Failed to fetch payment history:', error)
      // Mock data for now
      setPaymentHistory([
        {
          id: 'pay_1',
          date: '2024-12-10',
          amount: 125,
          description: 'Individual Membership - Monthly',
          status: 'completed',
          invoiceUrl: '#'
        },
        {
          id: 'pay_2',
          date: '2024-11-10',
          amount: 125,
          description: 'Individual Membership - Monthly',
          status: 'completed',
          invoiceUrl: '#'
        },
        {
          id: 'pay_3',
          date: '2024-10-10',
          amount: 125,
          description: 'Individual Membership - Monthly',
          status: 'completed',
          invoiceUrl: '#'
        }
      ])
    }
  }

  const handleProfileUpdate = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      await apiClient.put('/profile', profileData)
      setIsEditing(false)
    } catch (error) {
      console.error('Failed to update profile:', error)
    }
  }

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    setProfileData({
      ...profileData,
      [e.target.name]: e.target.value
    })
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    })
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
          <button 
            className={`tab ${activeTab === 'payments' ? 'active' : ''}`}
            onClick={() => setActiveTab('payments')}
          >
            Payment History
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
                    <label className="form-label">Phone Number</label>
                    <input
                      type="tel"
                      name="phoneNumber"
                      className="form-input"
                      value={profileData.phoneNumber}
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
                </div>

                {isEditing && (
                  <div className="form-actions">
                    <button type="submit" className="btn btn-primary">
                      Save Changes
                    </button>
                    <button 
                      type="button" 
                      className="btn btn-ghost"
                      onClick={() => setIsEditing(false)}
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
                      <span className="detail-label">Plan</span>
                      <span className="detail-value">{subscription.plan}</span>
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

                  <div className="subscription-actions">
                    <button className="btn btn-secondary">Update Payment Method</button>
                    <button className="btn btn-ghost" disabled>Cancel Subscription</button>
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

          {activeTab === 'payments' && (
            <div className="payments-section">
              <div className="card">
                <div className="card-header">
                  <h2 className="card-title">Payment History</h2>
                </div>

                {paymentHistory.length > 0 ? (
                  <div className="payments-table-container">
                    <table className="payments-table">
                      <thead>
                        <tr>
                          <th>Date</th>
                          <th>Description</th>
                          <th>Amount</th>
                          <th>Status</th>
                          <th>Invoice</th>
                        </tr>
                      </thead>
                      <tbody>
                        {paymentHistory.map(payment => (
                          <tr key={payment.id}>
                            <td>{formatDate(payment.date)}</td>
                            <td>{payment.description}</td>
                            <td>{formatCurrency(payment.amount)}</td>
                            <td>
                              <span className={`tag tag-${payment.status === 'completed' ? 'green' : 'gray'}`}>
                                {payment.status}
                              </span>
                            </td>
                            <td>
                              {payment.invoiceUrl && (
                                <a href={payment.invoiceUrl} className="invoice-link">
                                  Download
                                </a>
                              )}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <div className="empty-state">
                    <p>No payment history available.</p>
                  </div>
                )}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

export default ProfilePage