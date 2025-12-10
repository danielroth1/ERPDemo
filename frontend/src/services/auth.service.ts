import apiService from './api.service';
import type { LoginRequest, RegisterRequest, AuthResponse, ApiResponse, User } from '../types';

class AuthService {
  private refreshTimer: number | null = null;

  async login(credentials: LoginRequest): Promise<AuthResponse> {
    const response = await apiService.post<ApiResponse<AuthResponse>>(
      '/api/v1/auth/login',
      credentials
    );
    
    if (response.success && response.data) {
      localStorage.setItem('accessToken', response.data.accessToken);
      localStorage.setItem('refreshToken', response.data.refreshToken);
      localStorage.setItem('user', JSON.stringify(response.data.user));
      this.scheduleTokenRefresh();
    }
    
    return response.data;
  }

  async register(data: RegisterRequest): Promise<AuthResponse> {
    const response = await apiService.post<ApiResponse<AuthResponse>>(
      '/api/v1/auth/register',
      data
    );
    
    if (response.success && response.data) {
      localStorage.setItem('accessToken', response.data.accessToken);
      localStorage.setItem('refreshToken', response.data.refreshToken);
      localStorage.setItem('user', JSON.stringify(response.data.user));
      this.scheduleTokenRefresh();
    }
    
    return response.data;
  }

  async logout(): Promise<void> {
    try {
      await apiService.post('/api/v1/auth/logout');
    } finally {
      this.clearRefreshTimer();
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('user');
    }
  }

  async refreshToken(refreshToken: string): Promise<AuthResponse> {
    const response = await apiService.post<ApiResponse<AuthResponse>>(
      '/api/v1/auth/refresh',
      { refreshToken }
    );
    
    if (response.success && response.data) {
      localStorage.setItem('accessToken', response.data.accessToken);
      localStorage.setItem('refreshToken', response.data.refreshToken);
      localStorage.setItem('user', JSON.stringify(response.data.user));
      this.scheduleTokenRefresh();
    }
    
    return response.data;
  }

  private decodeToken(token: string): { exp: number } | null {
    try {
      const base64Url = token.split('.')[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      return JSON.parse(jsonPayload);
    } catch {
      return null;
    }
  }

  private scheduleTokenRefresh(): void {
    this.clearRefreshTimer();
    
    const accessToken = localStorage.getItem('accessToken');
    if (!accessToken) return;

    const decoded = this.decodeToken(accessToken);
    if (!decoded?.exp) return;

    const now = Date.now() / 1000;
    const expiresIn = decoded.exp - now;
    
    // Refresh 5 minutes before expiration
    const refreshIn = Math.max(0, (expiresIn - 300) * 1000);

    this.refreshTimer = setTimeout(async () => {
      const refreshToken = localStorage.getItem('refreshToken');
      if (refreshToken) {
        try {
          await this.refreshToken(refreshToken);
        } catch (error) {
          console.error('Auto token refresh failed:', error);
          // If refresh fails, redirect to login
          window.location.href = '/login';
        }
      }
    }, refreshIn);
  }

  private clearRefreshTimer(): void {
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
      this.refreshTimer = null;
    }
  }

  // Initialize auto-refresh on service creation
  initializeAutoRefresh(): void {
    const accessToken = localStorage.getItem('accessToken');
    if (accessToken && this.isAuthenticated()) {
      this.scheduleTokenRefresh();
    }
  }

  getCurrentUser(): User | null {
    const userStr = localStorage.getItem('user');
    return userStr ? JSON.parse(userStr) : null;
  }

  getAccessToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  isAuthenticated(): boolean {
    return !!this.getAccessToken();
  }
}

export const authService = new AuthService();
export default authService;
