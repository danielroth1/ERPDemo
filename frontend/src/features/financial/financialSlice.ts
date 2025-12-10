import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { financialService } from '../../services/financial.service';
import type { Account, Transaction } from '../../types';

interface FinancialState {
  accounts: Account[];
  transactions: Transaction[];
  selectedTransaction: Transaction | null;
  isLoading: boolean;
  error: string | null;
  totalTransactions: number;
  currentPage: number;
}

const initialState: FinancialState = {
  accounts: [],
  transactions: [],
  selectedTransaction: null,
  isLoading: false,
  error: null,
  totalTransactions: 0,
  currentPage: 1,
};

export const fetchAccounts = createAsyncThunk(
  'financial/fetchAccounts',
  async (_, { rejectWithValue }) => {
    try {
      return await financialService.getAccounts();
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch accounts');
    }
  }
);

export const fetchTransactions = createAsyncThunk(
  'financial/fetchTransactions',
  async ({ page, pageSize }: { page: number; pageSize: number }, { rejectWithValue }) => {
    try {
      return await financialService.getTransactions(page, pageSize);
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch transactions');
    }
  }
);

export const createTransaction = createAsyncThunk(
  'financial/createTransaction',
  async (transaction: Partial<Transaction>, { rejectWithValue }) => {
    try {
      return await financialService.createTransaction(transaction);
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to create transaction');
    }
  }
);

export const deleteTransaction = createAsyncThunk(
  'financial/deleteTransaction',
  async (id: string, { rejectWithValue }) => {
    try {
      await financialService.deleteTransaction(id);
      return id;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to delete transaction');
    }
  }
);

const financialSlice = createSlice({
  name: 'financial',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    setSelectedTransaction: (state, action) => {
      state.selectedTransaction = action.payload;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchAccounts.fulfilled, (state, action) => {
        state.accounts = action.payload;
      })
      .addCase(fetchTransactions.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchTransactions.fulfilled, (state, action) => {
        state.isLoading = false;
        state.transactions = action.payload.items;
        state.totalTransactions = action.payload.totalCount;
        state.currentPage = action.payload.page;
      })
      .addCase(fetchTransactions.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      })
      .addCase(createTransaction.fulfilled, (state, action) => {
        state.transactions.unshift(action.payload);
        state.totalTransactions += 1;
      })
      .addCase(deleteTransaction.fulfilled, (state, action) => {
        state.transactions = state.transactions.filter((t) => t.id !== action.payload);
        state.totalTransactions -= 1;
      });
  },
});

export const { clearError, setSelectedTransaction } = financialSlice.actions;
export default financialSlice.reducer;
