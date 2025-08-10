// Application configuration
export const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001/api';
export const STRIPE_PUBLISHABLE_KEY = import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY || '';

// You can add other configuration here as needed
export const config = {
  apiUrl: API_URL,
  stripePublishableKey: STRIPE_PUBLISHABLE_KEY,
};

// Debug helper - remove in production
if (import.meta.env.DEV) {
  console.log('Config loaded:', {
    apiUrl: API_URL,
    hasStripeKey: !!STRIPE_PUBLISHABLE_KEY,
    stripeKeyPrefix: STRIPE_PUBLISHABLE_KEY.substring(0, 7) // Shows pk_test or pk_live
  });
}