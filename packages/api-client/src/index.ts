import type { 
  User,
  UserProfile,
  AdminUser,
  LoginRequest,
  RegisterRequest,
  AuthResponse,
  UpdateUserProfile,
  Event,
  EventRsvp,
  CreateEventRequest,
  UpdateEventRequest,
  CreateRsvpRequest,
  Subscription,
  AdminStats,
  Activity,
  EmailRequest,
  EmailResponse,
  UserQueryParams,
  EventQueryParams,
  ApiError
} from '@memberorg/shared';

export interface ApiClientOptions {
  baseURL: string;
  getAuthToken?: () => string | null;
  onAuthError?: () => void;
}

export class ApiClient {
  private baseURL: string;
  private getAuthToken?: () => string | null;
  private onAuthError?: () => void;

  constructor(options: ApiClientOptions) {
    this.baseURL = options.baseURL;
    this.getAuthToken = options.getAuthToken;
    this.onAuthError = options.onAuthError;
  }

  private async request<T>(
    path: string,
    options: RequestInit = {}
  ): Promise<T> {
    const token = this.getAuthToken?.();
    
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };
    
    // Merge any existing headers
    if (options.headers) {
      Object.assign(headers, options.headers);
    }

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const response = await fetch(`${this.baseURL}${path}`, {
      ...options,
      headers,
    });

    if (response.status === 401) {
      this.onAuthError?.();
      throw new Error('Unauthorized');
    }

    if (!response.ok) {
      let error: ApiError;
      try {
        error = await response.json();
      } catch {
        error = { 
          message: `Request failed with status ${response.status}`,
          statusCode: response.status 
        };
      }
      throw error;
    }

    // Handle 204 No Content responses
    if (response.status === 204) {
      return {} as T;
    }

    return response.json();
  }

  // Authentication endpoints
  async login(credentials: LoginRequest): Promise<AuthResponse> {
    return this.request<AuthResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify(credentials),
    });
  }

  async register(data: RegisterRequest): Promise<AuthResponse> {
    return this.request<AuthResponse>('/auth/register', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async logout(): Promise<void> {
    return this.request<void>('/auth/logout', {
      method: 'POST',
    });
  }

  // Profile endpoints
  async getProfile(): Promise<UserProfile> {
    return this.request<UserProfile>('/profile');
  }

  async updateProfile(data: UpdateUserProfile): Promise<void> {
    return this.request<void>('/profile', {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  async getSubscription(): Promise<Subscription | null> {
    try {
      return await this.request<Subscription>('/profile/subscription');
    } catch (error: any) {
      // Only return null for 404 (no subscription found)
      // Re-throw other errors so they can be handled properly
      if (error.statusCode === 404) {
        return null;
      }
      console.error('Error fetching subscription:', error);
      // For now, return null for all errors to maintain backward compatibility
      // but log the error so we can debug
      return null;
    }
  }

  // Event endpoints
  async getEvents(params?: EventQueryParams): Promise<Event[]> {
    const queryParams = new URLSearchParams();
    if (params?.status) queryParams.append('status', params.status);
    if (params?.upcoming !== undefined) queryParams.append('upcoming', params.upcoming.toString());
    if (params?.past !== undefined) queryParams.append('past', params.past.toString());
    if (params?.page) queryParams.append('page', params.page.toString());
    if (params?.pageSize) queryParams.append('pageSize', params.pageSize.toString());
    
    const query = queryParams.toString();
    return this.request<Event[]>(`/events${query ? `?${query}` : ''}`);
  }

  async getEvent(id: string): Promise<Event> {
    return this.request<Event>(`/events/${id}`);
  }

  async createEvent(data: CreateEventRequest): Promise<Event> {
    return this.request<Event>('/events', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async updateEvent(id: string, data: UpdateEventRequest): Promise<void> {
    return this.request<void>(`/events/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  async deleteEvent(id: string): Promise<void> {
    return this.request<void>(`/events/${id}`, {
      method: 'DELETE',
    });
  }

  async getEventRsvps(eventId: string): Promise<EventRsvp[]> {
    return this.request<EventRsvp[]>(`/events/${eventId}/rsvps`);
  }

  async getMyRsvp(eventId: string): Promise<EventRsvp | null> {
    try {
      return await this.request<EventRsvp>(`/events/${eventId}/my-rsvp`);
    } catch (error: any) {
      // 404 is expected when user hasn't RSVP'd yet - return null silently
      if (error.statusCode === 404) {
        return null;
      }
      // Log other unexpected errors
      if (error.statusCode !== 404) {
        console.error(`Error fetching RSVP for event ${eventId}:`, error);
      }
      // Return null for all errors to maintain backward compatibility
      return null;
    }
  }

  async createRsvp(eventId: string, data: CreateRsvpRequest): Promise<EventRsvp> {
    return this.request<EventRsvp>(`/events/${eventId}/rsvp`, {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  // Admin endpoints
  async getUsers(params?: UserQueryParams): Promise<{ users: AdminUser[]; totalCount: number }> {
    const queryParams = new URLSearchParams();
    if (params?.page) queryParams.append('page', params.page.toString());
    if (params?.pageSize) queryParams.append('pageSize', params.pageSize.toString());
    if (params?.role) queryParams.append('role', params.role);
    if (params?.isActive !== undefined) queryParams.append('isActive', params.isActive.toString());
    if (params?.search) queryParams.append('search', params.search);
    
    const query = queryParams.toString();
    const response = await fetch(`${this.baseURL}/admin/users${query ? `?${query}` : ''}`, {
      headers: {
        'Authorization': `Bearer ${this.getAuthToken?.()}`,
      },
    });
    
    const totalCount = response.headers.get('x-total-count');
    const users = await response.json();
    
    return {
      users,
      totalCount: totalCount ? parseInt(totalCount) : users.length,
    };
  }

  async getUser(id: string): Promise<AdminUser> {
    return this.request<AdminUser>(`/admin/users/${id}`);
  }

  async updateUser(id: string, data: Partial<User>): Promise<AdminUser> {
    return this.request<AdminUser>(`/admin/users/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  async updateUserRole(id: string, role: string): Promise<void> {
    return this.request<void>(`/admin/users/${id}/role`, {
      method: 'PUT',
      body: JSON.stringify({ role }),
    });
  }

  async deleteUser(id: string): Promise<void> {
    return this.request<void>(`/admin/users/${id}`, {
      method: 'DELETE',
    });
  }

  async recordCheckPayment(
    userId: string, 
    data: { 
      membershipTier: string; 
      amount: number; 
      startDate: string; 
    }
  ): Promise<AdminUser> {
    return this.request<AdminUser>(`/admin/users/${userId}/check-payment`, {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async getAdminStats(): Promise<AdminStats> {
    return this.request<AdminStats>('/admin/stats');
  }

  // Activity log endpoints
  async getUserActivities(userId: string, params?: { take?: number; activityCategory?: string }): Promise<Activity[]> {
    const queryParams = new URLSearchParams();
    if (params?.take) queryParams.append('take', params.take.toString());
    if (params?.activityCategory) queryParams.append('activityCategory', params.activityCategory);
    const query = queryParams.toString();
    return this.request<Activity[]>(`/activitylog/user/${userId}${query ? `?${query}` : ''}`);
  }

  async getMyActivities(params?: { take?: number; activityCategory?: string }): Promise<Activity[]> {
    const queryParams = new URLSearchParams();
    if (params?.take) queryParams.append('take', params.take.toString());
    if (params?.activityCategory) queryParams.append('activityCategory', params.activityCategory);
    const query = queryParams.toString();
    return this.request<Activity[]>(`/activitylog/my-activities${query ? `?${query}` : ''}`);
  }

  async getRecentActivities(): Promise<Activity[]> {
    return this.request<Activity[]>('/activitylog/recent');
  }

  // Email endpoints
  async sendBroadcastEmail(data: EmailRequest): Promise<EmailResponse> {
    return this.request<EmailResponse>('/adminemail/send', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async sendEventReminderToNonRsvps(eventId: string): Promise<EmailResponse> {
    return this.request<EmailResponse>(`/events/${eventId}/remind-non-rsvps`, {
      method: 'POST',
    });
  }

  // Email queue/campaigns/scheduling (admin)
  async getEmailCampaigns(): Promise<any[]> {
    return this.request<any[]>('/admin/emails/campaigns');
  }

  async getEmailCampaign(id: string): Promise<any> {
    return this.request<any>(`/admin/emails/campaigns/${id}`);
  }

  async getEmailQueue(params?: { status?: string; take?: number }): Promise<any[]> {
    const qp = new URLSearchParams();
    if (params?.status) qp.append('status', params.status);
    if (params?.take) qp.append('take', params.take.toString());
    const query = qp.toString();
    return this.request<any[]>(`/admin/emails/queue${query ? `?${query}` : ''}`);
  }

  async getScheduledEmailJobs(params?: { status?: string; take?: number }): Promise<any[]> {
    const qp = new URLSearchParams();
    if (params?.status) qp.append('status', params.status);
    if (params?.take) qp.append('take', params.take.toString());
    const query = qp.toString();
    return this.request<any[]>(`/admin/emails/scheduled-jobs${query ? `?${query}` : ''}`);
  }

  // Test endpoint
  async getHello(): Promise<{ message: string }> {
    return this.request<{ message: string }>('/hello');
  }

  // Stripe endpoints
  async calculateFees(tier: string): Promise<{ basePrice: number; processingFee: number; total: number }> {
    return this.request<{ basePrice: number; processingFee: number; total: number }>(`/stripe/calculate-fees/${tier}`);
  }

  async createCheckoutSession(membershipTier: string): Promise<{ sessionId: string; checkoutUrl: string }> {
    return this.request<{ sessionId: string; checkoutUrl: string }>('/stripe/create-checkout-session', {
      method: 'POST',
      body: JSON.stringify({ membershipTier }),
    });
  }
}

// Export a singleton instance that can be configured
let apiClientInstance: ApiClient | null = null;

export function initializeApiClient(options: ApiClientOptions): ApiClient {
  apiClientInstance = new ApiClient(options);
  return apiClientInstance;
}

export function getApiClient(): ApiClient {
  if (!apiClientInstance) {
    // Auto-initialize with default settings if not already initialized
    // This handles cases where components try to use the API before App.tsx runs
    console.warn('API Client not initialized, using default configuration');
    const baseURL = typeof window !== 'undefined' 
      ? (window as any).VITE_API_URL || 'http://localhost:5001/api'
      : 'http://localhost:5001/api';
    
    apiClientInstance = new ApiClient({
      baseURL,
      getAuthToken: () => {
        if (typeof window !== 'undefined') {
          return localStorage.getItem('authToken');
        }
        return null;
      },
      onAuthError: () => {
        if (typeof window !== 'undefined') {
          localStorage.removeItem('authToken');
          localStorage.removeItem('authExpiresAt');
          localStorage.removeItem('authUser');
          window.location.href = '/login';
        }
      }
    });
  }
  return apiClientInstance;
}
