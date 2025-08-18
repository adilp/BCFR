// User & Authentication Types
export interface User {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'Admin' | 'Member';
  dateOfBirth?: string;
  phone?: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  isActive: boolean;
  emailVerified?: boolean;
  memberSince?: string;
  lastLoginAt?: string;
  createdAt?: string;
  updatedAt?: string;
  dietaryRestrictions?: string[];
}

export interface UserProfile extends User {
  stripeCustomerId?: string;
  membershipTier?: string;
  subscriptionStatus?: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  dateOfBirth?: string;
  phone?: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  dietaryRestrictions?: string[];
}

export interface AuthResponse {
  token: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  expiresAt: string;
}

export interface UpdateUserProfile {
  firstName?: string;
  lastName?: string;
  email?: string;
  username?: string;
  dateOfBirth?: string;
  phone?: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  dietaryRestrictions?: string[];
}

// Event Types
export interface Event {
  id: string;
  title: string;
  description: string;
  eventDate: string;
  eventTime: string;
  endTime: string;
  location: string;
  speaker: string;
  speakerTitle?: string;
  speakerBio?: string;
  rsvpDeadline: string;
  maxAttendees?: number;
  allowPlusOne: boolean;
  status: 'draft' | 'published' | 'cancelled';
  createdBy?: string;
  createdAt?: string;
  updatedAt?: string;
  rsvpStats?: RsvpStats;
}

export interface RsvpStats {
  yes: number;
  no: number;
  pending: number;
  plusOnes: number;
}

export interface EventRsvp {
  id: string;
  eventId: string;
  userId: string;
  userName: string;
  userEmail: string;
  response: 'yes' | 'no' | 'pending';
  hasPlusOne: boolean;
  responseDate: string;
  checkedIn?: boolean;
  checkInTime?: string;
}

export interface CreateEventRequest {
  title: string;
  description: string;
  eventDate: string;
  eventTime: string;
  endTime: string;
  location: string;
  speaker: string;
  speakerTitle?: string;
  speakerBio?: string;
  rsvpDeadline: string;
  maxAttendees?: number;
  allowPlusOne: boolean;
  status: 'draft' | 'published' | 'cancelled';
}

export interface UpdateEventRequest extends Partial<CreateEventRequest> {}

export interface CreateRsvpRequest {
  response: 'yes' | 'no';
  hasPlusOne: boolean;
}

// Subscription Types
export interface Subscription {
  id: string;
  userId?: string;
  stripeSubscriptionId?: string;
  stripeCustomerId?: string;
  membershipTier: string;
  status: 'active' | 'cancelled' | 'past_due';
  amount: number;
  currentPeriodStart?: string;
  currentPeriodEnd?: string;
  nextBillingDate: string;
  startDate: string;
  endDate?: string;
  cancelAtPeriodEnd?: boolean;
  createdAt: string;
  updatedAt?: string;
}

// Admin Types
export interface AdminStats {
  totalUsers: number;
  activeUsers: number;
  newUsersThisMonth: number;
  totalRevenue: number;
  activeSubscriptions: number;
  upcomingEvents: number;
  recentActivities?: Activity[];
}

export interface Activity {
  id: string;
  userId: string;
  action: string;
  entity: string;
  entityId?: string;
  details?: string;
  ipAddress?: string;
  userAgent?: string;
  timestamp: string;
}

// Email Types
export interface EmailRequest {
  toEmails: string[];
  subject: string;
  body: string;
  isHtml: boolean;
}

export interface EmailResponse {
  success: boolean;
  message: string;
  sentCount?: number;
  failedCount?: number;
}

// Pagination Types
export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface PaginationParams {
  page?: number;
  pageSize?: number;
}

export interface UserQueryParams extends PaginationParams {
  role?: string;
  isActive?: boolean;
  search?: string;
}

export interface EventQueryParams extends PaginationParams {
  status?: 'draft' | 'published' | 'cancelled';
  upcoming?: boolean;
  past?: boolean;
}

// API Error Response
export interface ApiError {
  message: string;
  errors?: Record<string, string[]>;
  statusCode?: number;
}