import { useState, useMemo } from 'react';
import type { AdminUser, AdminCreateUserRequest } from '@memberorg/shared';
import { getApiClient } from '@memberorg/api-client';
import Drawer from './shared/Drawer';
import { FormSection, FormGroup, FormGrid } from './shared/FormSection';

interface AddUserDrawerProps {
  onClose: () => void;
  onCreated: (user: AdminUser) => void;
}

function AddUserDrawer({ onClose, onCreated }: AddUserDrawerProps) {
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [username, setUsername] = useState('');
  const [role, setRole] = useState<'Admin' | 'Member'>('Member');
  const [isActive, setIsActive] = useState(true);
  const [phone, setPhone] = useState('');
  const [dateOfBirth, setDateOfBirth] = useState('');
  const [address, setAddress] = useState('');
  const [city, setCity] = useState('');
  const [state, setState] = useState('');
  const [zipCode, setZipCode] = useState('');
  const [country, setCountry] = useState('United States');

  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Auto-suggest username from email prefix if username not typed
  const suggestedUsername = useMemo(() => {
    if (!email.includes('@')) return '';
    return email.split('@')[0];
  }, [email]);

  const canSubmit = firstName.trim() && lastName.trim() && email.trim() && !submitting;

  const handleSubmit = async () => {
    if (!canSubmit) return;
    setSubmitting(true);
    setError(null);
    try {
      const payload: AdminCreateUserRequest = {
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        email: email.trim(),
        username: username.trim() || suggestedUsername || undefined,
        role,
        isActive,
        phone: phone.trim() || undefined,
        dateOfBirth: dateOfBirth || undefined,
        address: address.trim() || undefined,
        city: city.trim() || undefined,
        state: state.trim() || undefined,
        zipCode: zipCode.trim() || undefined,
        country: country.trim() || undefined
      };

      const apiClient = getApiClient();
      const created = await apiClient.createUser(payload);
      onCreated(created);
      onClose();
    } catch (err: any) {
      console.error('Failed to create user:', err);
      setError(err.message || 'Failed to create user');
    } finally {
      setSubmitting(false);
    }
  };

  const footer = (
    <>
      <button className="btn btn-secondary" onClick={onClose} disabled={submitting}>Cancel</button>
      <button className="btn btn-primary" onClick={handleSubmit} disabled={!canSubmit}>
        {submitting ? 'Creating...' : 'Create User'}
      </button>
    </>
  );

  return (
    <Drawer
      isOpen={true}
      onClose={onClose}
      title="Add New User"
      footer={footer}
      size="lg"
    >
      {error && (
        <div style={{
          background: '#fde8e8',
          border: '1px solid #f5c2c7',
          color: '#842029',
          padding: '0.75rem 1rem',
          borderRadius: '6px',
          marginBottom: '1rem'
        }}>
          {error}
        </div>
      )}

      <FormSection title="Personal Information">
        <FormGrid columns={2}>
          <FormGroup label="First Name" required>
            <input className="form-input" value={firstName} onChange={(e) => setFirstName(e.target.value)} />
          </FormGroup>
          <FormGroup label="Last Name" required>
            <input className="form-input" value={lastName} onChange={(e) => setLastName(e.target.value)} />
          </FormGroup>
          <FormGroup label="Email" required>
            <input type="email" className="form-input" value={email} onChange={(e) => setEmail(e.target.value)} />
          </FormGroup>
          <FormGroup label="Username (optional)">
            <input className="form-input" placeholder={suggestedUsername || 'username'} value={username} onChange={(e) => setUsername(e.target.value)} />
          </FormGroup>
          <FormGroup label="Phone">
            <input className="form-input" value={phone} onChange={(e) => setPhone(e.target.value)} />
          </FormGroup>
          <FormGroup label="Date of Birth (optional)">
            <input type="date" className="form-input" value={dateOfBirth} onChange={(e) => setDateOfBirth(e.target.value)} />
          </FormGroup>
        </FormGrid>
      </FormSection>

      <FormSection title="Address">
        <FormGrid columns={2}>
          <FormGroup label="Street Address" fullWidth>
            <input className="form-input" value={address} onChange={(e) => setAddress(e.target.value)} />
          </FormGroup>
          <FormGroup label="City">
            <input className="form-input" value={city} onChange={(e) => setCity(e.target.value)} />
          </FormGroup>
          <FormGroup label="State">
            <input className="form-input" value={state} onChange={(e) => setState(e.target.value)} />
          </FormGroup>
          <FormGroup label="ZIP Code">
            <input className="form-input" value={zipCode} onChange={(e) => setZipCode(e.target.value)} />
          </FormGroup>
          <FormGroup label="Country">
            <input className="form-input" value={country} onChange={(e) => setCountry(e.target.value)} />
          </FormGroup>
        </FormGrid>
      </FormSection>

      <FormSection title="Account Settings">
        <FormGrid columns={2}>
          <FormGroup label="Role">
            <select className="form-select" value={role} onChange={(e) => setRole(e.target.value as 'Admin' | 'Member')}>
              <option value="Member">Member</option>
              <option value="Admin">Admin</option>
            </select>
          </FormGroup>
          <FormGroup label="Status">
            <select className="form-select" value={isActive ? 'active' : 'inactive'} onChange={(e) => setIsActive(e.target.value === 'active')}>
              <option value="active">Active</option>
              <option value="inactive">Inactive</option>
            </select>
          </FormGroup>
        </FormGrid>
      </FormSection>

      <div style={{ marginTop: '1rem', color: '#6C757D' }}>
        Note: A secure random password is generated. The user can reset it via the login screen.
      </div>
    </Drawer>
  );
}

export default AddUserDrawer;
