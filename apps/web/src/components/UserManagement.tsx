import { useState, useEffect } from 'react';
import UserDetailsDrawer from './UserDetailsDrawer';
import AddUserDrawer from './AddUserDrawer';
import { getApiClient } from '@memberorg/api-client';
import type { AdminUser } from '@memberorg/shared';
import { formatAddress, formatDateForDisplay } from '@memberorg/shared';
import { 
  MagnifyingGlassIcon,
  ChevronRightIcon,
  ChevronDownIcon,
  PencilIcon,
  PlusIcon,
  ArrowDownTrayIcon
} from '@heroicons/react/24/outline';
import './UserManagement.css';

function UserManagement() {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [selectedUser, setSelectedUser] = useState<AdminUser | null>(null);
  const [showAddUser, setShowAddUser] = useState(false);
  const [expandedRows, setExpandedRows] = useState<Set<string>>(new Set());
  const [searchTerm, setSearchTerm] = useState('');
  const [filterRole, setFilterRole] = useState('all');
  const [filterStatus, setFilterStatus] = useState('all');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const pageSize = 20;

  useEffect(() => {
    fetchUsers();
  }, [currentPage, filterRole, filterStatus]);

  const fetchUsers = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const apiClient = getApiClient();
      const { users: fetchedUsers, totalCount } = await apiClient.getUsers({
        page: currentPage,
        pageSize,
        role: filterRole !== 'all' ? filterRole : undefined,
        isActive: filterStatus !== 'all' ? filterStatus === 'active' : undefined
      });
      
      setTotalCount(totalCount);
      
      // Debug: Log the fetched users to see subscription data
      console.log('Fetched users:', fetchedUsers);
      if (fetchedUsers.length > 0) {
        console.log('First user subscription data:', {
          membershipTier: fetchedUsers[0].membershipTier,
          subscriptionStatus: fetchedUsers[0].subscriptionStatus,
          nextBillingDate: fetchedUsers[0].nextBillingDate,
          amount: fetchedUsers[0].amount
        });
      }
      
      setUsers(fetchedUsers);
    } catch (err: any) {
      console.error('Failed to fetch users:', err);
      setError(err.message || 'Failed to load users');
    } finally {
      setLoading(false);
    }
  };

  const handleSaveUser = async (updatedUser: AdminUser) => {
    try {
      const apiClient = getApiClient();
      // The updateUser API now returns the updated user with subscription data
      const updated = await apiClient.updateUser(updatedUser.id, updatedUser);
      
      // Update the local state with the returned data
      setUsers(prevUsers => 
        prevUsers.map(u => u.id === updated.id ? updated : u)
      );
      
      setSelectedUser(null);
    } catch (err: any) {
      console.error('Failed to update user:', err);
      alert(err.message || 'Failed to update user');
    }
  };

  const toggleRowExpansion = (userId: string) => {
    const newExpanded = new Set(expandedRows);
    if (newExpanded.has(userId)) {
      newExpanded.delete(userId);
    } else {
      newExpanded.add(userId);
    }
    setExpandedRows(newExpanded);
  };

  const filteredUsers = users.filter(user => {
    const matchesSearch = searchTerm === '' || 
      user.firstName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      user.lastName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      user.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
      user.username.toLowerCase().includes(searchTerm.toLowerCase());
    
    const matchesRole = filterRole === 'all' || user.role === filterRole;
    const matchesStatus = filterStatus === 'all' || 
      (filterStatus === 'active' && user.subscriptionStatus === 'active') ||
      (filterStatus === 'inactive' && user.subscriptionStatus !== 'active');

    return matchesSearch && matchesRole && matchesStatus;
  });

  const getStatusColor = (status?: string) => {
    switch (status) {
      case 'active': return 'green';
      case 'canceled': return 'red';
      case 'past_due': return 'yellow';
      default: return 'gray';
    }
  };

  return (
    <div className="user-management">
      <div className="management-header">
        <h2 className="management-title">User Management</h2>
        <div className="management-actions">
          <button className="btn btn-primary" onClick={() => setShowAddUser(true)}>
            <PlusIcon className="icon-xs" />
            Add User
          </button>
          <button className="btn btn-secondary">
            <ArrowDownTrayIcon className="icon-xs" />
            Export
          </button>
        </div>
      </div>

      <div className="management-filters">
        <div className="search-box">
          <MagnifyingGlassIcon className="search-icon" />
          <input
            type="text"
            className="form-input"
            placeholder="Search users..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
        
        <select 
          className="form-select"
          value={filterRole}
          onChange={(e) => setFilterRole(e.target.value)}
        >
          <option value="all">All Roles</option>
          <option value="Admin">Admin</option>
          <option value="Member">Member</option>
        </select>
        
        <select
          className="form-select"
          value={filterStatus}
          onChange={(e) => setFilterStatus(e.target.value)}
        >
          <option value="all">All Status</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
        </select>
      </div>

      <div className="spreadsheet-container">
        {loading ? (
          <div style={{ textAlign: 'center', padding: '2rem' }}>
            <div>Loading users...</div>
          </div>
        ) : error ? (
          <div style={{ textAlign: 'center', padding: '2rem', color: '#DC3545' }}>
            <div>Error: {error}</div>
            <button 
              className="btn btn-primary" 
              style={{ marginTop: '1rem' }}
              onClick={() => fetchUsers()}
            >
              Retry
            </button>
          </div>
        ) : filteredUsers.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '2rem', color: '#6C757D' }}>
            No users found. Try adjusting your filters.
          </div>
        ) : (
          <table className="spreadsheet-table">
            <thead>
              <tr>
                <th></th>
                <th>Name</th>
                <th>Email</th>
                <th>Phone</th>
                <th>Role</th>
                <th>Membership</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredUsers.map((user) => (
              <>
                <tr key={user.id} className="user-row">
                  <td>
                    <button 
                      className="expand-btn"
                      onClick={() => toggleRowExpansion(user.id)}
                    >
                      {expandedRows.has(user.id) ? 
                        <ChevronDownIcon className="icon-xs" /> : 
                        <ChevronRightIcon className="icon-xs" />
                      }
                    </button>
                  </td>
                  <td className="user-name">
                    <div className="name-cell">
                      <div className="user-avatar-small">
                        {user.firstName[0]}{user.lastName[0]}
                      </div>
                      {user.firstName} {user.lastName}
                    </div>
                  </td>
                  <td>{user.email}</td>
                  <td>{user.phone || '-'}</td>
                  <td>
                    <span className={`tag tag-${user.role === 'Admin' ? 'purple' : 'blue'}`}>
                      {user.role}
                    </span>
                  </td>
                  <td>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                      {user.membershipTier ? (
                        <>
                          <span>{user.membershipTier}</span>
                          {user.stripeCustomerId?.startsWith('CHECK_') && (
                            <span 
                              className="badge" 
                              style={{ 
                                backgroundColor: '#e3f2fd', 
                                color: '#1976d2',
                                padding: '2px 6px',
                                borderRadius: '4px',
                                fontSize: '0.75rem',
                                fontWeight: '600'
                              }}
                            >
                              CHECK
                            </span>
                          )}
                        </>
                      ) : (
                        '-'
                      )}
                    </div>
                  </td>
                  <td>
                    <span className={`tag tag-${getStatusColor(user.subscriptionStatus)}`}>
                      {user.subscriptionStatus || 'N/A'}
                    </span>
                  </td>
                  <td>
                    <button 
                      className="btn btn-ghost btn-icon"
                      onClick={() => setSelectedUser(user)}
                    >
                      <PencilIcon className="icon-sm" />
                    </button>
                  </td>
                </tr>
                {expandedRows.has(user.id) && (
                  <tr className="expanded-row">
                    <td colSpan={8}>
                      <div className="expanded-content">
                        <div className="detail-grid">
                          <div className="detail-section">
                            <h4>Personal Information</h4>
                            <div className="detail-item">
                              <span className="detail-label">Username:</span>
                              <span className="detail-value">{user.username}</span>
                            </div>
                            <div className="detail-item">
                              <span className="detail-label">Date of Birth:</span>
                              <span className="detail-value">
                                {user.dateOfBirth ? formatDateForDisplay(user.dateOfBirth, { format: 'short' }) : 'Not provided'}
                              </span>
                            </div>
                            <div className="detail-item">
                              <span className="detail-label">Join Date:</span>
                              <span className="detail-value">
                                {user.createdAt ? formatDateForDisplay(user.createdAt, { format: 'short' }) : 'Unknown'}
                              </span>
                            </div>
                            <div className="detail-item">
                              <span className="detail-label">Last Login:</span>
                              <span className="detail-value">
                                {user.lastLoginAt ? formatDateForDisplay(user.lastLoginAt, { format: 'short' }) : 'Never'}
                              </span>
                            </div>
                          </div>
                          
                          <div className="detail-section">
                            <h4>Address</h4>
                            <div className="detail-item">
                              <span className="detail-label">Street:</span>
                              <span className="detail-value">{user.address || 'Not provided'}</span>
                            </div>
                            <div className="detail-item">
                              <span className="detail-label">Location:</span>
                              <span className="detail-value">
                                {formatAddress(undefined, user.city, user.state, user.zipCode) || '-'}
                              </span>
                            </div>
                            <div className="detail-item">
                              <span className="detail-label">Country:</span>
                              <span className="detail-value">{user.country || 'Not provided'}</span>
                            </div>
                          </div>
                          
                          <div className="detail-section">
                            <h4>Subscription Details</h4>
                            <div className="detail-item">
                              <span className="detail-label">Payment Method:</span>
                              <span className="detail-value">
                                {user.stripeCustomerId ? (
                                  user.stripeCustomerId.startsWith('CHECK_') ? '✓ Check' : '💳 Credit Card'
                                ) : 'N/A'}
                              </span>
                            </div>
                            <div className="detail-item">
                              <span className="detail-label">Customer ID:</span>
                              <span className="detail-value">{user.stripeCustomerId || 'N/A'}</span>
                            </div>
                            <div className="detail-item">
                              <span className="detail-label">Amount:</span>
                              <span className="detail-value">
                                {user.amount ? `$${user.amount.toFixed(2)}` : 'N/A'}
                              </span>
                            </div>
                            <div className="detail-item">
                              <span className="detail-label">Next Billing:</span>
                              <span className="detail-value">
                                {user.nextBillingDate ? formatDateForDisplay(user.nextBillingDate, { format: 'short' }) : 'N/A'}
                              </span>
                            </div>
                          </div>
                        </div>
                      </div>
                    </td>
                  </tr>
                )}
              </>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {totalCount > pageSize && (
        <div style={{ 
          display: 'flex', 
          justifyContent: 'center', 
          alignItems: 'center',
          gap: '1rem',
          padding: '1rem',
          borderTop: '1px solid #F0EBE5'
        }}>
          <button 
            className="btn btn-secondary"
            onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
            disabled={currentPage === 1}
          >
            Previous
          </button>
          <span>Page {currentPage} of {Math.ceil(totalCount / pageSize)}</span>
          <button 
            className="btn btn-secondary"
            onClick={() => setCurrentPage(p => p + 1)}
            disabled={currentPage >= Math.ceil(totalCount / pageSize)}
          >
            Next
          </button>
        </div>
      )}

      {selectedUser && (
        <UserDetailsDrawer
          user={selectedUser}
          onClose={() => setSelectedUser(null)}
          onSave={handleSaveUser}
        />
      )}

      {showAddUser && (
        <AddUserDrawer
          onClose={() => setShowAddUser(false)}
          onCreated={(created) => {
            // Prepend to current list and open details so admin can record payment
            setUsers(prev => [created, ...prev]);
            setSelectedUser(created);
          }}
        />
      )}
    </div>
  );
}

export default UserManagement;
