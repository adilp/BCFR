import { useState } from 'react';
import UserDetailsDrawer from './UserDetailsDrawer';
import { 
  MagnifyingGlassIcon,
  ChevronRightIcon,
  ChevronDownIcon,
  PencilIcon,
  PlusIcon,
  ArrowDownTrayIcon
} from '@heroicons/react/24/outline';
import './UserManagement.css';

interface User {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  phone?: string;
  role: string;
  membershipTier?: string;
  subscriptionStatus?: string;
  joinDate: string;
  lastLogin?: string;
  isActive: boolean;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  dateOfBirth?: string;
  stripeCustomerId?: string;
  nextBillingDate?: string;
  amount?: number;
}

const mockUsers: User[] = [
  {
    id: '1',
    username: 'jsmith',
    email: 'john.smith@example.com',
    firstName: 'John',
    lastName: 'Smith',
    phone: '(555) 123-4567',
    role: 'Member',
    membershipTier: 'Individual',
    subscriptionStatus: 'active',
    joinDate: '2023-01-15',
    lastLogin: '2024-01-10',
    isActive: true,
    address: '123 Main St',
    city: 'New York',
    state: 'NY',
    zipCode: '10001',
    country: 'USA',
    dateOfBirth: '1985-06-15',
    stripeCustomerId: 'cus_abc123',
    nextBillingDate: '2024-02-15',
    amount: 125.00
  },
  {
    id: '2',
    username: 'sjohnson',
    email: 'sarah.johnson@example.com',
    firstName: 'Sarah',
    lastName: 'Johnson',
    phone: '(555) 234-5678',
    role: 'Member',
    membershipTier: 'Family',
    subscriptionStatus: 'active',
    joinDate: '2022-06-20',
    lastLogin: '2024-01-09',
    isActive: true,
    address: '456 Oak Ave',
    city: 'Los Angeles',
    state: 'CA',
    zipCode: '90001',
    country: 'USA',
    dateOfBirth: '1990-03-22',
    stripeCustomerId: 'cus_def456',
    nextBillingDate: '2024-02-20',
    amount: 200.00
  },
  {
    id: '3',
    username: 'mdavis',
    email: 'mike.davis@example.com',
    firstName: 'Mike',
    lastName: 'Davis',
    phone: '(555) 345-6789',
    role: 'Admin',
    membershipTier: 'Individual',
    subscriptionStatus: 'active',
    joinDate: '2021-11-05',
    lastLogin: '2024-01-11',
    isActive: true,
    address: '789 Pine St',
    city: 'Chicago',
    state: 'IL',
    zipCode: '60601',
    country: 'USA',
    dateOfBirth: '1978-09-10',
    stripeCustomerId: 'cus_ghi789',
    nextBillingDate: '2024-02-05',
    amount: 125.00
  },
  {
    id: '4',
    username: 'echen',
    email: 'emily.chen@student.edu',
    firstName: 'Emily',
    lastName: 'Chen',
    role: 'Member',
    membershipTier: 'Student',
    subscriptionStatus: 'active',
    joinDate: '2023-09-01',
    lastLogin: '2024-01-08',
    isActive: true,
    city: 'Boston',
    state: 'MA',
    zipCode: '02101',
    country: 'USA',
    dateOfBirth: '2001-12-05',
    stripeCustomerId: 'cus_jkl012',
    nextBillingDate: '2024-02-01',
    amount: 25.00
  },
  {
    id: '5',
    username: 'rwilson',
    email: 'robert.wilson@example.com',
    firstName: 'Robert',
    lastName: 'Wilson',
    phone: '(555) 456-7890',
    role: 'Member',
    membershipTier: 'Individual',
    subscriptionStatus: 'canceled',
    joinDate: '2022-03-10',
    lastLogin: '2023-12-15',
    isActive: false,
    address: '321 Elm St',
    city: 'Houston',
    state: 'TX',
    zipCode: '77001',
    country: 'USA',
    dateOfBirth: '1982-07-18',
    stripeCustomerId: 'cus_mno345',
    amount: 125.00
  }
];

function UserManagement() {
  const [users] = useState<User[]>(mockUsers);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [expandedRows, setExpandedRows] = useState<Set<string>>(new Set());
  const [searchTerm, setSearchTerm] = useState('');
  const [filterRole, setFilterRole] = useState('all');
  const [filterStatus, setFilterStatus] = useState('all');

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
          <button className="btn btn-primary">
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
                  <td>{user.membershipTier || '-'}</td>
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
                              <span className="detail-value">{user.dateOfBirth || 'Not provided'}</span>
                            </div>
                            <div className="detail-item">
                              <span className="detail-label">Join Date:</span>
                              <span className="detail-value">{user.joinDate}</span>
                            </div>
                            <div className="detail-item">
                              <span className="detail-label">Last Login:</span>
                              <span className="detail-value">{user.lastLogin || 'Never'}</span>
                            </div>
                          </div>
                          
                          <div className="detail-section">
                            <h4>Address</h4>
                            <div className="detail-item">
                              <span className="detail-label">Street:</span>
                              <span className="detail-value">{user.address || 'Not provided'}</span>
                            </div>
                            <div className="detail-item">
                              <span className="detail-label">City, State:</span>
                              <span className="detail-value">
                                {user.city || '-'}, {user.state || '-'} {user.zipCode || ''}
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
                              <span className="detail-label">Stripe ID:</span>
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
                              <span className="detail-value">{user.nextBillingDate || 'N/A'}</span>
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
      </div>

      {selectedUser && (
        <UserDetailsDrawer
          user={selectedUser}
          onClose={() => setSelectedUser(null)}
          onSave={(updatedUser) => {
            console.log('Saving user:', updatedUser);
            setSelectedUser(null);
          }}
        />
      )}
    </div>
  );
}

export default UserManagement;