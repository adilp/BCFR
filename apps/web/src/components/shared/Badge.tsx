import { ReactNode } from 'react';
import './Badge.css';

type BadgeVariant = 'default' | 'success' | 'warning' | 'error' | 'info' | 'purple' | 'blue';
type BadgeSize = 'sm' | 'md' | 'lg';

interface BadgeProps {
  children: ReactNode;
  variant?: BadgeVariant;
  size?: BadgeSize;
  dot?: boolean;
  className?: string;
}

function Badge({ 
  children, 
  variant = 'default', 
  size = 'md', 
  dot = false,
  className = '' 
}: BadgeProps) {
  return (
    <span className={`badge badge-${variant} badge-${size} ${className}`}>
      {dot && <span className="badge-dot" />}
      {children}
    </span>
  );
}

// Helper function to get badge variant based on common status values
export function getStatusBadgeVariant(status: string): BadgeVariant {
  const statusMap: Record<string, BadgeVariant> = {
    // User statuses
    'active': 'success',
    'inactive': 'default',
    'suspended': 'error',
    'pending': 'warning',
    
    // Subscription statuses
    'trialing': 'info',
    'past_due': 'warning',
    'canceled': 'error',
    'cancelled': 'error',
    
    // Event statuses
    'draft': 'default',
    'published': 'success',
    
    // RSVP statuses
    'attending': 'success',
    'not_attending': 'error',
    'maybe': 'warning',
    
    // Roles
    'Admin': 'purple',
    'Member': 'blue',
  };
  
  return statusMap[status] || 'default';
}

export default Badge;