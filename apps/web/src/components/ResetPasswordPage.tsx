import { useState, useMemo } from 'react';
import Navigation from './Navigation';
import { getApiClient } from '@memberorg/api-client';
import './LoginPage.css';

const ResetPasswordPage = () => {
  // TanStack Router v1 provides useSearch at route-level; for simplicity, parse from window.location
  const token = useMemo(() => {
    if (typeof window === 'undefined') return '';
    const params = new URLSearchParams(window.location.search);
    return params.get('token') || '';
  }, []);

  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState('');

  const canSubmit = !!token && !!password && password === confirm && !submitting;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    setSubmitting(true);
    setError('');
    try {
      const api = getApiClient();
      await api.resetPassword({ token, newPassword: password });
      setSubmitted(true);
    } catch (err: any) {
      setError(err.message || 'Failed to reset password');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="login-page">
      <Navigation />
      <div className="login-container">
        <div className="login-form-section">
          <h1 className="login-title">Reset password</h1>
          {submitted ? (
            <div style={{ color: '#155724', background: '#d4edda', border: '1px solid #c3e6cb', padding: '12px 16px', borderRadius: 8, marginBottom: 16 }}>
              Your password has been updated. You can now <a href="/login">sign in</a>.
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="login-form">
              <div className="form-group">
                <label>New Password</label>
                <input
                  type="password"
                  className="form-input"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••"
                  required
                />
              </div>
              <div className="form-group">
                <label>Confirm Password</label>
                <input
                  type="password"
                  className="form-input"
                  value={confirm}
                  onChange={(e) => setConfirm(e.target.value)}
                  placeholder="••••••••"
                  required
                />
                {confirm && confirm !== password && (
                  <div style={{ color: '#842029', marginTop: 8, fontSize: '0.9rem' }}>
                    Passwords do not match
                  </div>
                )}
              </div>
              {error && (
                <div className="error-message" style={{ color: 'red', marginBottom: '1rem' }}>{error}</div>
              )}
              <button type="submit" className="signin-btn" disabled={!canSubmit}>
                {submitting ? 'Updating...' : 'Update Password'}
              </button>
            </form>
          )}
        </div>
        <div className="login-illustration" />
      </div>
    </div>
  );
};

export default ResetPasswordPage;
