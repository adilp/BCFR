import { useState, useEffect } from 'react';
import Navigation from './Navigation';
import UserManagement from './UserManagement';
import EventManagement from './EventManagement';
import EmailComposer from './admin/EmailComposer';
import EmailCampaignsList from './admin/EmailCampaignsList';
import EmailQueueList from './admin/EmailQueueList';
import ScheduledEmailJobsList from './admin/ScheduledEmailJobsList';
import { getApiClient } from '@memberorg/api-client';
import type { AdminStats } from '@memberorg/shared';
import {
  ArrowTrendingUpIcon,
  ArrowTrendingDownIcon
} from '@heroicons/react/24/outline';
import {
  UserIcon as UserIconSolid,
  CreditCardIcon as CreditCardIconSolid,
  CalendarDaysIcon,
  UserGroupIcon,
  ChartBarIcon
} from '@heroicons/react/24/solid';
import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip, BarChart, Bar, XAxis, YAxis, CartesianGrid } from 'recharts';
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
  const [stats, setStats] = useState<AdminStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedPeriod, setSelectedPeriod] = useState<'quarter' | 'year' | 'all'>('quarter');

  useEffect(() => {
    fetchStats();
  }, [selectedPeriod]);

  const fetchStats = async () => {
    try {
      setLoading(true);
      setError(null);
      const apiClient = getApiClient();
      const data = await apiClient.getAdminStats({ period: selectedPeriod });
      setStats(data);
    } catch (err: unknown) {
      console.error('Failed to fetch stats:', err);
      setError('Failed to load dashboard statistics');
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(value);
  };

  const formatPercentage = (value: number) => {
    return `${value >= 0 ? '+' : ''}${value.toFixed(1)}%`;
  };

  if (loading) {
    return <div className="dashboard-overview"><div style={{ textAlign: 'center', padding: '40px' }}>Loading dashboard data...</div></div>;
  }

  if (error || !stats) {
    return <div className="dashboard-overview"><div style={{ textAlign: 'center', padding: '40px', color: '#f44336' }}>{error || 'No data available'}</div></div>;
  }

  const statCards = [
    {
      label: 'Active Members',
      value: stats.activeUsers.toString(),
      change: `+${stats.newUsersThisPeriod} new`,
      color: 'blue',
      trending: 'up',
      icon: <UserIconSolid className="icon-sm" />
    },
    {
      label: 'Monthly Revenue',
      value: formatCurrency(stats.monthlyRecurringRevenue),
      change: formatPercentage(stats.revenueGrowthRate),
      color: 'green',
      trending: stats.revenueGrowthRate >= 0 ? 'up' : 'down',
      icon: <CreditCardIconSolid className="icon-sm" />
    },
    {
      label: 'Event Attendance',
      value: `${stats.averageAttendanceRate.toFixed(1)}%`,
      change: formatPercentage(stats.attendanceTrend),
      color: 'yellow',
      trending: stats.attendanceTrend >= 0 ? 'up' : 'down',
      icon: <CalendarDaysIcon className="icon-sm" />
    },
    {
      label: 'RSVP Response Rate',
      value: `${stats.averageRsvpResponseRate.toFixed(1)}%`,
      change: `${stats.totalEventsHeld} events`,
      color: 'purple',
      trending: 'up',
      icon: <UserGroupIcon className="icon-sm" />
    },
    {
      label: 'Member Engagement',
      value: `${stats.averageEventsPerMember.toFixed(1)}`,
      change: 'events/member',
      color: 'orange',
      trending: 'up',
      icon: <ChartBarIcon className="icon-sm" />
    },
    {
      label: 'Churn Rate',
      value: stats.activeUsers > 0 ? `${((stats.churnedUsersThisPeriod / stats.activeUsers) * 100).toFixed(1)}%` : '0%',
      change: `${stats.churnedUsersThisPeriod} churned`,
      color: 'red',
      trending: 'down',
      icon: <UserIconSolid className="icon-sm" />
    }
  ];

  return (
    <div className="dashboard-overview">
      {/* Period Selector */}
      <div style={{ marginBottom: '20px', display: 'flex', gap: '10px', alignItems: 'center' }}>
        <label style={{ fontWeight: '600', fontSize: '14px' }}>Time Period:</label>
        <select
          value={selectedPeriod}
          onChange={(e) => setSelectedPeriod(e.target.value as typeof selectedPeriod)}
          style={{
            padding: '8px 12px',
            borderRadius: '6px',
            border: '1px solid #ddd',
            fontSize: '14px',
            cursor: 'pointer'
          }}
        >
          <option value="quarter">This Quarter</option>
          <option value="year">This Year</option>
          <option value="all">All Time</option>
        </select>
        <span style={{ marginLeft: 'auto', fontSize: '14px', color: '#666' }}>
          {stats.periodLabel}
        </span>
      </div>

      {/* Stats Grid */}
      <div className="stats-grid">
        {statCards.map((stat, index) => (
          <div key={index} className={`stat-card stat-${stat.color}`}>
            <div className="stat-header">
              <span className="stat-label">{stat.label}</span>
              <span className={`stat-change ${stat.change.startsWith('+') ? 'positive' : stat.trending === 'down' ? 'negative' : ''}`}>
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

      {/* Dashboard Grid - Top Engaged Members and Key Metrics */}
      <div className="dashboard-grid">
        <div className="activity-card">
          <div className="card-header">
            <h3 className="card-title">Top Engaged Members</h3>
          </div>
          <div className="activity-list">
            {stats.topEngagedMembers.length === 0 ? (
              <div style={{ padding: '20px', textAlign: 'center', color: '#666' }}>
                No engagement data available for this period
              </div>
            ) : (
              stats.topEngagedMembers.map((member, index) => (
                <div key={index} className="activity-item">
                  <div className="activity-icon">
                    <UserIconSolid className="icon-sm" />
                  </div>
                  <div className="activity-content">
                    <div className="activity-main">
                      <strong>{member.name}</strong>
                      <span className="activity-time">{member.eventsAttended} events</span>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        <div className="chart-card">
          <div className="card-header">
            <h3 className="card-title">Financial Overview</h3>
          </div>
          <div style={{ padding: '20px' }}>
            <div style={{ marginBottom: '15px' }}>
              <div style={{ fontSize: '14px', color: '#666', marginBottom: '5px' }}>Annual Recurring Revenue</div>
              <div style={{ fontSize: '24px', fontWeight: 'bold', color: '#4263EB' }}>
                {formatCurrency(stats.annualRecurringRevenue)}
              </div>
            </div>
            <div style={{ marginBottom: '15px' }}>
              <div style={{ fontSize: '14px', color: '#666', marginBottom: '5px' }}>Active Subscriptions</div>
              <div style={{ fontSize: '20px', fontWeight: '600' }}>
                {stats.activeSubscriptions}
              </div>
            </div>
            <div>
              <div style={{ fontSize: '14px', color: '#666', marginBottom: '10px' }}>Revenue by Tier</div>
              {Object.entries(stats.revenueByTier).map(([tier, amount]) => (
                <div key={tier} style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px', fontSize: '14px' }}>
                  <span>{tier}:</span>
                  <strong>{formatCurrency(amount)}</strong>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* Charts Section */}
      <div className="dashboard-grid" style={{ marginTop: '20px' }}>
        {/* Revenue by Tier Pie Chart */}
        <div className="chart-card">
          <div className="card-header">
            <h3 className="card-title">Revenue by Membership Tier</h3>
          </div>
          <div style={{ padding: '20px', height: '350px' }}>
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={[
                    { name: 'Individual', value: stats.revenueByTier.Individual },
                    { name: 'Family', value: stats.revenueByTier.Family },
                    { name: 'Student', value: stats.revenueByTier.Student }
                  ]}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={(props: any) => {
                    const { name, percent } = props;
                    return `${name}: ${(percent * 100).toFixed(0)}%`;
                  }}
                  outerRadius={100}
                  fill="#8884d8"
                  dataKey="value"
                >
                  <Cell fill="#4263EB" />
                  <Cell fill="#6B3AA0" />
                  <Cell fill="#FAB005" />
                </Pie>
                <Tooltip formatter={(value: number) => formatCurrency(value)} />
                <Legend />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Age Distribution Bar Chart */}
        <div className="chart-card">
          <div className="card-header">
            <h3 className="card-title">Age Distribution</h3>
          </div>
          <div style={{ padding: '20px', height: '350px' }}>
            <ResponsiveContainer width="100%" height="100%">
              <BarChart
                data={Object.entries(stats.ageDistribution).map(([ageGroup, count]) => ({
                  ageGroup,
                  count
                }))}
                margin={{ top: 5, right: 30, left: 20, bottom: 5 }}
              >
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="ageGroup" />
                <YAxis />
                <Tooltip />
                <Bar dataKey="count" fill="#6B3AA0" />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
      </div>
    </div>
  );
}

export default AdminDashboard;
