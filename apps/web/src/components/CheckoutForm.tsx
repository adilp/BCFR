import { useState, useEffect } from 'react';
import { getApiClient } from '@memberorg/api-client';

interface CheckoutFormProps {
  membershipTier: string;
  onSuccess?: () => void;
  onError?: (error: string) => void;
}

interface FeeCalculation {
  basePrice: number;
  processingFee: number;
  total: number;
}

const CheckoutForm = ({ membershipTier, onSuccess, onError }: CheckoutFormProps) => {
  const [isLoading, setIsLoading] = useState(false);
  const [fees, setFees] = useState<FeeCalculation | null>(null);

  useEffect(() => {
    // Fetch fee calculation when component mounts or tier changes
    const fetchFees = async () => {
      try {
        const apiClient = getApiClient();
        const data = await apiClient.calculateFees(membershipTier);
        setFees(data);
      } catch (error) {
        console.error('Error fetching fees:', error);
      }
    };
    fetchFees();
  }, [membershipTier]);

  const handleCheckout = async () => {
    setIsLoading(true);

    try {
      const apiClient = getApiClient();
      
      // Create checkout session with API client
      const data = await apiClient.createCheckoutSession(membershipTier);

      if (!data.sessionId) {
        throw new Error('Failed to create checkout session');
      }

      // Redirect to Stripe Checkout
      if (data.checkoutUrl) {
        window.location.href = data.checkoutUrl;
      } else {
        // Fallback: construct checkout URL from session ID
        throw new Error('Checkout URL not provided by server');
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
        return { name: 'Over 40 Membership' };
      case 'under40':
        return { name: 'Under 40 Membership' };
      case 'student':
        return { name: 'Student Membership' };
      default:
        return { name: '' };
    }
  };

  const { name } = getTierDetails();

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
        {fees && (
          <>
            <div style={{ marginTop: '1rem', paddingTop: '1rem', borderTop: '1px solid #e0e0e0' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
                <span>Annual Membership:</span>
                <span>${fees.basePrice.toFixed(2)}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem', color: '#666', fontSize: '0.95rem' }}>
                <span>Processing Fee (one-time):</span>
                <span>${fees.processingFee.toFixed(2)}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', fontWeight: 'bold', paddingTop: '0.5rem', borderTop: '1px solid #e0e0e0' }}>
                <span>Total First Payment:</span>
                <span>${fees.total.toFixed(2)}</span>
              </div>
            </div>
            <p style={{ marginTop: '1rem', fontSize: '0.85rem', color: '#666', fontStyle: 'italic' }}>
              * Processing fee covers payment processing costs. This fee applies to all payments including annual renewals.
            </p>
          </>
        )}
      </div>

      <button
        onClick={handleCheckout}
        disabled={isLoading || !fees}
        style={{
          width: '100%',
          padding: '1rem',
          backgroundColor: isLoading || !fees ? '#ccc' : '#4263EB',
          color: 'white',
          border: 'none',
          borderRadius: '4px',
          fontSize: '1rem',
          fontWeight: 'bold',
          cursor: isLoading || !fees ? 'default' : 'pointer',
          transition: 'background-color 0.2s'
        }}
      >
        {isLoading ? 'Processing...' : fees ? `Pay $${fees.total.toFixed(2)} Now` : 'Loading...'}
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