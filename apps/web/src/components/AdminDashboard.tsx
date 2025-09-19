import { useState } from 'react';
import Navigation from './Navigation';
import UserManagement from './UserManagement';
import EventManagement from './EventManagement';
import EmailComposer from './admin/EmailComposer';
import EmailCampaignsList from './admin/EmailCampaignsList';
import EmailQueueList from './admin/EmailQueueList';
import ScheduledEmailJobsList from './admin/ScheduledEmailJobsList';
import { 
  ArrowTrendingUpIcon,
  ArrowTrendingDownIcon
} from '@heroicons/react/24/outline';
import { 
  UserIcon as UserIconSolid,
  CreditCardIcon as CreditCardIconSolid,
  CalendarDaysIcon
} from '@heroicons/react/24/solid';
import './AdminDashboard.css';

type AdminView = 'overview' | 'users' | 'events' | 'finances' | 'reports' | 'email';
type EmailSubView = 'compose' | 'campaigns' | 'queue' | 'scheduled';

function AdminDashboard() {
  const [currentView, setCurrentView] = useState<AdminView>('overview');
  const [emailView, setEmailView] = useState<EmailSubView>('compose');

  const renderContent = () => {
    switch (currentView) {
      case 'overview':
        return <DashboardOverview />;
      case 'users':
        return <UserManagement />;
      case 'email':
        return (
          <div style={{ maxWidth: '1200px' }}>
            <div className="admin-tabs" style={{ justifyContent: 'flex-start', marginTop: '-8px' }}>
              <button className={`admin-tab ${emailView === 'compose' ? 'active' : ''}`} onClick={() => setEmailView('compose')}>Compose</button>
              <button className={`admin-tab ${emailView === 'campaigns' ? 'active' : ''}`} onClick={() => setEmailView('campaigns')}>Campaigns</button>
              <button className={`admin-tab ${emailView === 'queue' ? 'active' : ''}`} onClick={() => setEmailView('queue')}>Queue</button>
              <button className={`admin-tab ${emailView === 'scheduled' ? 'active' : ''}`} onClick={() => setEmailView('scheduled')}>Scheduled</button>
            </div>
            {emailView === 'compose' && <EmailComposer />}
            {emailView === 'campaigns' && <EmailCampaignsList />}
            {emailView === 'queue' && <EmailQueueList />}
            {emailView === 'scheduled' && <ScheduledEmailJobsList />}
          </div>
        );
      case 'events':
        return <EventManagement />;
      case 'finances':
        return <div className="placeholder-content">Financial Overview - Coming Soon</div>;
      case 'reports':
        return <div className="placeholder-content">Reports & Analytics - Coming Soon</div>;
      default:
        return <DashboardOverview />;
    }
  };

  return (
    <div className="admin-page">
      <Navigation />
      
      <div className="admin-container">
        <div className="admin-header">
          <h1>Admin Dashboard</h1>
        </div>
        
        <div className="admin-tabs">
          <button 
            className={`admin-tab ${currentView === 'overview' ? 'active' : ''}`}
            onClick={() => setCurrentView('overview')}
          >
            Overview
          </button>
          <button 
            className={`admin-tab ${currentView === 'users' ? 'active' : ''}`}
            onClick={() => setCurrentView('users')}
          >
            Users
          </button>
          <button 
            className={`admin-tab ${currentView === 'email' ? 'active' : ''}`}
            onClick={() => setCurrentView('email')}
          >
            Email
          </button>
          <button 
            className={`admin-tab ${currentView === 'events' ? 'active' : ''}`}
            onClick={() => setCurrentView('events')}
          >
            Events
          </button>
          <button 
            className={`admin-tab ${currentView === 'finances' ? 'active' : ''}`}
            onClick={() => setCurrentView('finances')}
          >
            Finances
          </button>
          <button 
            className={`admin-tab ${currentView === 'reports' ? 'active' : ''}`}
            onClick={() => setCurrentView('reports')}
          >
            Reports
          </button>
        </div>
        
        <div className="admin-content">
          {renderContent()}
        </div>
      </div>
    </div>
  );
}

function DashboardOverview() {
  const stats = [
    { label: 'Total Members', value: '487', change: '+12%', color: 'blue', trending: 'up' },
    { label: 'Active Subscriptions', value: '423', change: '+8%', color: 'green', trending: 'up' },
    { label: 'Monthly Revenue', value: '$52,850', change: '+15%', color: 'purple', trending: 'up' },
    { label: 'Event Attendance', value: '89%', change: '+5%', color: 'yellow', trending: 'up' },
  ];

  const recentActivity = [
    { type: 'new_member', user: 'John Smith', time: '2 hours ago', detail: 'Joined as Individual Member' },
    { type: 'payment', user: 'Sarah Johnson', time: '5 hours ago', detail: 'Renewed Family Membership' },
    { type: 'event_rsvp', user: 'Mike Davis', time: '1 day ago', detail: 'RSVP for Annual Gala' },
    { type: 'new_member', user: 'Emily Chen', time: '2 days ago', detail: 'Joined as Student Member' },
  ];

  return (
    <div className="dashboard-overview">
      <div className="stats-grid">
        {stats.map((stat, index) => (
          <div key={index} className={`stat-card stat-${stat.color}`}>
            <div className="stat-header">
              <span className="stat-label">{stat.label}</span>
              <span className={`stat-change ${stat.change.startsWith('+') ? 'positive' : 'negative'}`}>
                {stat.trending === 'up' ? (
                  <ArrowTrendingUpIcon className="icon-xs" />
                ) : (
                  <ArrowTrendingDownIcon className="icon-xs" />
                )}
                {stat.change}
              </span>
            </div>
            <div className="stat-value">{stat.value}</div>
          </div>
        ))}
      </div>

      <div className="dashboard-grid">
        <div className="activity-card">
          <div className="card-header">
            <h3 className="card-title">Recent Activity</h3>
            <button className="btn-ghost">View All</button>
          </div>
          <div className="activity-list">
            {recentActivity.map((activity, index) => (
              <div key={index} className="activity-item">
                <div className="activity-icon">
                  {activity.type === 'new_member' && <UserIconSolid className="icon-sm" />}
                  {activity.type === 'payment' && <CreditCardIconSolid className="icon-sm" />}
                  {activity.type === 'event_rsvp' && <CalendarDaysIcon className="icon-sm" />}
                </div>
                <div className="activity-content">
                  <div className="activity-main">
                    <strong>{activity.user}</strong>
                    <span className="activity-time">{activity.time}</span>
                  </div>
                  <div className="activity-detail">{activity.detail}</div>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="chart-card">
          <div className="card-header">
            <h3 className="card-title">Membership Growth</h3>
            <div className="chart-legend">
              <div className="legend-item">
                <span className="legend-dot" style={{ backgroundColor: '#4263EB' }}></span>
                <span>2024</span>
              </div>
              <div className="legend-item">
                <span className="legend-dot" style={{ backgroundColor: '#6B3AA0' }}></span>
                <span>2023</span>
              </div>
            </div>
          </div>
          <div className="bar-chart">
            <div className="bar" style={{ height: '70%', backgroundColor: '#4263EB' }}></div>
            <div className="bar" style={{ height: '85%', backgroundColor: '#6B3AA0' }}></div>
            <div className="bar" style={{ height: '60%', backgroundColor: '#40C057' }}></div>
            <div className="bar" style={{ height: '90%', backgroundColor: '#FAB005' }}></div>
            <div className="bar" style={{ height: '75%', backgroundColor: '#15AABF' }}></div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default AdminDashboard;
