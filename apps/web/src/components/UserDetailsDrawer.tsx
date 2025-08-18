import { useState } from 'react';
import ActivityTimeline from './ActivityTimeline';
import './UserDetailsDrawer.css';

interface User {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  phone?: string;
  role: string;
  membershipTier?: string;
  subscriptionStatus?: string;
  joinDate?: string; // Made optional to match UserWithSubscription
  lastLogin?: string;
  isActive: boolean;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  dateOfBirth?: string;
  stripeCustomerId?: string;
  nextBillingDate?: string;
  amount?: number;
  dietaryRestrictions?: string[];
}

interface UserDetailsDrawerProps {
  user: User;
  onClose: () => void;
  onSave: (user: User) => void;
}

function UserDetailsDrawer({ user, onClose, onSave }: UserDetailsDrawerProps) {
  const [editedUser, setEditedUser] = useState<User>({ ...user });
  const [activeTab, setActiveTab] = useState<'personal' | 'subscription' | 'activity'>('personal');
  const [newRestriction, setNewRestriction] = useState('');

  const handleInputChange = (field: keyof User, value: any) => {
    setEditedUser(prev => ({ ...prev, [field]: value }));
  };

  const handleSave = () => {
    onSave(editedUser);
  };

  const handleAddRestriction = () => {
    if (newRestriction.trim()) {
      const currentRestrictions = editedUser.dietaryRestrictions || [];
      if (!currentRestrictions.includes(newRestriction.trim())) {
        setEditedUser(prev => ({
          ...prev,
          dietaryRestrictions: [...currentRestrictions, newRestriction.trim()]
        }));
        setNewRestriction('');
      }
    }
  };

  const handleRemoveRestriction = (index: number) => {
    const currentRestrictions = editedUser.dietaryRestrictions || [];
    setEditedUser(prev => ({
      ...prev,
      dietaryRestrictions: currentRestrictions.filter((_, i) => i !== index)
    }));
  };

  return (
    <>
      <div className="drawer-overlay active" onClick={onClose}></div>
      <div className="user-details-drawer open">
        <div className="drawer-header">
          <h2 className="drawer-title">Edit User Details</h2>
          <button className="drawer-close" onClick={onClose}>✕</button>
        </div>

        <div className="drawer-user-header">
          <div className="user-avatar-large">
            {editedUser.firstName[0]}{editedUser.lastName[0]}
          </div>
          <div className="user-header-info">
            <h3>{editedUser.firstName} {editedUser.lastName}</h3>
            <p className="user-username">@{editedUser.username}</p>
            <div className="user-badges">
              <span className={`tag tag-${editedUser.role === 'Admin' ? 'purple' : 'blue'}`}>
                {editedUser.role}
              </span>
              {editedUser.subscriptionStatus && (
                <span className={`tag tag-${editedUser.subscriptionStatus === 'active' ? 'green' : 'red'}`}>
                  {editedUser.subscriptionStatus}
                </span>
              )}
            </div>
          </div>
        </div>

        <div className="drawer-tabs">
          <button 
            className={`tab ${activeTab === 'personal' ? 'active' : ''}`}
            onClick={() => setActiveTab('personal')}
          >
            Personal Info
          </button>
          <button 
            className={`tab ${activeTab === 'subscription' ? 'active' : ''}`}
            onClick={() => setActiveTab('subscription')}
          >
            Subscription
          </button>
          <button 
            className={`tab ${activeTab === 'activity' ? 'active' : ''}`}
            onClick={() => setActiveTab('activity')}
          >
            Activity
          </button>
        </div>

        <div className="drawer-content">
          {activeTab === 'personal' && (
            <div className="tab-content">
              <div className="form-section">
                <h4 className="section-title">Basic Information</h4>
                <div className="form-grid">
                  <div className="form-group">
                    <label className="form-label">First Name</label>
                    <input
                      type="text"
                      className="form-input"
                      value={editedUser.firstName}
                      onChange={(e) => handleInputChange('firstName', e.target.value)}
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Last Name</label>
                    <input
                      type="text"
                      className="form-input"
                      value={editedUser.lastName}
                      onChange={(e) => handleInputChange('lastName', e.target.value)}
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Username</label>
                    <input
                      type="text"
                      className="form-input"
                      value={editedUser.username}
                      onChange={(e) => handleInputChange('username', e.target.value)}
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Email</label>
                    <input
                      type="email"
                      className="form-input"
                      value={editedUser.email}
                      onChange={(e) => handleInputChange('email', e.target.value)}
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Phone</label>
                    <input
                      type="tel"
                      className="form-input"
                      value={editedUser.phone || ''}
                      onChange={(e) => handleInputChange('phone', e.target.value)}
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Date of Birth</label>
                    <input
                      type="date"
                      className="form-input"
                      value={editedUser.dateOfBirth || ''}
                      onChange={(e) => handleInputChange('dateOfBirth', e.target.value)}
                    />
                  </div>
                </div>
              </div>

              <div className="form-section">
                <h4 className="section-title">Address</h4>
                <div className="form-grid">
                  <div className="form-group full-width">
                    <label className="form-label">Street Address</label>
                    <input
                      type="text"
                      className="form-input"
                      value={editedUser.address || ''}
                      onChange={(e) => handleInputChange('address', e.target.value)}
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">City</label>
                    <input
                      type="text"
                      className="form-input"
                      value={editedUser.city || ''}
                      onChange={(e) => handleInputChange('city', e.target.value)}
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">State</label>
                    <input
                      type="text"
                      className="form-input"
                      value={editedUser.state || ''}
                      onChange={(e) => handleInputChange('state', e.target.value)}
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">ZIP Code</label>
                    <input
                      type="text"
                      className="form-input"
                      value={editedUser.zipCode || ''}
                      onChange={(e) => handleInputChange('zipCode', e.target.value)}
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Country</label>
                    <input
                      type="text"
                      className="form-input"
                      value={editedUser.country || ''}
                      onChange={(e) => handleInputChange('country', e.target.value)}
                    />
                  </div>
                </div>
              </div>

              <div className="form-section">
                <h4 className="section-title">Dietary Restrictions</h4>
                <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '0.5rem' }}>
                  <input
                    type="text"
                    className="form-input"
                    value={newRestriction}
                    onChange={(e) => setNewRestriction(e.target.value)}
                    onKeyPress={(e) => {
                      if (e.key === 'Enter') {
                        e.preventDefault();
                        handleAddRestriction();
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
                    style={{ padding: '0.5rem 1rem' }}
                  >
                    Add
                  </button>
                </div>
                {editedUser.dietaryRestrictions && editedUser.dietaryRestrictions.length > 0 && (
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem' }}>
                    {editedUser.dietaryRestrictions.map((restriction, index) => (
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
              </div>

              <div className="form-section">
                <h4 className="section-title">Account Settings</h4>
                <div className="form-grid">
                  <div className="form-group">
                    <label className="form-label">Role</label>
                    <select
                      className="form-select"
                      value={editedUser.role}
                      onChange={(e) => handleInputChange('role', e.target.value)}
                    >
                      <option value="Member">Member</option>
                      <option value="Admin">Admin</option>
                    </select>
                  </div>
                  <div className="form-group">
                    <label className="form-label">Account Status</label>
                    <select
                      className="form-select"
                      value={editedUser.isActive ? 'active' : 'inactive'}
                      onChange={(e) => handleInputChange('isActive', e.target.value === 'active')}
                    >
                      <option value="active">Active</option>
                      <option value="inactive">Inactive</option>
                    </select>
                  </div>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'subscription' && (
            <div className="tab-content">
              <div className="form-section">
                <h4 className="section-title">Membership Details</h4>
                <div className="form-grid">
                  <div className="form-group">
                    <label className="form-label">Membership Tier</label>
                    <select
                      className="form-select"
                      value={editedUser.membershipTier || ''}
                      onChange={(e) => handleInputChange('membershipTier', e.target.value)}
                    >
                      <option value="">None</option>
                      <option value="Individual">Individual ($125/year)</option>
                      <option value="Family">Family ($200/year)</option>
                      <option value="Student">Student ($25/year)</option>
                    </select>
                  </div>
                  <div className="form-group">
                    <label className="form-label">Subscription Status</label>
                    <select
                      className="form-select"
                      value={editedUser.subscriptionStatus || ''}
                      onChange={(e) => handleInputChange('subscriptionStatus', e.target.value)}
                    >
                      <option value="">None</option>
                      <option value="active">Active</option>
                      <option value="canceled">Canceled</option>
                      <option value="past_due">Past Due</option>
                      <option value="trialing">Trialing</option>
                    </select>
                  </div>
                  <div className="form-group">
                    <label className="form-label">Stripe Customer ID</label>
                    <input
                      type="text"
                      className="form-input"
                      value={editedUser.stripeCustomerId || ''}
                      onChange={(e) => handleInputChange('stripeCustomerId', e.target.value)}
                      placeholder="cus_..."
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Amount</label>
                    <input
                      type="number"
                      className="form-input"
                      value={editedUser.amount || ''}
                      onChange={(e) => handleInputChange('amount', parseFloat(e.target.value))}
                      step="0.01"
                      min="0"
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Next Billing Date</label>
                    <input
                      type="date"
                      className="form-input"
                      value={editedUser.nextBillingDate || ''}
                      onChange={(e) => handleInputChange('nextBillingDate', e.target.value)}
                    />
                  </div>
                </div>
              </div>

              {editedUser.subscriptionStatus === 'active' && (
                <div className="subscription-actions">
                  <button className="btn btn-secondary">Cancel Subscription</button>
                  <button className="btn btn-secondary">Send Invoice</button>
                </div>
              )}
            </div>
          )}

          {activeTab === 'activity' && (
            <div className="tab-content">
              <ActivityTimeline userId={editedUser.id} showFilters={true} limit={50} />

              <div className="form-section">
                <h4 className="section-title">Account Actions</h4>
                <div className="action-buttons">
                  <button className="btn btn-secondary">Reset Password</button>
                  <button className="btn btn-secondary">Send Welcome Email</button>
                  <button className="btn btn-secondary">Export User Data</button>
                  {editedUser.isActive ? (
                    <button className="btn btn-danger">Deactivate Account</button>
                  ) : (
                    <button className="btn btn-success">Reactivate Account</button>
                  )}
                </div>
              </div>
            </div>
          )}
        </div>

        <div className="drawer-footer">
          <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
          <button className="btn btn-primary" onClick={handleSave}>Save Changes</button>
        </div>
      </div>
    </>
  );
}

export default UserDetailsDrawer;