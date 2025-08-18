import { useNavigate } from '@tanstack/react-router';
import { useAuth } from '../contexts/AuthContext';
import { useEffect } from 'react';

interface AdminProtectedRouteProps {
  children: React.ReactNode;
}

function AdminProtectedRoute({ children }: AdminProtectedRouteProps) {
  const { isAuthenticated, isLoading, user, isAdmin } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!isLoading) {
      if (!isAuthenticated) {
        console.log('Not authenticated, redirecting to login');
        navigate({ to: '/login' });
      } else if (!isAdmin) {
        console.log('Not admin, redirecting to home');
        navigate({ to: '/' });
      }
    }
  }, [isAuthenticated, isAdmin, isLoading, navigate]);

  if (isLoading) {
    return (
      <div className="loading-container">
        <div className="loading-spinner"></div>
        <p>Loading...</p>
      </div>
    );
  }

  if (!isAuthenticated || !isAdmin) {
    return null;
  }

  return <>{children}</>;
}

export default AdminProtectedRoute;