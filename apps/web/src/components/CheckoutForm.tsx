import { useState } from 'react';
import { stripePromise } from '../lib/stripe';
import { API_URL } from '../config';

interface CheckoutFormProps {
  membershipTier: string;
  onSuccess?: () => void;
  onError?: (error: string) => void;
}

const CheckoutForm = ({ membershipTier, onSuccess, onError }: CheckoutFormProps) => {
  const [isLoading, setIsLoading] = useState(false);

  const handleCheckout = async () => {
    setIsLoading(true);

    try {
      // Get auth token from localStorage
      const token = localStorage.getItem('authToken');
      if (!token) {
        throw new Error('Please log in to continue');
      }

      // Create checkout session with your API
      const response = await fetch(`${API_URL}/stripe/create-checkout-session`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          membershipTier
        })
      });

      if (!response.ok) {
        throw new Error('Failed to create checkout session');
      }

      const { sessionId, url } = await response.json();

      // Redirect to Stripe Checkout
      if (url) {
        window.location.href = url;
      } else {
        // Fallback to using Stripe.js if URL not provided
        const stripe = await stripePromise;
        if (!stripe) {
          throw new Error('Stripe failed to load');
        }

        const { error } = await stripe.redirectToCheckout({
          sessionId
        });

        if (error) {
          throw error;
        }
      }

      onSuccess?.();
    } catch (error: any) {
      console.error('Checkout error:', error);
      onError?.(error.message || 'Something went wrong');
    } finally {
      setIsLoading(false);
    }
  };

  const getTierDetails = () => {
    switch (membershipTier) {
      case 'over40':
        return { name: 'Over 40 Membership', price: '$300/year' };
      case 'under40':
        return { name: 'Under 40 Membership', price: '$200/year' };
      case 'student':
        return { name: 'Student Membership', price: '$75/year' };
      default:
        return { name: '', price: '' };
    }
  };

  const { name, price } = getTierDetails();

  return (
    <div style={{
      padding: '2rem',
      border: '1px solid #e0e0e0',
      borderRadius: '8px',
      backgroundColor: '#f9f9f9',
      marginTop: '2rem'
    }}>
      <h3 style={{ marginBottom: '1rem' }}>Complete Your Membership</h3>
      
      <div style={{
        padding: '1rem',
        backgroundColor: 'white',
        borderRadius: '4px',
        marginBottom: '1rem'
      }}>
        <p><strong>Selected Plan:</strong> {name}</p>
        <p><strong>Annual Price:</strong> {price}</p>
      </div>

      <button
        onClick={handleCheckout}
        disabled={isLoading}
        style={{
          width: '100%',
          padding: '1rem',
          backgroundColor: isLoading ? '#ccc' : '#4263EB',
          color: 'white',
          border: 'none',
          borderRadius: '4px',
          fontSize: '1rem',
          fontWeight: 'bold',
          cursor: isLoading ? 'default' : 'pointer',
          transition: 'background-color 0.2s'
        }}
      >
        {isLoading ? 'Processing...' : 'Proceed to Payment'}
      </button>

      <p style={{
        marginTop: '1rem',
        fontSize: '0.9rem',
        color: '#666',
        textAlign: 'center'
      }}>
        You will be redirected to Stripe's secure payment page
      </p>
    </div>
  );
};

export default CheckoutForm;