import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { salesService } from '../../services/sales.service';
import type { Order, Customer } from '../../types';

interface SalesState {
  orders: Order[];
  customers: Customer[];
  selectedOrder: Order | null;
  isLoading: boolean;
  error: string | null;
  totalOrders: number;
  currentPage: number;
}

const initialState: SalesState = {
  orders: [],
  customers: [],
  selectedOrder: null,
  isLoading: false,
  error: null,
  totalOrders: 0,
  currentPage: 1,
};

export const fetchOrders = createAsyncThunk(
  'sales/fetchOrders',
  async ({ page, pageSize }: { page: number; pageSize: number }, { rejectWithValue }) => {
    try {
      return await salesService.getOrders(page, pageSize);
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch orders');
    }
  }
);

export const fetchCustomers = createAsyncThunk(
  'sales/fetchCustomers',
  async (_, { rejectWithValue }) => {
    try {
      return await salesService.getCustomers();
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch customers');
    }
  }
);

export const createOrder = createAsyncThunk(
  'sales/createOrder',
  async (order: Partial<Order>, { rejectWithValue }) => {
    try {
      return await salesService.createOrder(order);
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to create order');
    }
  }
);

export const updateOrderStatus = createAsyncThunk(
  'sales/updateOrderStatus',
  async ({ id, status }: { id: string; status: string }, { rejectWithValue }) => {
    try {
      return await salesService.updateOrderStatus(id, status);
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to update order status');
    }
  }
);

export const deleteOrder = createAsyncThunk(
  'sales/deleteOrder',
  async (id: string, { rejectWithValue }) => {
    try {
      await salesService.deleteOrder(id);
      return id;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to delete order');
    }
  }
);

const salesSlice = createSlice({
  name: 'sales',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    setSelectedOrder: (state, action) => {
      state.selectedOrder = action.payload;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchOrders.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchOrders.fulfilled, (state, action) => {
        state.isLoading = false;
        state.orders = action.payload.items;
        state.totalOrders = action.payload.totalCount;
        state.currentPage = action.payload.page;
      })
      .addCase(fetchOrders.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      })
      .addCase(fetchCustomers.fulfilled, (state, action) => {
        state.customers = action.payload;
      })
      .addCase(createOrder.fulfilled, (state, action) => {
        state.orders.unshift(action.payload);
        state.totalOrders += 1;
      })
      .addCase(updateOrderStatus.fulfilled, (state, action) => {
        const index = state.orders.findIndex((o) => o.id === action.payload.id);
        if (index !== -1) {
          state.orders[index] = action.payload;
        }
      })
      .addCase(deleteOrder.fulfilled, (state, action) => {
        state.orders = state.orders.filter((o) => o.id !== action.payload);
        state.totalOrders -= 1;
      });
  },
});

export const { clearError, setSelectedOrder } = salesSlice.actions;
export default salesSlice.reducer;
