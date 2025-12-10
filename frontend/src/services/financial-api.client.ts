// Financial API Client using Kiota-generated client
import { createFinancialClient } from '../generated/clients/financial/financialClient';
import { FetchRequestAdapter } from '@microsoft/kiota-http-fetchlibrary';
import { BearerTokenAuthenticationProvider } from './auth/bearer-token-provider';
import { extractErrorMessage } from '../utils/error-handler';
import type {
  AccountResponse,
  CreateAccountRequest,
  UpdateAccountRequest,
  TransactionResponse,
  CreateTransactionRequest,
  BudgetResponse,
  CreateBudgetRequest,
  UpdateBudgetRequest,
  BalanceSheetResponse,
  IncomeStatementResponse,
} from '../generated/clients/financial/models';

const API_BASE_URL = import.meta.env.VITE_API_GATEWAY_URL || 'http://localhost:5000';

class FinancialApiClient {
  private client;

  constructor() {
    const authProvider = new BearerTokenAuthenticationProvider();
    const adapter = new FetchRequestAdapter(authProvider);
    adapter.baseUrl = API_BASE_URL;

    this.client = createFinancialClient(adapter);
  }

  // Accounts
  async getAccounts(): Promise<AccountResponse[]> {
    try {
      const response = await this.client.api.v1.accounts.get();
      return response?.data || [];
    } catch (error: any) {
      // If 404, return empty array (no accounts yet)
      if (error?.status === 404 || error?.message?.includes('404')) {
        return [];
      }
      throw new Error(extractErrorMessage(error, 'Failed to load accounts'));
    }
  }

  async getAccountById(id: string): Promise<AccountResponse> {
    const response = await this.client.api.v1.accounts.byId(id).get();
    if (!response?.data) {
      throw new Error(`Account ${id} not found`);
    }
    return response.data;
  }

  async getUserAccount(userId: string): Promise<AccountResponse | null> {
    try {
      const response = await this.client.api.v1.accounts.user.byUserId(userId).get();
      return response?.data || null;
    } catch (error: any) {
      // If 404, return null (no account for user)
      if (error?.status === 404 || error?.message?.includes('404')) {
        return null;
      }
      throw error;
    }
  }

  async getAccount(id: string): Promise<AccountResponse> {
    return this.getAccountById(id);
  }

  async createAccount(request: CreateAccountRequest): Promise<AccountResponse> {
    try {
      const response = await this.client.api.v1.accounts.post(request);
      if (!response?.data) {
        throw new Error('Failed to create account');
      }
      return response.data;
    } catch (error: any) {
      throw new Error(extractErrorMessage(error, 'Failed to create account'));
    }
  }

  async updateAccount(id: string, request: UpdateAccountRequest): Promise<AccountResponse> {
    const response = await this.client.api.v1.accounts.byId(id).put(request);
    if (!response?.data) {
      throw new Error('Failed to update account');
    }
    return response.data;
  }

  async deleteAccount(_id: string): Promise<void> {
    // Delete method may not be available - backend might not support it
    throw new Error('Delete account not implemented in backend');
  }

  async adjustAccountBalance(accountId: string, amount: number): Promise<AccountResponse> {
    const response = await this.client.api.v1.accounts.byId(accountId).adjustBalance.post({
      queryParameters: { amount }
    });
    if (!response?.data) {
      throw new Error('Failed to adjust account balance');
    }
    return response.data;
  }

  // Transactions
  async getTransactions(skip: number = 0, limit: number = 10): Promise<TransactionResponse[]> {
    const response = await this.client.api.v1.transactions.get({
      queryParameters: { skip, limit },
    });
    return response?.data || [];
  }

  async getTransaction(id: string): Promise<TransactionResponse> {
    const response = await this.client.api.v1.transactions.byId(id).get();
    if (!response?.data) {
      throw new Error(`Transaction ${id} not found`);
    }
    return response.data;
  }

  async createTransaction(request: CreateTransactionRequest): Promise<TransactionResponse> {
    const response = await this.client.api.v1.transactions.post(request);
    if (!response?.data) {
      throw new Error('Failed to create transaction');
    }
    return response.data;
  }

  async deleteTransaction(id: string): Promise<void> {
    // Void the transaction instead of deleting
    await this.client.api.v1.transactions.byId(id).voidEscaped.post();
  }

  // Budgets
  async getBudgets(): Promise<BudgetResponse[]> {
    const response = await this.client.api.v1.budgets.get();
    return response?.data || [];
  }

  async getBudget(id: string): Promise<BudgetResponse> {
    const response = await this.client.api.v1.budgets.byId(id).get();
    if (!response?.data) {
      throw new Error(`Budget ${id} not found`);
    }
    return response.data;
  }

  async createBudget(request: CreateBudgetRequest): Promise<BudgetResponse> {
    const response = await this.client.api.v1.budgets.post(request);
    if (!response?.data) {
      throw new Error('Failed to create budget');
    }
    return response.data;
  }

  async updateBudget(id: string, request: UpdateBudgetRequest): Promise<BudgetResponse> {
    const response = await this.client.api.v1.budgets.byId(id).put(request);
    if (!response?.data) {
      throw new Error('Failed to update budget');
    }
    return response.data;
  }

  async deleteBudget(_id: string): Promise<void> {
    // Delete may not be available
    throw new Error('Delete budget not implemented in backend');
  }

  // Reports
  async getBalanceSheet(): Promise<BalanceSheetResponse> {
    const response = await this.client.api.v1.reports.balanceSheet.get();
    if (!response?.data) {
      throw new Error('Failed to fetch balance sheet');
    }
    return response.data;
  }

  async getIncomeStatement(startDate?: Date, endDate?: Date): Promise<IncomeStatementResponse> {
    const response = await this.client.api.v1.reports.incomeStatement.get({
      queryParameters: startDate && endDate ? { startDate, endDate } : undefined,
    });
    if (!response?.data) {
      throw new Error('Failed to fetch income statement');
    }
    return response.data;
  }
}

export const financialApiClient = new FinancialApiClient();
export default financialApiClient;
