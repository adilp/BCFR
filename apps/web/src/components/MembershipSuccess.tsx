import { useEffect, useState } from 'react';
import { useNavigate } from '@tanstack/react-router';
import Navigation from './Navigation';

const MembershipSuccess = () => {
  const navigate = useNavigate();
  const [sessionId, setSessionId] = useState<string | null>(null);

  useEffect(() => {
    // Get session_id from URL parameters
    const urlParams = new URLSearchParams(window.location.search);
    const sessionIdParam = urlParams.get('session_id');
    setSessionId(sessionIdParam);
  }, []);

  return (
    <div style={{ minHeight: '100vh', backgroundColor: '#f8f9fa' }}>
      <Navigation />
      
      <div style={{
        maxWidth: '600px',
        margin: '4rem auto',
        padding: '2rem',
        backgroundColor: 'white',
        borderRadius: '8px',
        boxShadow: '0 2px 10px rgba(0,0,0,0.1)',
        textAlign: 'center'
      }}>
        <div style={{
          width: '80px',
          height: '80px',
          backgroundColor: '#4CAF50',
          borderRadius: '50%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          margin: '0 auto 2rem',
          fontSize: '2rem'
        }}>
          ✓
        </div>

        <h1 style={{ color: '#333', marginBottom: '1rem' }}>
          Welcome to Birmingham Council on Foreign Relations!
        </h1>
        
        <p style={{
          fontSize: '1.1rem',
          color: '#666',
          marginBottom: '2rem',
          lineHeight: '1.6'
        }}>
          Your membership has been successfully activated. 
          You now have full access to all member benefits, including exclusive events, 
          resources, and our member directory.
        </p>

        {sessionId && (
          <p style={{
            fontSize: '0.9rem',
            color: '#999',
            marginBottom: '2rem'
          }}>
            Transaction ID: {sessionId}
          </p>
        )}

        <div style={{
          padding: '1.5rem',
          backgroundColor: '#f0f7ff',
          borderRadius: '8px',
          marginBottom: '2rem'
        }}>
          <h3 style={{ color: '#4263EB', marginBottom: '1rem' }}>
            What's Next?
          </h3>
          <ul style={{
            textAlign: 'left',
            color: '#666',
            lineHeight: '1.8',
            paddingLeft: '1.5rem'
          }}>
            <li>Check your email for membership confirmation and receipt</li>
            <li>Browse upcoming events and RSVP for those that interest you</li>
            <li>Complete your member profile to connect with other members</li>
            <li>Join our member-only discussion forums</li>
          </ul>
        </div>

        <div style={{ display: 'flex', gap: '1rem', justifyContent: 'center' }}>
          <button
            onClick={() => navigate({ to: '/' })}
            style={{
              padding: '0.75rem 2rem',
              backgroundColor: '#4263EB',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              fontSize: '1rem',
              fontWeight: 'bold',
              cursor: 'pointer'
            }}
          >
            Go to Dashboard
          </button>
          
          <button
            onClick={() => navigate({ to: '/events' })}
            style={{
              padding: '0.75rem 2rem',
              backgroundColor: 'white',
              color: '#4263EB',
              border: '2px solid #4263EB',
              borderRadius: '4px',
              fontSize: '1rem',
              fontWeight: 'bold',
              cursor: 'pointer'
            }}
          >
            View Events
          </button>
        </div>
      </div>
    </div>
  );
};

export default MembershipSuccess;