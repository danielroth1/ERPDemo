import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { analyticsService } from '../../services/analytics.service';
import type { KPI, Alert } from '../../types';
import type { 
  DashboardMetricsResponse,
  TopProductResponse 
} from '../../generated/clients/dashboard/models';

interface AnalyticsState {
  kpis: KPI[];
  alerts: Alert[];
  dashboardSummary: DashboardMetricsResponse | null;
  topProducts: TopProductResponse[];
  revenueChart: any;
  isLoading: boolean;
  error: string | null;
}

const initialState: AnalyticsState = {
  kpis: [],
  alerts: [],
  dashboardSummary: null,
  topProducts: [],
  revenueChart: null,
  isLoading: false,
  error: null,
};

export const fetchKPIs = createAsyncThunk(
  'analytics/fetchKPIs',
  async (_, { rejectWithValue }) => {
    try {
      return await analyticsService.getKPIs();
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch KPIs');
    }
  }
);

export const fetchAlerts = createAsyncThunk(
  'analytics/fetchAlerts',
  async (_, { rejectWithValue }) => {
    try {
      return await analyticsService.getAlerts();
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch alerts');
    }
  }
);

export const fetchDashboardSummary = createAsyncThunk(
  'analytics/fetchDashboardSummary',
  async (_, { rejectWithValue }) => {
    try {
      return await analyticsService.getDashboardSummary();
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch dashboard summary');
    }
  }
);

export const fetchTopProducts = createAsyncThunk(
  'analytics/fetchTopProducts',
  async (limit: number = 5, { rejectWithValue }) => {
    try {
      return await analyticsService.getTopProducts(limit);
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch top products');
    }
  }
);

const analyticsSlice = createSlice({
  name: 'analytics',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    addAlert: (state, action) => {
      state.alerts.unshift(action.payload);
    },
    updateKPI: (state, action) => {
      const index = state.kpis.findIndex((k) => k.id === action.payload.id);
      if (index !== -1) {
        state.kpis[index] = action.payload;
      } else {
        state.kpis.push(action.payload);
      }
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchKPIs.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchKPIs.fulfilled, (state, action) => {
        state.isLoading = false;
        state.kpis = action.payload as any;
      })
      .addCase(fetchKPIs.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      })
      .addCase(fetchAlerts.fulfilled, (state, action) => {
        state.alerts = action.payload as any;
      })
      .addCase(fetchDashboardSummary.fulfilled, (state, action) => {
        state.dashboardSummary = action.payload;
      })
      .addCase(fetchTopProducts.fulfilled, (state, action) => {
        state.topProducts = action.payload;
      });
  },
});

export const { clearError, addAlert, updateKPI } = analyticsSlice.actions;
export default analyticsSlice.reducer;
