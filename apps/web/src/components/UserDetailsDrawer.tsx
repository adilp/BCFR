import { useState } from 'react';
import ActivityTimeline from './ActivityTimeline';
import Drawer from './shared/Drawer';
import TabNavigation from './shared/TabNavigation';
import { FormSection, FormGroup, FormGrid } from './shared/FormSection';
import Badge, { getStatusBadgeVariant } from './shared/Badge';
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
  const [activeTab, setActiveTab] = useState('personal');
  const [newRestriction, setNewRestriction] = useState('');

  const tabs = [
    { id: 'personal', label: 'Personal Info' },
    { id: 'subscription', label: 'Subscription' },
    { id: 'activity', label: 'Activity' }
  ];

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

  const drawerFooter = (
    <>
      <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
      <button className="btn btn-primary" onClick={handleSave}>Save Changes</button>
    </>
  );

  return (
    <Drawer
      isOpen={true}
      onClose={onClose}
      title="Edit User Details"
      footer={drawerFooter}
      size="lg"
    >

      <div className="drawer-user-header">
        <div className="user-avatar-large">
          {editedUser.firstName[0]}{editedUser.lastName[0]}
        </div>
        <div className="user-header-info">
          <h3>{editedUser.firstName} {editedUser.lastName}</h3>
          <p className="user-username">@{editedUser.username}</p>
          <div className="user-badges">
            <Badge variant={getStatusBadgeVariant(editedUser.role)}>
              {editedUser.role}
            </Badge>
            {editedUser.subscriptionStatus && (
              <Badge variant={getStatusBadgeVariant(editedUser.subscriptionStatus)}>
                {editedUser.subscriptionStatus}
              </Badge>
            )}
          </div>
        </div>
      </div>

      <TabNavigation
        tabs={tabs}
        activeTab={activeTab}
        onTabChange={setActiveTab}
        variant="default"
      />

      <div className="drawer-content">
        {activeTab === 'personal' && (
          <div className="tab-content">
            <FormSection title="Basic Information">
              <FormGrid columns={2}>
                <FormGroup label="First Name">
                  <input
                    type="text"
                    className="form-input"
                    value={editedUser.firstName}
                    onChange={(e) => handleInputChange('firstName', e.target.value)}
                  />
                </FormGroup>
                <FormGroup label="Last Name">
                  <input
                    type="text"
                    className="form-input"
                    value={editedUser.lastName}
                    onChange={(e) => handleInputChange('lastName', e.target.value)}
                  />
                </FormGroup>
                <FormGroup label="Username">
                  <input
                    type="text"
                    className="form-input"
                    value={editedUser.username}
                    onChange={(e) => handleInputChange('username', e.target.value)}
                  />
                </FormGroup>
                <FormGroup label="Email">
                  <input
                    type="email"
                    className="form-input"
                    value={editedUser.email}
                    onChange={(e) => handleInputChange('email', e.target.value)}
                  />
                </FormGroup>
                <FormGroup label="Phone">
                  <input
                    type="tel"
                    className="form-input"
                    value={editedUser.phone || ''}
                    onChange={(e) => handleInputChange('phone', e.target.value)}
                  />
                </FormGroup>
                <FormGroup label="Date of Birth">
                  <input
                    type="date"
                    className="form-input"
                    value={editedUser.dateOfBirth || ''}
                    onChange={(e) => handleInputChange('dateOfBirth', e.target.value)}
                  />
                </FormGroup>
              </FormGrid>
            </FormSection>

            <FormSection title="Address">
              <FormGrid columns={2}>
                <FormGroup label="Street Address" fullWidth>
                  <input
                    type="text"
                    className="form-input"
                    value={editedUser.address || ''}
                    onChange={(e) => handleInputChange('address', e.target.value)}
                  />
                </FormGroup>
                <FormGroup label="City">
                  <input
                    type="text"
                    className="form-input"
                    value={editedUser.city || ''}
                    onChange={(e) => handleInputChange('city', e.target.value)}
                  />
                </FormGroup>
                <FormGroup label="State">
                  <input
                    type="text"
                    className="form-input"
                    value={editedUser.state || ''}
                    onChange={(e) => handleInputChange('state', e.target.value)}
                  />
                </FormGroup>
                <FormGroup label="ZIP Code">
                  <input
                    type="text"
                    className="form-input"
                    value={editedUser.zipCode || ''}
                    onChange={(e) => handleInputChange('zipCode', e.target.value)}
                  />
                </FormGroup>
                <FormGroup label="Country">
                  <input
                    type="text"
                    className="form-input"
                    value={editedUser.country || ''}
                    onChange={(e) => handleInputChange('country', e.target.value)}
                  />
                </FormGroup>
              </FormGrid>
            </FormSection>

            <FormSection title="Dietary Restrictions">
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
            </FormSection>

            <FormSection title="Account Settings">
              <FormGrid columns={2}>
                <FormGroup label="Role">
                  <select
                    className="form-select"
                    value={editedUser.role}
                    onChange={(e) => handleInputChange('role', e.target.value)}
                  >
                    <option value="Member">Member</option>
                    <option value="Admin">Admin</option>
                  </select>
                </FormGroup>
                <FormGroup label="Account Status">
                  <select
                    className="form-select"
                    value={editedUser.isActive ? 'active' : 'inactive'}
                    onChange={(e) => handleInputChange('isActive', e.target.value === 'active')}
                  >
                    <option value="active">Active</option>
                    <option value="inactive">Inactive</option>
                  </select>
                </FormGroup>
              </FormGrid>
            </FormSection>
          </div>
        )}

        {activeTab === 'subscription' && (
          <div className="tab-content">
            <FormSection title="Membership Details">
              <FormGrid columns={2}>
                <FormGroup label="Membership Tier">
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
                </FormGroup>
                <FormGroup label="Subscription Status">
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
                </FormGroup>
                <FormGroup label="Stripe Customer ID">
                  <input
                    type="text"
                    className="form-input"
                    value={editedUser.stripeCustomerId || ''}
                    onChange={(e) => handleInputChange('stripeCustomerId', e.target.value)}
                    placeholder="cus_..."
                  />
                </FormGroup>
                <FormGroup label="Amount">
                  <input
                    type="number"
                    className="form-input"
                    value={editedUser.amount || ''}
                    onChange={(e) => handleInputChange('amount', parseFloat(e.target.value))}
                    step="0.01"
                    min="0"
                  />
                </FormGroup>
                <FormGroup label="Next Billing Date">
                  <input
                    type="date"
                    className="form-input"
                    value={editedUser.nextBillingDate || ''}
                    onChange={(e) => handleInputChange('nextBillingDate', e.target.value)}
                  />
                </FormGroup>
              </FormGrid>
            </FormSection>

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

            <FormSection title="Account Actions">
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
            </FormSection>
          </div>
        )}
      </div>

    </Drawer>
  );
}

export default UserDetailsDrawer;