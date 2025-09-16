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
  ApiError,
  EmailJob,
  EmailJobDetail,
  EmailJobStats,
  EmailQuota
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
      // Only return null for 404 (no RSVP found)
      if (error.statusCode === 404) {
        return null;
      }
      console.error(`Error fetching RSVP for event ${eventId}:`, error);
      // For now, return null for all errors to maintain backward compatibility
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

  async queueBroadcastEmail(data: EmailRequest): Promise<EmailResponse> {
    return this.request<EmailResponse>('/adminemail/queue', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  // Email job endpoints
  async getEmailJobs(status?: string): Promise<EmailJob[]> {
    const params = status ? `?status=${status}` : '';
    return this.request<EmailJob[]>(`/emailjob/list${params}`);
  }

  async getEmailJob(jobId: string): Promise<EmailJobDetail> {
    return this.request<EmailJobDetail>(`/emailjob/${jobId}`);
  }

  async cancelEmailJob(jobId: string): Promise<{ message: string }> {
    return this.request<{ message: string }>(`/emailjob/${jobId}/cancel`, {
      method: 'POST',
    });
  }

  async pauseEmailJob(jobId: string): Promise<{ message: string }> {
    return this.request<{ message: string }>(`/emailjob/${jobId}/pause`, {
      method: 'POST',
    });
  }

  async resumeEmailJob(jobId: string): Promise<{ message: string }> {
    return this.request<{ message: string }>(`/emailjob/${jobId}/resume`, {
      method: 'POST',
    });
  }

  async getEmailJobStats(): Promise<EmailJobStats> {
    return this.request<EmailJobStats>('/emailjob/stats');
  }

  async getEmailQuota(): Promise<EmailQuota> {
    return this.request<EmailQuota>('/emailjob/quota');
  }

  async sendBroadcastEmailWithProgress(
    data: EmailRequest,
    onProgress: (sent: number, total: number) => void
  ): Promise<boolean> {
    const token = this.options.getAuthToken();
    const url = `${this.options.baseURL}/adminemail/send-with-progress`;
    
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
      },
      body: JSON.stringify(data),
    });

    if (!response.ok) {
      throw new Error(`Failed to send emails: ${response.statusText}`);
    }

    const reader = response.body?.getReader();
    const decoder = new TextDecoder();
    
    if (!reader) {
      throw new Error('Response body is not readable');
    }

    let success = false;
    
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      
      const chunk = decoder.decode(value);
      const lines = chunk.split('\n');
      
      for (const line of lines) {
        if (line.startsWith('data: ')) {
          const data = line.slice(6);
          try {
            const progress = JSON.parse(data);
            onProgress(progress.sent, progress.total);
            if (progress.complete) {
              success = progress.success;
            }
          } catch (e) {
            console.error('Failed to parse progress data:', e);
          }
        }
      }
    }
    
    return success;
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