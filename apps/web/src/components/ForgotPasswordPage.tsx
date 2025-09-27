import { useState } from 'react';
import Navigation from './Navigation';
import { getApiClient } from '@memberorg/api-client';
import './LoginPage.css';

const ForgotPasswordPage = () => {
  const [emailOrUsername, setEmailOrUsername] = useState('');
  const [submitted, setSubmitted] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);
    try {
      const api = getApiClient();
      await api.forgotPassword({ emailOrUsername });
      setSubmitted(true);
    } catch (err: any) {
      // Always show success-style message; avoid enumeration
      setSubmitted(true);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="login-page">
      <Navigation />
      <div className="login-container">
        <div className="login-form-section">
          <h1 className="login-title">Forgot password</h1>
          {submitted ? (
            <div style={{ color: '#155724', background: '#d4edda', border: '1px solid #c3e6cb', padding: '12px 16px', borderRadius: 8, marginBottom: 16 }}>
              If an account exists for that identifier, a password reset email has been sent.
            </div>
          ) : null}
          <form onSubmit={handleSubmit} className="login-form">
            {!submitted && (
              <>
                <div className="form-group">
                  <label htmlFor="emailOrUsername">Username or Email</label>
                  <input
                    id="emailOrUsername"
                    type="text"
                    className="form-input"
                    value={emailOrUsername}
                    onChange={(e) => setEmailOrUsername(e.target.value)}
                    placeholder="Enter your username or email"
                    required
                    disabled={isLoading}
                  />
                </div>
                {error && (
                  <div className="error-message" style={{ color: 'red', marginBottom: '1rem' }}>{error}</div>
                )}
                <button type="submit" className="signin-btn" disabled={isLoading}>
                  {isLoading ? 'Sending...' : 'Send reset link'}
                </button>
              </>
            )}
          </form>
        </div>
        <div className="login-illustration" />
      </div>
    </div>
  );
};

export default ForgotPasswordPage;

