import type { User } from '@memberorg/shared';

export class ApiClient {
  private baseURL: string;

  constructor(baseURL: string) {
    this.baseURL = baseURL;
  }

  async getHello(): Promise<{ message: string }> {
    const response = await fetch(`${this.baseURL}/api/hello`);
    return response.json();
  }

  async getUser(): Promise<User> {
    // Mock user for now
    return {
      id: '1',
      email: 'user@example.com',
      name: 'Test User'
    };
  }
}