import apiService from './api.service';
import type { User, ApiResponse, PaginatedResponse } from '../types';

class UserService {
  async getUsers(page: number = 1, pageSize: number = 10): Promise<PaginatedResponse<User>> {
    const response = await apiService.get<ApiResponse<PaginatedResponse<User>>>(
      `/api/v1/users?page=${page}&pageSize=${pageSize}`
    );
    return response.data;
  }

  async getUser(id: string): Promise<User> {
    const response = await apiService.get<ApiResponse<User>>(`/api/v1/users/${id}`);
    return response.data;
  }

  async updateUser(id: string, user: Partial<User>): Promise<User> {
    const response = await apiService.put<ApiResponse<User>>(`/api/v1/users/${id}`, user);
    return response.data;
  }

  async deleteUser(id: string): Promise<void> {
    await apiService.delete(`/api/v1/users/${id}`);
  }

  async activateUser(id: string): Promise<User> {
    const response = await apiService.patch<ApiResponse<User>>(`/api/v1/users/${id}/activate`);
    return response.data;
  }

  async deactivateUser(id: string): Promise<User> {
    const response = await apiService.patch<ApiResponse<User>>(`/api/v1/users/${id}/deactivate`);
    return response.data;
  }
}

export const userService = new UserService();
export default userService;
