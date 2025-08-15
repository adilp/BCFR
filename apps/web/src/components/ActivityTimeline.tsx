import { useState, useEffect } from 'react';
import api from '../services/api';
import './ActivityTimeline.css';

export interface ActivityLog {
  id: string;
  userId: string;
  activityType: string;
  activityCategory: string;
  description: string;
  oldValue?: string;
  newValue?: string;
  ipAddress?: string;
  userAgent?: string;
  performedById?: string;
  performedBy?: {
    firstName: string;
    lastName: string;
  };
  metadata?: string;
  createdAt: string;
}

interface ActivityTimelineProps {
  userId?: string;
  showFilters?: boolean;
  limit?: number;
}

const activityIcons: Record<string, string> = {
  Authentication: '🔐',
  Profile: '👤',
  Subscription: '💳',
  Engagement: '📊',
  Administration: '⚙️',
  Communication: '✉️',
};

const activityColors: Record<string, string> = {
  Authentication: '#4263EB',
  Profile: '#28A745',
  Subscription: '#FFC833',
  Engagement: '#6B3AA0',
  Administration: '#DC3545',
  Communication: '#17A2B8',
};

function formatDate(dateString: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMs / 3600000);
  const diffDays = Math.floor(diffMs / 86400000);

  if (diffMins < 1) return 'Just now';
  if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`;
  if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
  if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
  
  return date.toLocaleDateString('en-US', { 
    month: 'short', 
    day: 'numeric', 
    year: date.getFullYear() !== now.getFullYear() ? 'numeric' : undefined 
  });
}

function ActivityTimeline({ userId, showFilters = false, limit = 20 }: ActivityTimelineProps) {
  const [activities, setActivities] = useState<ActivityLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedCategory, setSelectedCategory] = useState<string>('');
  const [searchTerm, setSearchTerm] = useState('');

  useEffect(() => {
    fetchActivities();
  }, [userId, selectedCategory]);

  const fetchActivities = async () => {
    try {
      setLoading(true);
      setError(null);

      let endpoint = userId 
        ? `/activitylog/user/${userId}?take=${limit}`
        : `/activitylog/my-activities?take=${limit}`;

      if (selectedCategory) {
        endpoint += `&activityCategory=${selectedCategory}`;
      }

      const response = await api.get(endpoint);
      setActivities(response.data);
    } catch (err) {
      console.error('Failed to fetch activities:', err);
      setError('Failed to load activity history');
    } finally {
      setLoading(false);
    }
  };

  const filteredActivities = activities.filter(activity =>
    searchTerm === '' || 
    activity.description.toLowerCase().includes(searchTerm.toLowerCase()) ||
    activity.activityType.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const parseMetadata = (metadata?: string) => {
    if (!metadata) return null;
    try {
      return JSON.parse(metadata);
    } catch {
      return null;
    }
  };

  const renderActivityDetails = (activity: ActivityLog) => {
    const metadata = parseMetadata(activity.metadata);
    
    if (activity.oldValue && activity.newValue) {
      try {
        const oldVal = JSON.parse(activity.oldValue);
        const newVal = JSON.parse(activity.newValue);
        return (
          <div className="activity-change">
            <span className="change-label">Changed from:</span>
            <span className="old-value">{typeof oldVal === 'object' ? JSON.stringify(oldVal) : oldVal}</span>
            <span className="change-label">to:</span>
            <span className="new-value">{typeof newVal === 'object' ? JSON.stringify(newVal) : newVal}</span>
          </div>
        );
      } catch {
        return null;
      }
    }

    if (metadata) {
      return (
        <div className="activity-metadata">
          {Object.entries(metadata).map(([key, value]) => (
            <div key={key} className="metadata-item">
              <span className="metadata-key">{key}:</span>
              <span className="metadata-value">{String(value)}</span>
            </div>
          ))}
        </div>
      );
    }

    return null;
  };

  if (loading) {
    return <div className="activity-timeline-loading">Loading activity history...</div>;
  }

  if (error) {
    return <div className="activity-timeline-error">{error}</div>;
  }

  return (
    <div className="activity-timeline">
      {showFilters && (
        <div className="activity-filters">
          <input
            type="text"
            placeholder="Search activities..."
            className="activity-search"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
          <select
            className="activity-category-filter"
            value={selectedCategory}
            onChange={(e) => setSelectedCategory(e.target.value)}
          >
            <option value="">All Categories</option>
            <option value="Authentication">Authentication</option>
            <option value="Profile">Profile</option>
            <option value="Subscription">Subscription</option>
            <option value="Engagement">Engagement</option>
            <option value="Administration">Administration</option>
            <option value="Communication">Communication</option>
          </select>
        </div>
      )}

      {filteredActivities.length === 0 ? (
        <div className="no-activities">No activity history available</div>
      ) : (
        <div className="timeline-items">
          {filteredActivities.map((activity) => (
            <div key={activity.id} className="timeline-item">
              <div 
                className="timeline-marker"
                style={{ backgroundColor: activityColors[activity.activityCategory] || '#6B7280' }}
              >
                {activityIcons[activity.activityCategory] || '📝'}
              </div>
              <div className="timeline-content">
                <div className="timeline-header">
                  <strong className="activity-description">{activity.description}</strong>
                  <span className="timeline-date">{formatDate(activity.createdAt)}</span>
                </div>
                <div className="timeline-meta">
                  <span className="activity-type">{activity.activityType}</span>
                  {activity.performedBy && activity.performedById !== activity.userId && (
                    <span className="performed-by">
                      by {activity.performedBy.firstName} {activity.performedBy.lastName}
                    </span>
                  )}
                  {activity.ipAddress && (
                    <span className="ip-address">IP: {activity.ipAddress}</span>
                  )}
                </div>
                {renderActivityDetails(activity)}
              </div>
            </div>
          ))}
        </div>
      )}

      {activities.length >= limit && (
        <button className="load-more-btn" onClick={() => {}}>
          Load More Activities
        </button>
      )}
    </div>
  );
}

export default ActivityTimeline;