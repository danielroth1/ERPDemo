import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { userService } from '../../services/user.service';
import type { User } from '../../types';

interface UsersState {
  users: User[];
  selectedUser: User | null;
  isLoading: boolean;
  error: string | null;
  totalUsers: number;
  currentPage: number;
}

const initialState: UsersState = {
  users: [],
  selectedUser: null,
  isLoading: false,
  error: null,
  totalUsers: 0,
  currentPage: 1,
};

export const fetchUsers = createAsyncThunk(
  'users/fetchUsers',
  async ({ page, pageSize }: { page: number; pageSize: number }, { rejectWithValue }) => {
    try {
      return await userService.getUsers(page, pageSize);
    } catch (error: any) {
      return rejectWithValue(error.response?.statusText || 'Failed to fetch users');
    }
  }
);

export const updateUser = createAsyncThunk(
  'users/updateUser',
  async ({ id, user }: { id: string; user: Partial<User> }, { rejectWithValue }) => {
    try {
      return await userService.updateUser(id, user);
    } catch (error: any) {
      return rejectWithValue(error.response?.statusText || 'Failed to update user');
    }
  }
);

export const deleteUser = createAsyncThunk(
  'users/deleteUser',
  async (id: string, { rejectWithValue }) => {
    try {
      await userService.deleteUser(id);
      return id;
    } catch (error: any) {
      return rejectWithValue(error.response?.statusText || 'Failed to delete user');
    }
  }
);

export const toggleUserStatus = createAsyncThunk(
  'users/toggleStatus',
  async ({ id, isActive }: { id: string; isActive: boolean }, { rejectWithValue }) => {
    try {
      return isActive
        ? await userService.deactivateUser(id)
        : await userService.activateUser(id);
    } catch (error: any) {
      return rejectWithValue(error.response?.statusText || 'Failed to update user status');
    }
  }
);

const usersSlice = createSlice({
  name: 'users',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    setSelectedUser: (state, action) => {
      state.selectedUser = action.payload;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchUsers.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchUsers.fulfilled, (state, action) => {
        state.isLoading = false;
        state.users = action.payload.items;
        state.totalUsers = action.payload.totalCount;
        state.currentPage = action.payload.page;
      })
      .addCase(fetchUsers.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      })
      .addCase(updateUser.fulfilled, (state, action) => {
        const index = state.users.findIndex((u) => u.id === action.payload.id);
        if (index !== -1) {
          state.users[index] = action.payload;
        }
      })
      .addCase(deleteUser.fulfilled, (state, action) => {
        state.users = state.users.filter((u) => u.id !== action.payload);
        state.totalUsers -= 1;
      })
      .addCase(toggleUserStatus.fulfilled, (state, action) => {
        const index = state.users.findIndex((u) => u.id === action.payload.id);
        if (index !== -1) {
          state.users[index] = action.payload;
        }
      });
  },
});

export const { clearError, setSelectedUser } = usersSlice.actions;
export default usersSlice.reducer;
